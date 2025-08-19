using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;

namespace WebAPIUtility.Models
{
    public class Feedback
    {
        public string Result { get; set; } = "fail";

        public string Data { get; set; }
    }
}