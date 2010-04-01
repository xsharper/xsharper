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
            var j = Global.JobManager.Find<RunScriptContext>(jg);
            if (j==null)
                return null;
            var ret=new JobStatus
                {
                    IsCompleted = j.IsCompleted,
                    HtmlUpdate = j.GetHtmlUpdate()
                };
            if (ret.IsCompleted)
                Global.JobManager.Remove(jg);
            return ret;
        }

        [WebMethod]
        public static void Stop(string jobId)
        {
            Global.JobManager.Stop(new Guid(jobId));
        }
        
        [WebMethod]
        public static string Start(string xml, string args, bool debug)
        {
            return Global.JobManager.Add(new RunScriptContext(xml, args, debug)).ToString();
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
                        new XS.Print(),
                        new XS.Print("-- This will print 3 steps, ${=2+1} seconds one after another ---"),
                        new XS.While {
                                MaxCount = 3, 
                                Name = "i",
                                Items = new List<XS.IScriptAction> {
                                    new XS.Print("Hello ") { NewLine = false },
                                    new XS.Print("World #${i}!") { OutTo = "^bold" },
                                    new XS.Sleep(3000)
                                }
                        },
                        new XS.Shell(@"@echo off

echo -- This batch file will print 10 steps, 2 seconds one after another
for /l %%f in (1 1 10) do (@echo Step #%%f) & (echo | @CHOICE /D y /T 2 2>nul 1>nul )
echo -- Completed -- 

") {
                            OutTo = "^info",
                            ErrorTo = "^error",
                            CreateNoWindow = true,
                            IgnoreExitCode = true,
                            Mode = XS.ShellMode.Batch
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
