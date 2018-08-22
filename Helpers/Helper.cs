using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using ClashAnalyzer.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace ClashAnalyzer
{
    public static class Helper
    {
        public const string CLASH_API = "https://api.clashroyale.com/v1/";
        public const string CLASH_API_KEY_NAME = "CLASH_API_KEY";

        public static void InitHttpClient(ExecutionContext context, ref HttpClient client)
        {
            // Set up app settings access.
            var config = new ConfigurationBuilder()
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

        public static void CheckCardLevels(List<PlayerDetail> players)
        {
            foreach (var player in players)
            {
                CheckCardLevels(player);
            }
        }

        public static void CheckCardLevels(PlayerDetail player)
        {
            var totalLevels = 0;
            var completedLevels = 0;

            foreach (var card in player.Cards)
            {
                totalLevels += card.MaxLevel;
                completedLevels += card.Level;
            }

            var percent = completedLevels * 100 / totalLevels;
            if (percent < 60)
            {
                FlagHelper.FlagPlayer(player.Name, $"Cards only {percent}% leveled");
            }
        }

        public static void Fail(string errorMessage)
        {
            throw new Exception(errorMessage);
        }
    }
}
