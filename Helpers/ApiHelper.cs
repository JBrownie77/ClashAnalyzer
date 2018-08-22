using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ClashAnalyzer.Models;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace ClashAnalyzer
{
    public static class ApiHelper
    {
        public const string CLAN_TAG = "#Q2YVYJ";

        public static async Task<List<PlayerDetail>> GetClanPlayers(TraceWriter log, HttpClient client)
        {
            var url = string.Format("clans/{0}/members", HttpUtility.UrlEncode(CLAN_TAG));
            var memberList = await Get<ClanMemberList>(log, client, url);

            var players = new List<PlayerDetail>();

            foreach (var member in memberList.Items)
            {
                var player = await GetPlayer(log, client, member.Tag);
                players.Add(player);
            }

            return players;
        }

        public static async Task<WarLog> GetClanWarlog(TraceWriter log, HttpClient client)
        {
            var url = string.Format("clans/{0}/warlog", HttpUtility.UrlEncode(CLAN_TAG));
            var warLog = await Get<WarLog>(log, client, url);
            return warLog;
        }

        public static async Task<PlayerDetail> GetPlayer(TraceWriter log, HttpClient client, string tag)
        {
            var url = string.Format("players/{0}", HttpUtility.UrlEncode(tag));
            var player = await Get<PlayerDetail>(log, client, url);
            return player;
        }

        public static async Task<T> Get<T>(TraceWriter log, HttpClient client, string url)
        {
            var result = await client.GetAsync(url);
            string content = await result.Content.ReadAsStringAsync();

            // Fail if there was an error.
            if (!result.IsSuccessStatusCode)
            {
                var error = JsonConvert.DeserializeObject<Error>(content);
                var message = string.Format("{0} failed: {1}", result.RequestMessage.RequestUri.AbsoluteUri, error.Message);
                Helper.Fail(message);
            }

            // Return the data.
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
