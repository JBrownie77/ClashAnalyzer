using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ClashAnalyzer
{
    public static class ClashAnalyzer
    {
        [FunctionName("ClashAnalyzer")]
        public static async Task Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, TraceWriter log, ExecutionContext context)
        {
            // Set up the HTTP client.
            var client = new HttpClient();
            Helper.InitHttpClient(context, ref client);

            // Get the list of players in the clan.
            var players = await ApiHelper.GetClanPlayers(log, client);

            // Check card levels for each player.
            Helper.CheckCardLevels(players);

            var data = FlagHelper.ToOutput();
            log.Info($"result!\n\n{data}");

            // await GetClanWarlog(log, client);

            // Dispose the HTTP client.
            client.Dispose();
        }
    }
}
