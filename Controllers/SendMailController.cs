using log4net;
using MimeKit;
using System;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using System.Web.Http;
using WebAPIUtility.Models;

namespace WebAPIUtility.Controllers
{
    public class SendMailController : ApiController
    {
        private static readonly string smtp_host = System.Configuration.ConfigurationManager.AppSettings["SMTPHost"];

        private static readonly string smtp_port = System.Configuration.ConfigurationManager.AppSettings["SMTPPort"];

        //private static readonly string smtp_user = System.Configuration.ConfigurationManager.AppSettings["SMTPUser"];

        //private static readonly string smtp_user_password = System.Configuration.ConfigurationManager.AppSettings["SMTPPassword"];

        private static readonly string sender = System.Configuration.ConfigurationManager.AppSettings["Sender"];

        private static readonly ILog logger = LogManager.GetLogger(typeof(SendMailController));

        static SendMailController()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        [HttpPost]
        public IHttpActionResult SendMail(MailMessageModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("it's not a valid model");
            try
            {
                Send(model.Caller, model.Subject, model.Body, model.Tos, model.Ccs, model.IsHighPriority);
                Feedback fb = new Feedback()
                {
                    Result = "pass",
                    Data = "send mail success"
                };
                return Ok(fb);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.InnerException + ex.StackTrace);
                return InternalServerError(ex);
            }
        }

        public static void Send(string senderName, string subject, string body, List<string> to, List<string> cc, bool isHighPriority)
        {
            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress(senderName, sender));
            to.ForEach(t =>
            {
                if (!string.IsNullOrEmpty(t))
                {
                    mailMessage.To.Add(MailboxAddress.Parse(t));
                }
            });
            if (cc != null)
            {
                cc.ForEach(c =>
                {
                    if (!string.IsNullOrEmpty(c))
                    {
                        mailMessage.Cc.Add(MailboxAddress.Parse(c));
                    }
                });
            }
            mailMessage.Subject = subject;
            mailMessage.Priority = isHighPriority ? MessagePriority.Urgent : MessagePriority.Normal;
            var builder = new BodyBuilder()
            {
                HtmlBody = body
            };
            mailMessage.Body = builder.ToMessageBody();
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect(smtp_host, Convert.ToInt32(smtp_port), MailKit.Security.SecureSocketOptions.None);
                logger.Info("SMTP client connected");
                //don't need authentication
                //smtpClient.Authenticate(smtp_user, smtp_user_password);
                logger.InfoFormat("message:{0} begin send", mailMessage.MessageId);
                smtpClient.Send(mailMessage);
                smtpClient.Disconnect(true);
                logger.Info("SMTP client disconnected");
            }
        }
    }
}
