using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using WebAPIUtility.Help;
using WebAPIUtility.Models;

namespace WebAPIUtility.Controllers
{
    [EnableCors("*", "*", "*")]
    public class CryptographyController : ApiController
    {
        [HttpPost]
        public IHttpActionResult GetConvertedStr(EncryptModel model)
        {
            Feedback feedback = new Feedback();
            if (string.IsNullOrEmpty(model.Key))
            {
                feedback.Result = "fail";
                feedback.Data = "key is invalid";
                return Ok(feedback);
            }
            else if (string.IsNullOrEmpty(model.Content))
            {
                feedback.Result = "fail";
                feedback.Data = "content is invalid";
                return Ok(feedback);
            }
            else if (string.IsNullOrEmpty(model.Action))
            {
                feedback.Result = "fail";
                feedback.Data = "action is invalid";
                return Ok(feedback);
            }
            else if (model.Action != "encrypt" && model.Action != "decrypt")
            {
                feedback.Result = "fail";
                feedback.Data = "action is invalid";
                return Ok(feedback);
            }
            if (model.Action.ToLower() == "encrypt")
            {
                //encrypt
                feedback.Result = "pass";
                var str = Cryptography.Encrypt(model.Content, "$Mps.ConnStrKey#2022");
                feedback.Data = str;
            }
            else if (model.Action.ToLower() == "decrypt")
            {
                //decrypt
                try
                {
                    feedback.Result = "pass";
                    var str = Cryptography.Decrypt(model.Content, "$Mps.ConnStrKey#2022");
                    feedback.Data = str;
                }
                catch (Exception ex)
                {
                    feedback.Result = "fail";
                    feedback.Data = "key content is invalid,can't decrypt=>" + ex.Message;
                }
            }
            return Ok(feedback);
        }
    }
}
