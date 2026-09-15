using System.Web;
using System.Web.Http;

namespace Contoso.LegacyBank.Statements.Api
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(App_Start.WebApiConfig.Register);
        }
    }
}
