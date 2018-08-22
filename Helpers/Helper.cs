using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClashAnalyzer.Email;
using ClashAnalyzer.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace ClashAnalyzer
{
    public static class Helper
    {
        public const string CLASH_API = "https://api.clashroyale.com/v1/";
        public const string CLASH_API_KEY_NAME = "CLASH_API_KEY";
        public const string SEND_GRID_API_KEY_NAME = "SEND_GRID_API_KEY";

        const int WAR_KING_LEVEL = 11;
        static IConfigurationRoot config;

        public static void InitHttpClient(ExecutionContext context, ref HttpClient client)
        {
            // Set up app settings access.
            config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Make sure the API token is available.
            var apiToken = config[Helper.CLASH_API_KEY_NAME];
            if (string.IsNullOrEmpty(apiToken))
            {
                Helper.Fail($"Missing {Helper.CLASH_API_KEY_NAME}");
            }

            // Set up the client.
            client.BaseAddress = new Uri(Helper.CLASH_API);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }

        public static void CheckLevels(List<PlayerDetail> players)
        {
            foreach (var player in players)
            {
                CheckKingLevel(player);
                CheckCardLevels(player);
            }
        }

        public static void CheckKingLevel(PlayerDetail player)
        {
            if (player.ExpLevel < WAR_KING_LEVEL)
            {
                FlagHelper.FlagPlayer(player.Name, $"King only level {player.ExpLevel}");
            }
        }

        public static void CheckCardLevels(PlayerDetail player)
        {
            var totalLevels = 0;
            var completedLevels = 0;

            foreach (var card in player.Cards)
            {
                // Max level is considered relative to the war league's max level.
                totalLevels += MapWarCardLevel(card.MaxLevel);
                completedLevels += card.Level;
            }

            var percent = completedLevels * 100 / totalLevels;
            if (percent < 60)
            {
                FlagHelper.FlagPlayer(player.Name, $"Cards only {percent}% leveled");
            }
        }

        public static void CheckWarResults(WarLog warLog, List<PlayerDetail> currentPlayers)
        {
            var warParticipants = new Dictionary<string, PlayerWarStats>(); 

            // Iterate wars and track participant stats.
            foreach (var war in warLog.Items)
            {
                foreach (var participant in war.Participants)
                {
                    // Only look at players currently in the clan.
                    if (!currentPlayers.Any(p => p.Tag == participant.Tag))
                    {
                        continue;
                    }

                    if (warParticipants.TryGetValue(participant.Name, out PlayerWarStats stats))
                    {
                        // Accumulate war stats if this player has already been seen.
                        stats.WarsParticipated++;
                        stats.WarDayBattles += participant.BattlesPlayed;
                        stats.WarDayWins += participant.Wins;
                        stats.CardsEarned += participant.CardsEarned;
                        stats.LossStreak = (participant.Wins < participant.BattlesPlayed) ?
                            (stats.LossStreak + 1) : 0;
                        stats.LongestLossStreak = Math.Max(stats.LossStreak, stats.LongestLossStreak);
                    }
                    else
                    {
                        var lossStreak = (participant.Wins < participant.BattlesPlayed) ? 1 : 0;

                        // Create a new entry if this player hasn't already been seen.
                        warParticipants[participant.Name] = new PlayerWarStats()
                        {
                            WarsParticipated = 1,
                            WarDayBattles = participant.BattlesPlayed,
                            WarDayWins = participant.Wins,
                            CardsEarned = participant.CardsEarned,
                            LossStreak = lossStreak,
                            LongestLossStreak = lossStreak
                        };
                    }
                }
            }

            // Flag players that have subpar stats.
            foreach (var name in warParticipants.Keys)
            {
                var stats = warParticipants[name];
                var winRate = (stats.WarDayBattles == 0) ? 0 : stats.WarDayWins * 100 / stats.WarDayBattles;

                if (winRate < 50)
                {
                    FlagHelper.FlagPlayer(name, $"Win rate of {winRate}% ({stats.WarDayWins}/{stats.WarDayBattles}) in last {warLog.Items.Count} wars");
                }

                if (stats.LongestLossStreak >= 3)
                {
                    FlagHelper.FlagPlayer(name, $"Loss streak of {stats.LongestLossStreak} in last {warLog.Items.Count} wars");
                }
            }

            // Sort by collection day cards earned.
            var participantsByCollection = warParticipants
                .OrderBy(p => p.Value.CardsEarned)
                .ToList();

            // Flag anyone who has below 50% of the average of the best collector.
            var bestCollector = participantsByCollection.Last();
            var bestCollectionRate = (double)bestCollector.Value.CardsEarned / bestCollector.Value.WarsParticipated;
            bestCollectionRate = Math.Round(bestCollectionRate, 0);

            foreach (var participant in participantsByCollection)
            {
                var collectionRate = (double)participant.Value.CardsEarned / participant.Value.WarsParticipated;
                collectionRate = Math.Round(collectionRate, 0);

                var rate = collectionRate * 100 / bestCollectionRate;

                if (rate < 50)
                {
                    FlagHelper.FlagPlayer(participant.Key, $"Only {collectionRate} collection cards averaged in last {warLog.Items.Count} wars");
                }
            }
        }

        public static async Task EmailResults(string results)
        {
            var apiToken = config[Helper.SEND_GRID_API_KEY_NAME];
            if (string.IsNullOrEmpty(apiToken))
            {
                Helper.Fail($"Missing {Helper.SEND_GRID_API_KEY_NAME}");
            }

            var from = "clashanalyzer@clashanalyzer.com";
            var to = new List<string>() { "jbrownie77@gmail.com", "jcbspb@hotmail.com" };
            var subject = "American Apex Update";

            var emailClient = new SendGridEmailClient(apiToken);
            bool success = await emailClient.SendEmailAsync(from, to, subject, results, isHtml: false);

            if (!success)
            {
                Fail("Failed to send email");
            }
        }

        public static void Fail(string errorMessage)
        {
            throw new Exception(errorMessage);
        }

        private static int MapWarCardLevel(int maxLevel)
        {
            // Max level is considered relative to the war league's max level.
            switch (maxLevel)
            {
                case 13: return 11;
                case 11: return 9;
                case 8: return 6;
                case 5: return 3;
                default: Fail($"Unknown max card level: {maxLevel}"); return maxLevel;
            }
        }
    }
}
