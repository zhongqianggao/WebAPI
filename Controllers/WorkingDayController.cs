using System;
using System.Collections.Generic;
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
    public class WorkingDayController : ApiController
    {
        public IHttpActionResult GetWorkingDaysCount(string startDate, string endDate)
        {
            Feedback feedback = new Feedback();
            //check date format
            Regex reg = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$");
            if (!reg.IsMatch(startDate) || !reg.IsMatch(endDate))
            {
                feedback.Result = "fail";
                feedback.Data = "date format is not correct";
                return Ok(feedback);
            }
            DateTime sDate = DateTime.MinValue, eDate = DateTime.MinValue;
            if (!DateTime.TryParse(startDate, out sDate) || !DateTime.TryParse(endDate, out eDate))
            {
                feedback.Result = "fail";
                feedback.Data = "date format is not correct";
                return Ok(feedback);
            }
            if (sDate > eDate)
            {
                feedback.Result = "fail";
                feedback.Data = "start-date is greater than end-date";
                return Ok(feedback);
            }
            using (var context = new WorkingDayDataClassDataContext())
            {
                feedback.Data = context.WorkingDayModels.Where(w => w.WorkingDay >= sDate && w.WorkingDay <= eDate).Count().ToString();
                feedback.Result = "pass";
                return Ok(feedback);
            }
        }

        [HttpGet]
        public IHttpActionResult GetWorkingDays(byte weekNumber, string year)
        {
            using (var context = new WorkingDayDataClassDataContext())
            {
                var workingDays = context.WorkingDayModels.Where(w => w.WeekNumber == weekNumber && w.Year == Convert.ToInt32(year)).Select(r => r.WorkingDay).OrderBy(r => r).ToList();
                var results = new List<string>();
                workingDays.ForEach(w =>
                {
                    results.Add(w.ToString("yyyy-MM-dd"));
                });
                return Json(results);
            }
        }

        [HttpGet]
        [Route("api/WorkingDay/GetWorkingDaysUS")]
        public IHttpActionResult GetWorkingDaysUS(byte weekNumber, string year)
        {
            using (var context = new WorkingDayUSDataClassDataContext())
            {
                var workingDays = context.WorkingDayUSModels.Where(w => w.WeekNumber == weekNumber && w.Year == Convert.ToInt32(year)).Select(r => r.WorkingDay).OrderBy(r => r).ToList();
                var results = new List<string>();
                workingDays.ForEach(w =>
                {
                    results.Add(w.ToString("yyyy-MM-dd"));
                });
                return Json(results);
            }
        }
    }
}
