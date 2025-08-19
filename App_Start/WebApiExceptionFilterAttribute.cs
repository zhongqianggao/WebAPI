using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Filters;

namespace WebAPIUtility.App_Start
{
    public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(WebApiExceptionFilterAttribute));
        public WebApiExceptionFilterAttribute()
        {

            log4net.Config.XmlConfigurator.Configure();
        }
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            logger.Error(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "——" +
                actionExecutedContext.Exception.GetType().ToString() + "：" + actionExecutedContext.Exception.Message + "——stack info：" +
                actionExecutedContext.Exception.StackTrace);

            if (actionExecutedContext.Exception is NotImplementedException)
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
            }
            else if (actionExecutedContext.Exception is TimeoutException)
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            else
            {
                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            base.OnException(actionExecutedContext);
        }
    }
}