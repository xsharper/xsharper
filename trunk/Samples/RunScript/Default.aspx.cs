using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using XS=XSharper.Core;
using System.Text;
using System.Web.Services;

namespace RunScript
{

    public class JobStatus
    {
        public bool     IsCompleted;
        public string   HtmlUpdate;
    }
    public partial class _Default : System.Web.UI.Page
    {
        [WebMethod]
        public static JobStatus GetUpdate(string jobId)
        {
            var jg = new Guid(jobId);
            var j = Global.JobManager.FindJob(jg);
            if (j==null)
                return null;
            var ret=new JobStatus
                {
                    IsCompleted = j.IsCompleted,
                    HtmlUpdate = j.GetHtmlUpdate()
                };
            if (ret.IsCompleted)
                Global.JobManager.RemoveJob(jg);
            return ret;
        }

        [WebMethod]
        public static void Stop(string jobId)
        {
            var j = Global.JobManager.FindJob(new Guid(jobId));
            if (j != null)
                j.Stop();
        }
        
        [WebMethod]
        public static string Start(string xml, string args, bool debug)
        {
            return Global.JobManager.CreateJob(xml, args, debug).ToString();
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
             if (!IsPostBack)
             {
                 XS.Script script = new XS.Script
                 {
                     Items = new List<XS.IScriptAction> {
                         new XS.Print("Current directory: ${=.CurrentDirectory}") { OutTo = "^bold"},
                         new XS.Print("Temp directory: ${%TEMP%}") { OutTo = "^bold"},
                                    new XS.Dir
                                    {
                                        From = "${%TEMP%}",
                                        Filter = "*.*",
                                        Items = new List<XS.IScriptAction> {
                                            new XS.Print(" > ${}") { OutTo = "^info"}
                                        }
                                    },
                        new XS.Print(),
                        new XS.While {
                                MaxCount = 10, 
                                Name = "i",
                                Items = new List<XS.IScriptAction> {
                                    new XS.Print("Hello ") { NewLine = false },
                                    new XS.Print("World #${i}!") { OutTo = "^bold" },
                                    new XS.Sleep(2000)
                                }
                        }
                     }
                 };
                 tbScript.Text = script.SaveToString();
             }
        }


        protected void btnRun_Click(object sender, EventArgs e)
        {
            btnRun.Enabled = false;
            tbScript.ReadOnly= true;
            btnStop.Enabled = true;
            output.InnerHtml = null;
            received.InnerHtml = null;
            hfJobId.Value = Start(tbScript.Text, tbArguments.Text, cbDebug.Checked);
        }
    }
}
