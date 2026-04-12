using blaubergselector_wrapper_coils.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace blaubergselector_wrapper_coils
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var rootPath = ConfigurationManager.AppSettings["Coils.RootPath"];
            CoilsEngine.Init(rootPath);
        }

        protected void Application_End()
        {
            CoilsEngine.Release();
        }
    }
}
