using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPIUtility.Models
{
    public class MailMessageModel
    {
        public string Caller { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public List<string> Tos { get; set; }

        public List<string> Ccs { get; set; }

        public bool IsHighPriority { get; set; }
    }
}