using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using XSharper.Core;

namespace $safeprojectname$
{
	class Program
    {
		static int Main(string[] args)
		{
			ScriptContext context = new ScriptContext();
			using (ConsoleWithColors cout = new ConsoleWithColors(Environment.GetEnvironmentVariable("XSH_COLORS")))
			using (ConsoleRedirector redir = new ConsoleRedirector(context))
            using (CtrlCInterceptor ctrl = new CtrlCInterceptor())
            {
                context.Output += cout.OnOutput;
                ctrl.Output = context.Error;
                ctrl.Abort += delegate { context.Abort(); };
                cout.DebugMode = true;

				// context.Progress=console.OnOutputProgress;
				int exitCode = 0;
				try
				{
                    var script = new Generated.$safeprojectname$().Script;
                    ctrl.IgnoreCtrlC = script.IgnoreCtrlC;
                    ctrl.AbortDelay = Utils.ToTimeSpan(script.AbortDelay) ?? ctrl.AbortDelay;
                    ctrl.ExitDelay = Utils.ToTimeSpan(script.ExitDelay) ?? ctrl.ExitDelay;

                    object o = context.ExecuteScript(script, args);
					if (o != null)
						exitCode = Utils.To<int>(o);
				}
				catch (ScriptTerminateException te)
				{
					exitCode = te.ExitCode;
					if (te.InnerException != null)
						context.WriteException(te.InnerException);
				}
				catch (Exception e)
				{
					context.WriteException(e);
					exitCode = -1;
				}
				return exitCode;
			}
		}
    }
}
