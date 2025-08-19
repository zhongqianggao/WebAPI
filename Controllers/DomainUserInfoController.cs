using log4net;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Cors;
using WebAPIUtility.LinqToSQL;
using WebAPIUtility.Models;

namespace WebAPIUtility.Controllers
{
    [EnableCors("*", "*", "*")]
    public class DomainUserInfoController : ApiController
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DomainUserInfoController));

        [HttpGet]
        public IHttpActionResult IsDisabled(string samAccount)
        {
            logger.Info($"check user id:{samAccount} is disabled or not");
            Feedback fb = new Feedback();
            if (string.IsNullOrEmpty(samAccount))
            {
                fb.Result = "fail";
                fb.Data = "Sam Account Is Missing";
                return Ok(fb);
            }
            try
            {
                using (DirectoryEntry searchRoot = new DirectoryEntry("LDAP://DC=monolithicpower,DC=com"))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(samAccountName={samAccount})";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            if (searchResultCollection.Count == 0)
                            {
                                fb.Result = "fail";
                                fb.Data = "Can't Find Domain User Info";
                                return Ok(fb);
                            }
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                var adsPath = searchResult.Properties["adspath"].Count > 0 ? searchResult.Properties["adspath"][0] : "";
                                fb.Result = "pass";
                                fb.Data = adsPath.ToString().Contains("OU=Disabled") ? "true" : "false";
                                if (fb.Data == "false")
                                {
                                    // Check if account is enabled using userAccountControl
                                    if (searchResult.Properties.Contains("userAccountControl"))
                                    {
                                        int userAccountControl = (int)searchResult.Properties["userAccountControl"][0];
                                        bool isEnabled = !Convert.ToBoolean(userAccountControl & 0x0002); // Bit 0x2 means account is disabled
                                        if (!isEnabled)
                                            fb.Data = "true";
                                    }
                                }
                                break;
                            }
                        }
                    }
                    return Ok(fb);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetLastLogonTime")]
        public IHttpActionResult GetLastLogonTime(string samAccount)
        {
            logger.Info($"get user id:{samAccount} last logon time");
            Feedback fb = new Feedback();
            if (string.IsNullOrEmpty(samAccount))
            {
                fb.Result = "fail";
                fb.Data = "Sam Account Is Missing";
                return Ok(fb);
            }
            try
            {
                using (DirectoryEntry searchRoot = new DirectoryEntry("LDAP://DC=monolithicpower,DC=com"))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(samAccountName={samAccount}))";
                        directorySearcher.PropertiesToLoad.Add("lastLogonTimestamp");
                        SearchResult searchResult = directorySearcher.FindOne();
                        if (searchResult != null && searchResult.Properties["lastLogonTimestamp"].Count > 0)
                        {
                            // Convert the large integer timestamp from AD into a DateTime
                            long timestamp = (long)searchResult.Properties["lastLogonTimestamp"][0];
                            DateTime lastLogonDate = DateTime.FromFileTime(timestamp);
                            fb.Result = "pass";
                            fb.Data = lastLogonDate.ToString("yyyy-MM-dd HH:mm:ss");
                            return Ok(fb);
                        }
                        else
                        {
                            fb.Result = "fail";
                            fb.Data = "Can't Find Last Logon Time";
                            return Ok(fb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetCreatedTime")]
        public IHttpActionResult GetCreatedTime(string samAccount)
        {
            logger.Info($"get user id:{samAccount} created time");
            Feedback fb = new Feedback();
            if (string.IsNullOrEmpty(samAccount))
            {
                fb.Result = "fail";
                fb.Data = "Sam Account Is Missing";
                return Ok(fb);
            }
            try
            {
                using (DirectoryEntry searchRoot = new DirectoryEntry("LDAP://DC=monolithicpower,DC=com"))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(samAccountName={samAccount}))";
                        directorySearcher.PropertiesToLoad.Add("whenCreated");
                        SearchResult searchResult = directorySearcher.FindOne();
                        if (searchResult != null && searchResult.Properties["whenCreated"].Count > 0)
                        {
                            fb.Result = "pass";
                            fb.Data = searchResult.Properties["whenCreated"][0].ToString();
                            return Ok(fb);
                        }
                        else
                        {
                            fb.Result = "fail";
                            fb.Data = "Can't Find Created Time";
                            return Ok(fb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetDomainUserInfoByUserName")]
        public IHttpActionResult GetDomainUserInfoByUserName(string userName)
        {
            logger.Info($"get user name:{userName} domain information");
            if (string.IsNullOrEmpty(userName))
            {
                return BadRequest("User Name Is Missed");
            }
            try
            {
                var ladp = "LDAP://OU=MPS,DC=monolithicpower,DC=com";
                List<DomainUserInfo> users = new List<DomainUserInfo>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(name={userName}))";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                users.Add(this.ParseDomainUserInfo(searchResult));
                            }
                        }
                    }
                }
                if (users.Count > 0)
                {
                    var result = Json(users[0]);
                    logger.Info($"result:{result.Content.SamAccountName}");
                    return result;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetDomainUserInfoBysamAccount")]
        public IHttpActionResult GetDomainUserInfoBysamAccount(string samAccount)
        {
            logger.Info($"get user id:{samAccount} domain information");
            if (string.IsNullOrEmpty(samAccount))
            {
                return BadRequest("User ID Is Missed");
            }
            try
            {
                var ladp = "LDAP://OU=MPS,DC=monolithicpower,DC=com";
                List<DomainUserInfo> users = new List<DomainUserInfo>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(samAccountName={samAccount}))";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                users.Add(this.ParseDomainUserInfo(searchResult));
                            }
                        }
                    }
                }
                if (users.Count > 0)
                {
                    var result = Json(users[0]);
                    logger.Info($"result:{result.Content.SamAccountName}");
                    return result;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetDomainUserInfoBysamAccountIncludingDisabled")]
        public IHttpActionResult GetDomainUserInfoBysamAccountIncludingDisabled(string samAccount)
        {
            logger.Info($"get user id:{samAccount} domain information");
            if (string.IsNullOrEmpty(samAccount))
            {
                return BadRequest("User ID Is Missed");
            }
            try
            {
                var ladp = "LDAP://DC=monolithicpower,DC=com";
                List<DomainUserInfo> users = new List<DomainUserInfo>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(samAccountName={samAccount}))";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                users.Add(this.ParseDomainUserInfo(searchResult));
                            }
                        }
                    }
                }
                if (users.Count > 0)
                {
                    var result = Json(users[0]);
                    logger.Info($"result:{result.Content.SamAccountName}");
                    return result;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetDomainUserInfoListByUserName")]
        public IHttpActionResult GetDomainUserInfoListByUserName(string userName)
        {
            logger.Info($"get user list from domain by user name key workd:{userName}");
            if (string.IsNullOrEmpty(userName))
            {
                return BadRequest("User Name Is Missed");
            }
            try
            {
                var ladp = "LDAP://OU=MPS,DC=monolithicpower,DC=com";
                List<DomainUserInfo> users = new List<DomainUserInfo>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(name=*{userName}*))";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                users.Add(this.ParseDomainUserInfo(searchResult));
                            }
                        }
                    }
                }
                if (users.Count > 0)
                {
                    return Ok(users);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private DomainUserInfo ParseDomainUserInfo(SearchResult searchResult)
        {
            DomainUserInfo user = new DomainUserInfo();
            user.UserName = searchResult.Properties["displayname"].Count > 0 ? searchResult.Properties["displayname"][0].ToString() : "";
            user.SamAccountName = searchResult.Properties["samaccountname"].Count > 0 ? searchResult.Properties["samaccountname"][0].ToString() : "";
            user.Branch = searchResult.Properties["company"].Count > 0 ? searchResult.Properties["company"][0].ToString() : "";
            user.Department = searchResult.Properties["department"].Count > 0 ? searchResult.Properties["department"][0].ToString() : "";
            user.MailAddress = searchResult.Properties["mail"].Count > 0 ? searchResult.Properties["mail"][0].ToString() : "";
            var groupCount = searchResult.Properties["memberof"];
            if (groupCount.Count > 0)
            {
                user.Groups = new List<string>();
                searchResult.Properties["memberof"].Cast<string>().ToList().ForEach(r => user.Groups.Add(this.ParseGroup(r)));
            }
            return user;
        }

        private string ParseGroup(string group)
        {

            return group.Contains(",OU") ? group.Substring(3, group.IndexOf(",OU") - 3) : group;
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetAllDomainUserInfoBysamAccount")]
        public IHttpActionResult GetAllDomainUserInfoBysamAccount(string samAccount)
        {
            logger.Info($"get user id:{samAccount} domain information");
            if (string.IsNullOrEmpty(samAccount))
            {
                return BadRequest("User ID Is Missed");
            }
            try
            {
                var ladp = "LDAP://DC=monolithicpower,DC=com";
                List<ADProperty> propertiesList = new List<ADProperty>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(samAccountName={samAccount})";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                foreach (string propertyName in searchResult.Properties.PropertyNames)
                                {
                                    // Retrieve all values for each property
                                    foreach (var value in searchResult.Properties[propertyName])
                                    {
                                        propertiesList.Add(new ADProperty
                                        {
                                            PropertyName = propertyName,
                                            PropertyValue = value?.ToString() ?? "null"
                                        });
                                    }
                                }

                                // Check if account is enabled using userAccountControl
                                if (searchResult.Properties.Contains("userAccountControl"))
                                {
                                    int userAccountControl = (int)searchResult.Properties["userAccountControl"][0];
                                    bool isEnabled = !Convert.ToBoolean(userAccountControl & 0x0002); // Bit 0x2 means account is disabled
                                    propertiesList.Add(new ADProperty
                                    {
                                        PropertyName = "Enabled",
                                        PropertyValue = isEnabled.ToString()
                                    });
                                }

                                // Output the properties for demonstration
                                foreach (var prop in propertiesList)
                                {
                                    logger.Info($"Property Name: {prop.PropertyName}, Property Value: {prop.PropertyValue}");
                                }
                            }
                        }
                    }
                }
                if (propertiesList.Count > 0)
                {
                    var result = Json(propertiesList);
                    return result;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/DomainUserInfo/GetSpecificDomainUserInfoBysamAccount")]
        public IHttpActionResult GetSpecificDomainUserInfoBysamAccount(string samAccount,string property)
        {
            logger.Info($"get user id:{samAccount} property:{property} domain information");
            if (string.IsNullOrEmpty(samAccount))
            {
                return BadRequest("User ID Is Missed");
            }
            try
            {
                Feedback fb = new Feedback();
                var ladp = "LDAP://OU=MPS,DC=monolithicpower,DC=com";
                List<ADProperty> propertiesList = new List<ADProperty>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(samAccountName={samAccount})";
                        directorySearcher.PropertiesToLoad.Add(property);
                        SearchResult searchResult = directorySearcher.FindOne();
                        if (searchResult != null && searchResult.Properties[property].Count > 0)
                        {
                            fb.Result = "pass";
                            fb.Data = searchResult.Properties[property][0].ToString();
                            return Ok(fb);
                        }
                        else
                        {
                            fb.Result = "fail";
                            fb.Data = $"Can't Find {property}";
                            return Ok(fb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("api/DomainUserInfo/GetDomainUserInfoByUserEmail")]
        public IHttpActionResult GetDomainUserInfoByEmail(string userEmail)
        {
            logger.Info($"get user :{userEmail} domain information");
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User Email Is Missed");
            }
            try
            {
                var ladp = "LDAP://OU=MPS,DC=monolithicpower,DC=com";
                List<DomainUserInfo> users = new List<DomainUserInfo>();
                using (DirectoryEntry searchRoot = new DirectoryEntry(ladp))
                {
                    using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot))
                    {
                        directorySearcher.Filter = $"(&(objectCategory=person)(objectClass=user)(mail={userEmail}))";
                        using (SearchResultCollection searchResultCollection = directorySearcher.FindAll())
                        {
                            foreach (SearchResult searchResult in searchResultCollection)
                            {
                                users.Add(this.ParseDomainUserInfo(searchResult));
                            }
                        }
                    }
                }
                if (users.Count > 0)
                {
                    var result = Json(users[0]);
                    logger.Info($"result:{result.Content.SamAccountName}");
                    return result;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + ex.StackTrace + ex.InnerException);
                return InternalServerError(ex);
            }
        }
    }

    public class DomainUserInfo
    {
        public string UserName { get; set; }

        public string SamAccountName { get; set; }

        public string Branch { get; set; }

        public string Department { get; set; }

        public string MailAddress { get; set; }

        public List<string> Groups { get; set; }
    }

    public class ADProperty
    {
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
    }
}