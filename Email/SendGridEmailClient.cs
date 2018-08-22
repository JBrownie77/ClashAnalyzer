using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ClashAnalyzer.Email
{
    public class SendGridEmailClient : IEmailClient
    {
        private SendGridClient client;

        public SendGridEmailClient(string apiKey)
        {
            this.client = new SendGridClient(apiKey);
        }

        public async Task<bool> SendEmailAsync(string from, List<string> to, string subject, string body, bool isHtml)
        {
            List<EmailAddress> toEmails = new List<EmailAddress>();
            to.ForEach(t => toEmails.Add(new EmailAddress(t)));

            var fromEmail = new EmailAddress(from);
            var plainTextContent = isHtml ? null : body;
            var htmlContent = isHtml ? body : null;

            var email = MailHelper.CreateSingleEmailToMultipleRecipients(fromEmail, toEmails, subject, plainTextContent, htmlContent);
            return await SendAsync(email);
        }

        private async Task<bool> SendAsync(SendGridMessage email)
        {
            var response = await client.SendEmailAsync(email);
            return response.StatusCode == System.Net.HttpStatusCode.Accepted;
        }
    }
}
