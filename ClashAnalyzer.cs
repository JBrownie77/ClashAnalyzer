using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ClashAnalyzer
{
    public static class ClashAnalyzer
    {
        [FunctionName("ClashAnalyzer")]
        public static async Task Run([TimerTrigger("0 0 10 * * *")]TimerInfo myTimer, TraceWriter log, ExecutionContext context)
        {
            // Set up the HTTP client.
            var client = new HttpClient();
            Helper.InitHttpClient(context, ref client);

            // Get the list of players in the clan and check their card levels.
            var currentPlayers = await ApiHelper.GetClanPlayers(log, client);
            Helper.CheckLevels(currentPlayers);

            // Get the war log and check player stats.
            var warLog = await ApiHelper.GetClanWarlog(log, client);
            Helper.CheckWarResults(warLog, currentPlayers);

            // Get the results and email them.
            var results = FlagHelper.ToResults();
            await Helper.EmailResults(results);

            // Dispose the HTTP client.
            client.Dispose();
        }
    }
}
