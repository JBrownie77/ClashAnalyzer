using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClashAnalyzer.Email
{
    public interface IEmailClient
    {
        Task<bool> SendEmailAsync(string from, List<string> to, string subject, string body, bool isHtml);
    }
}
