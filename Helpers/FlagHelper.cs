using System.Collections.Generic;
using System.Text;

namespace ClashAnalyzer
{
    public static class FlagHelper
    {
        static Dictionary<string, List<string>> flaggedPlayers = new Dictionary<string, List<string>>();

        public static void FlagPlayer(string name, string reason)
        {
            if (flaggedPlayers.TryGetValue(name, out List<string> reasons))
            {
                reasons.Add(reason);
            }
            else
            {
                flaggedPlayers.Add(name, new List<string>() { reason });
            }
        }

        public static string ToOutput()
        {
            var sb = new StringBuilder();

            foreach (var name in flaggedPlayers.Keys)
            {
                sb.AppendLine($"Flagged {name} for:");

                foreach (var reason in flaggedPlayers[name])
                {
                    sb.AppendLine($"    - {reason}");
                }
            }

            return sb.ToString();
        }
    }
}
