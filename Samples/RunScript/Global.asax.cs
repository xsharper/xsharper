using System;
using System.Linq;
using System.Threading;
using System.Web.Security;
using System.Web.SessionState;

namespace RunScript
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {

        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        private static long s_cnt = 0;
        private static JobManager s_jobManager=new JobManager();

        public static JobManager JobManager
        {
            get
            {
                return s_jobManager;
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Do a clean up from time to time
            if (Interlocked.Increment(ref s_cnt)%10==0)
                JobManager.RemoveOldJobs();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}