using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClashAnalyzer.Email;
using ClashAnalyzer.Models;

namespace ClashAnalyzer
{
    public static class Helper
    {
        public const string CLASH_API = "https://api.clashroyale.com/v1/";
        public const string CLASH_API_KEY_NAME = "CLASH_API_KEY";
        public const string SEND_GRID_API_KEY_NAME = "SEND_GRID_API_KEY";

        public static HttpClient HttpClient { get; set; }

        static string ApiToken { get; set; }
        static string SendGridToken { get; set; }

        public static void Init(string apiToken, string sendGridToken)
        {
            ApiToken = apiToken;
            SendGridToken = sendGridToken;

            if (string.IsNullOrEmpty(apiToken))
            {
                Fail($"Missing {CLASH_API_KEY_NAME}");
            }

            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri(CLASH_API);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
        }

        public static void Dispose()
        {
            HttpClient.Dispose();
        }

        public static async Task<string> GetRiverRaceStats(List<ClanMember> currentPlayers)
        {
            var riverRaceLog = await ApiHelper.GetRiverRaceLog(HttpClient);
            var currentRiverRace = await ApiHelper.GetCurrentRiverRace(HttpClient);

            // Use player tag to uniquely identify.
            var pointsAccumulated = new Dictionary<string, int>();
            var racesParticipated = new Dictionary<string, int>();
            var racesMissed = new Dictionary<string, int>();

            var allResults = riverRaceLog.Items.SelectMany(i => i.Standings.Where(s => s.Clan.Tag == ApiHelper.CLAN_TAG)).ToList();

            // Include the current race if we have already finished it.
            if (!string.IsNullOrEmpty(currentRiverRace.Clan.FinishTime))
            {
                allResults.Add(currentRiverRace);
            }

            // Accumulate stats across past races.
            foreach (var raceResult in allResults)
            {
                foreach (var participant in raceResult.Clan.Participants)
                {
                    // Skip if they aren't still in the clan.
                    if (!currentPlayers.Any(p => p.Tag == participant.Tag))
                        continue;

                    var points = participant.Fame + participant.RepairPoints;

                    // Track total points gained.
                    if (pointsAccumulated.ContainsKey(participant.Tag))
                    {
                        pointsAccumulated[participant.Tag] += points;
                    }
                    else
                    {
                        pointsAccumulated[participant.Tag] = points;
                    }

                    // Track number of races participated (for averaging points).
                    if (racesParticipated.ContainsKey(participant.Tag))
                    {
                        racesParticipated[participant.Tag]++;
                    }
                    else
                    {
                        racesParticipated[participant.Tag] = 1;
                    }

                    // Track inactive races.
                    if (points == 0)
                    {
                        if (racesMissed.ContainsKey(participant.Tag))
                        {
                            racesMissed[participant.Tag]++;
                        }
                        else
                        {
                            racesMissed[participant.Tag] = 1;
                        }
                    }
                }
            }

            // Flag any players that missed races.
            foreach (var kvp in racesMissed)
            {
                var player = currentPlayers.FirstOrDefault(predicate => predicate.Tag == kvp.Key);
                if (player != null)
                {
                    FlagHelper.FlagPlayer(player.Name, $"Didn't participate in {kvp.Value} of {allResults.Count} recent river races");
                }
            }

            // Create the results.
            string result = $"--- Avg Points in Last {allResults.Count} River Races ---\n";

            var sortedByAveragePoints = pointsAccumulated.Select(kvp => new
                {
                    Tag = kvp.Key,
                    Total = kvp.Value,
                    Average = (float)kvp.Value / racesParticipated[kvp.Key]
                })
                .OrderByDescending(i => i.Average).ToList();

            foreach (var kvp in sortedByAveragePoints)
            {
                var player = currentPlayers.FirstOrDefault(predicate => predicate.Tag == kvp.Tag);
                if (player != null)
                {
                    result += $"{player.Name}: {kvp.Average:0.00} ({kvp.Total} / {racesParticipated[kvp.Tag]})\n";
                }
            }

            return result;
        }

        public static void CheckInactives(List<ClanMember> currentPlayers)
        {
            foreach (var player in currentPlayers)
            {
                var lastSeen = DateTime.Parse(StandardizeDateTime(player.LastSeen));
                var numDays = DateTime.UtcNow.Subtract(lastSeen).Days;

                if (numDays > 3)
                {
                    FlagHelper.FlagPlayer(player.Name, $"Inactive for {numDays} days");
                }
            }
        }

        public static async Task EmailResults(string results)
        {
            if (string.IsNullOrEmpty(SendGridToken))
            {
                Fail($"Missing {SEND_GRID_API_KEY_NAME}");
            }

            var from = "clashanalyzer@clashanalyzer.com";
            var to = new List<string>() { "jbrownie77@gmail.com", "jcbspb@hotmail.com" };
            var subject = "American Apex Update";

            var emailClient = new SendGridEmailClient(SendGridToken);
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

        static string StandardizeDateTime(string dateTime)
        {
            // DateTime comes in format 20200901T181021.000Z
            // DateTime.Parse expects 2020-09-01T18:10:21.000Z
            dateTime = dateTime.Insert(4, "-");
            dateTime = dateTime.Insert(7, "-");

            var indexOfT = dateTime.IndexOf("T");
            dateTime = dateTime.Insert(indexOfT + 3, ":");
            dateTime = dateTime.Insert(indexOfT + 6, ":");

            return dateTime;
        }
    }
}
