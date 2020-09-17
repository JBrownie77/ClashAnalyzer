using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClashAnalyzer
{
    public static class ClashAnalyzer
    {
        [FunctionName("ClashAnalyzer")]
        public static async Task Run([TimerTrigger("0 0 8 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Executed at: {DateTime.Now}");

            // Set up app settings access.
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var apiToken = config[Helper.CLASH_API_KEY_NAME];
            var sendGridToken = config[Helper.SEND_GRID_API_KEY_NAME];
            await DoWork(apiToken, sendGridToken);
        }

        public static async Task DoWork(string apiToken, string sendGridToken)
        {
            Helper.Init(apiToken, sendGridToken);

            var currentPlayers = await ApiHelper.GetClanPlayers(Helper.HttpClient);
            string raceStats = await Helper.GetRiverRaceStats(currentPlayers);

            Helper.CheckInactives(currentPlayers);

            var results = raceStats + "\n\n" + FlagHelper.ToResults();
            await Helper.EmailResults(results);

            Helper.Dispose();
        }
    }
}
