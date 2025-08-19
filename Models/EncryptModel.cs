using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPIUtility.Models
{
    public class EncryptModel
    {
        public string Key { get; set; }
        public string Content { get; set; }
        public string Action { get; set; }
    }
}