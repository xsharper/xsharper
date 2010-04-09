using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using XS=XSharper.Core;
using System.Text;
using System.Web.Services;
using System.Xml;

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
        
        
        protected void Page_Load(object sender, EventArgs e)
        {
             if (!IsPostBack)
             {
                 // List all *.xsh files in App_Data
                 var d=new DirectoryInfo(Server.MapPath("~/App_Data"));
                 bool any = false;
                 if (d.Exists)
                 {
                     foreach (var name in d.GetFiles("*.xsh"))
                     {
                         if (!any)
                         {
                             cbScript.Items.Clear();
                             tbScript.Visible = false;
                             cbScript.Visible = true;
                             any = true;
                         }


                         var ctx = new XS.ScriptContext() {EnableCodePrecompilation = false};
                         XS.Script s = ctx.LoadScript(name.FullName,false);
                         StringBuilder sb=new StringBuilder("["+name.Name+"] ");
                         sb.Append(s.VersionInfo.Title);
                         cbScript.Items.Add(new ListItem(sb.ToString(), name.Name));
                     
                     }
                 }
                 if (!any)
                 {
                     XS.Script script = new XS.Script
                         {
                             Items = new List<XS.IScriptAction>
                                 {
                                     new XS.Print("Current directory: ${=.CurrentDirectory}") {OutTo = "^bold"},
                                     new XS.Print("Temp directory: ${%TEMP%}") {OutTo = "^bold"},
                                     new XS.Print(),
                                     new XS.Print("-- This will print 3 steps, ${=2+1} seconds one after another ---"),
                                     new XS.While
                                         {
                                             MaxCount = 3,
                                             Name = "i",
                                             Items = new List<XS.IScriptAction>
                                                 {
                                                     new XS.Print("Hello ") {NewLine = false},
                                                     new XS.Print("World #${i}!") {OutTo = "^bold"},
                                                     new XS.Sleep(3000)
                                                 }
                                         },
                                     new XS.Shell(@"@echo off

echo -- This batch file will print 10 steps, 2 seconds one after another
for /l %%f in (1 1 10) do (@echo Step #%%f) & (echo | @CHOICE /D y /T 2 2>nul 1>nul )
echo -- Completed -- 

")
                                         {
                                             OutTo = "^info",
                                             ErrorTo = "^error",
                                             CreateNoWindow = true,
                                             IgnoreExitCode = true,
                                             Mode = XS.ShellMode.Batch
                                         }

                                 }
                         };
                     tbScript.Text = script.SaveToString();
                     tbScript.Visible = true;
                     cbScript.Visible = false;
                 }
                 
             }
        }


        protected void btnRun_Click(object sender, EventArgs e)
        {
            btnRun.Enabled = false;
            tbScript.ReadOnly = true;

            btnStop.Enabled = true;
            output.InnerHtml = null;
            received.InnerHtml = null;

            var d = new DirectoryInfo(Server.MapPath("~/App_Data"));

            if (d.Exists)
            {
                tbScript.Visible = false;
                bool any = false;
                foreach (var name in d.GetFiles("*.xsh"))
                {
                    any = true;
                    if (StringComparer.OrdinalIgnoreCase.Compare(name.Name, cbScript.SelectedValue) == 0)
                    {
                        hfJobId.Value = Global.JobManager.Add(new RunScriptContext(name.FullName, null, tbArguments.Text, cbDebug.Checked)).ToString();
                        break;
                    }
                }
                if (any)
                    return;
            }

            tbScript.Visible = true;
            hfJobId.Value = Global.JobManager.Add(new RunScriptContext(null, tbScript.Text, tbArguments.Text, cbDebug.Checked)).ToString();
        }
    }
}
