using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TPLHLRC.Web.Startup))]
namespace TPLHLRC.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
