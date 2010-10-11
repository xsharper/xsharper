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


        string[] getScripts()
        {
            var d = new DirectoryInfo(Server.MapPath("~/App_Data"));
            List<string> ret=new List<string>();
            if (d.Exists)
            {
                foreach (var name in d.GetFiles("*.xsh"))
                    ret.Add(name.FullName);
                foreach (var name in d.GetFiles("*.bat"))
                    ret.Add(name.FullName);
                foreach (var name in d.GetFiles("*.cmd"))
                    ret.Add(name.FullName);
                ret.Sort(StringComparer.InvariantCultureIgnoreCase);
            }
            return ret.ToArray();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // List all *.xsh files in App_Data
                var files = getScripts();
                if (files.Length > 0)
                {
                    cbScript.Items.Clear();
                    tbScript.Visible = false;
                    cbScript.Visible = true;

                    foreach (var file in files)
                    {
                        var name = Path.GetFileName(file);
                        var sb = new StringBuilder("[" + name + "] ");
                        if (Path.GetExtension(file).ToUpper() == ".XSH")
                        {
                            var ctx = new XS.ScriptContext() {EnableCodePrecompilation = false};
                            var s = ctx.LoadScript(file, false);
                            sb.Append(s.VersionInfo.Title);
                            cbScript.Items.Add(new ListItem(sb.ToString(), name));
                        }
                        else
                        {
                            var f=File.ReadAllLines(file);
                            if (f.Length>0 && f[0].StartsWith("@rem",StringComparison.OrdinalIgnoreCase))
                                sb.Append(f[0].Substring(5));
                            else
                                sb.Append(f[0].Substring(5));
                            cbScript.Items.Add(new ListItem(sb.ToString(), name));
                        }
                    }
                }
                else
                {
                    var script = new XS.Script
                        {
                            Items = new List<XS.IScriptAction>
                                {
                                    new XS.Print("Current directory: ${=.CurrentDirectory}") {OutTo = "^bold"},
                                    new XS.Print("Current user: ${=System.Security.Principal.WindowsIdentity.GetCurrent().Name}") {OutTo = "^bold"},
                                    new XS.Print("Temp directory: ${%TEMP%}") {OutTo = "^bold"},
                                    new XS.Print(),
                                    new XS.Print("-- This will print 3 steps, ${=2+1} seconds one after another ---"),
                                    new XS.While
                                        {
                                            MaxCount = "3",
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

            var files = getScripts();
            if (files.Length>0)
            {
                tbScript.Visible = false;
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    
                    if (StringComparer.OrdinalIgnoreCase.Compare(name, cbScript.SelectedValue) == 0)
                    {
                        if (Path.GetExtension(file).ToUpper() == ".XSH")
                            hfJobId.Value = Global.JobManager.Add(new RunScriptContext(file, null, tbArguments.Text, cbDebug.Checked)).ToString();
                        else
                        {
                            // We don't want user to enter stuff like '&& del c:\' so have to quote arguments explicitly. This won't be bulletproof though, ways of CMD.EXE are difficult to predict.
                            var shell = new XS.Shell() {Directory = Server.MapPath("~/App_Data"), OutTo = "^out", ErrorTo = "^error", CreateNoWindow = true};
                            shell.Args.Add(new XS.ShellArg(file, XS.TransformRules.None));
                            foreach (var s in XS.Utils.SplitArgs(tbArguments.Text))
                                shell.Args.Add(new XS.ShellArg(s, XS.TransformRules.None));

                            hfJobId.Value = Global.JobManager.Add(new RunScriptContext(null, new XS.Script { shell }.SaveToString(), null, cbDebug.Checked)).ToString();
                        }
                        break;
                    }
                }
                return;
            }

            tbScript.Visible = true;
            hfJobId.Value = Global.JobManager.Add(new RunScriptContext(null, tbScript.Text, tbArguments.Text, cbDebug.Checked)).ToString();
        }
    }
}
