using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ClashAnalyzer.Models;
using Newtonsoft.Json;

namespace ClashAnalyzer
{
    public static class ApiHelper
    {
        public const string CLAN_TAG = "#Q2YVYJ";

        public static async Task<List<ClanMember>> GetClanPlayers(HttpClient client)
        {
            var url = string.Format("clans/{0}/members", HttpUtility.UrlEncode(CLAN_TAG));
            var memberList = await Get<ClanMemberList>(client, url);
            return memberList.Items;
        }

        public static async Task<RiverRaceResult> GetCurrentRiverRace(HttpClient client)
        {
            var url = string.Format("clans/{0}/currentriverrace", HttpUtility.UrlEncode(CLAN_TAG));
            return await Get<RiverRaceResult>(client, url);
        }

        public static async Task<RiverRaceLog> GetRiverRaceLog(HttpClient client)
        {
            var url = string.Format("clans/{0}/riverracelog", HttpUtility.UrlEncode(CLAN_TAG));
            return await Get<RiverRaceLog>(client, url);
        }

        public static async Task<T> Get<T>(HttpClient client, string url)
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
