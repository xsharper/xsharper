#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using XS = XSharper.Core;

namespace ${GENERATED_NAMESPACE}
{
    public class ${GENERATED_CLASS_PROGRAM}
    {
        static class xs
        {
            public const string quiet = "xs.quiet";
            public const string debug = "xs.debug";
            public const string debugc = "xs.debugc";
            public const string verbose = "xs.verbose";
            public const string save = "xs.save";
            public const string log = "xs.log";
            public const string wait = "xs.wait";
            public const string last = "xs.last";
            public readonly static string nocolors = "xs.nocolors";
            public const string requireAdmin = "xs.requireAdmin";
            public const string scriptargs = "xs.scriptargs";
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern int SetErrorMode(int newMode);

         // Main must not call anything from embedded assemblies
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                // Disable fatal error message popups by SetErrorMode(SEM_FAILCRITICALERRORS)
                SetErrorMode(1);

                return AppDomainLoader.Loader(args) ?? Main2(args);
            }
            catch (System.Security.SecurityException)
            {
                Console.Error.WriteLine("Code security exception. Please copy the module to a local hard drive and try again.");
                return -3;
            }
        }

        // Calling with local scriptcontext
        public static int Main2(string[] args)
        {
            XS.ScriptContext context = new XS.ScriptContext(AppDomainLoader.ResourceAssembly);
            return MainWithContext(context, args);
        }

        // Real main
        static int MainWithContext(XS.ScriptContext context, string[] args)
        {
            AppDomainLoader.progress("MainWithContext: Entering --------------------");
            XS.CommandLineParameter[] param = new XS.CommandLineParameter[] {
                new XS.CommandLineParameter(xs.quiet,        XS.CommandLineValueCount.None, null,"true" ),
                new XS.CommandLineParameter(xs.debug,        XS.CommandLineValueCount.None, "false", "true" ) ,
                new XS.CommandLineParameter(xs.debugc,       XS.CommandLineValueCount.None, "false", "true" ) ,
                new XS.CommandLineParameter(xs.verbose,      XS.CommandLineValueCount.None, "false", "true" ) ,
                new XS.CommandLineParameter(xs.nocolors,     XS.CommandLineValueCount.None, "false", "true" ) ,
                new XS.CommandLineParameter(xs.wait,         XS.CommandLineValueCount.None, null,"true" ) ,
                new XS.CommandLineParameter(xs.save,         XS.CommandLineValueCount.Single, null,"xsharper_save.xsh" ) ,
                new XS.CommandLineParameter(xs.log,          XS.CommandLineValueCount.Single, null,"xsharper.log" ) ,
                new XS.CommandLineParameter(xs.requireAdmin, XS.CommandLineValueCount.None, null,"true" ) ,
                new XS.CommandLineParameter(xs.last,         XS.CommandLineValueCount.None, null,"true") ,
                new XS.CommandLineParameter(xs.scriptargs,   null, XS.CommandLineValueCount.Multiple, null,null) 
             };
            param[param.Length - 1].Last = true;
            param[param.Length - 2].Last = true;

            XS.CommandLineParameters xsParams = new XS.CommandLineParameters(param,"//",false);
            foreach (XS.CommandLineParameter a in xsParams)
                if (!string.IsNullOrEmpty(a.Name) && a.Name!=xs.scriptargs)
                    a.Switch = a.Name.Replace("xs.", "");
            
            
            int exitCode = 0;

            using (XS.ConsoleWithColors cout = new XS.ConsoleWithColors(Environment.GetEnvironmentVariable("XSH_COLORS")))
            using (XS.CtrlCInterceptor ctrl = new XS.CtrlCInterceptor())
            {
                context.Output += cout.OnOutput;
                ctrl.Output = context.Error;
                ctrl.Abort += delegate { context.Abort(); };

                AppDomainLoader.progress("MainWithContext: Console ready --------------------"); 
                System.Diagnostics.Stopwatch w = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    AppDomainLoader.progress("MainWithContext: Before parse--------------------"); 
                    xsParams.Parse(context, args, false);
                    AppDomainLoader.progress("MainWithContext: Before set options--------------------"); 
                    setOutputOptions(context, cout);

                    AppDomainLoader.progress("MainWithContext: Before load--------------------"); 
                    ${GENERATED_CLASS} cl = new ${GENERATED_CLASS}();
                    XS.Script s = cl.Script;
                    ctrl.IgnoreCtrlC = s.IgnoreCtrlC;
                    ctrl.AbortDelay = XS.Utils.ToTimeSpan(s.AbortDelay) ?? ctrl.AbortDelay;
                    ctrl.ExitDelay = XS.Utils.ToTimeSpan(s.ExitDelay) ?? ctrl.ExitDelay;
                        
                     
                    AppDomainLoader.progress("MainWithContext: After load--------------------"); 
                    if (context.IsSet(xs.save))
                    {
                        using (XS.ScriptContextScope r=new XS.ScriptContextScope(context))
                            s.Save(context.GetString(xs.save));
                    }
                    else
                    {
                        context.Compiler.AddRequireAdmin(XS.Utils.To<XS.RequireAdminMode>(context.GetStr(xs.requireAdmin, XS.RequireAdminMode.User.ToString())));
                        context.Compiler.AddRequireAdmin(s.RequireAdmin);
                        if (context.Compiler.RequireAdmin!=XS.RequireAdminMode.User && !context.IsAdministrator)
                            return restartAsAdmin(context, args, context.Compiler.RequireAdmin==XS.RequireAdminMode.Hidden);

                        AppDomainLoader.progress("MainWithContext: Before initialization --------------------"); 
                        context.Initialize(s);
                        AppDomainLoader.progress("MainWithContext: After initialization --------------------"); 
                        string[] parms = null;
                        if (context.IsSet(xs.scriptargs))
                            parms = context.GetStringArray(xs.scriptargs);

                        XS.ConsoleRedirector redir = new XS.ConsoleRedirector(context);
                        try
                        {
                            AppDomainLoader.progress("MainWithContext: Before executing --------------------"); 
                            object r = context.ExecuteScript(s, parms, XS.CallIsolation.High);
                            ctrl.KillAbortTimer();
                            AppDomainLoader.progress("MainWithContext: After executing --------------------"); 
                            if (r != null)
                                int.TryParse(r.ToString(), out exitCode);
                        }
                        finally 
                        {
                            ctrl.KillAbortTimer();
                            redir.Dispose();
                        }
                    }
                }
                catch (ThreadAbortException ae)
                {
                    resetAbort(context);
                    context.WriteException(ae.InnerException);
                    exitCode = -1000;
                }
                catch (XS.ScriptTerminateException te)
                {
                    resetAbort(context);
                    exitCode = te.ExitCode;
                    if (te.InnerException != null)
                        context.WriteException(te.InnerException);
                }
                catch (Exception e)
                {
                    resetAbort(context);
                    context.WriteException(e);
                    exitCode = -1;
                }
                finally
                {
                    resetAbort(context);
                }
                if (context.GetBool(xs.wait, false))
                {
                    cout.WriteLine(XS.OutputType.Info, string.Format("Completed in {0} with exit code={1}. Press Enter to close...", w.Elapsed, exitCode));
                    Console.ReadLine();
                }
            }
            return exitCode;

        }
        private static void resetAbort(XS.ScriptContext context)
        {
            if (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.AbortRequested)
                Thread.ResetAbort();
            context.ResetAbort();
        }
        private static void setOutputOptions(XS.ScriptContext context, XS.ConsoleWithColors cout)
        {
            // Set output level
            context.Verbose = context.GetBool(xs.verbose, false);
            cout.DebugMode = context.GetBool(xs.debug, false) || context.Verbose || context.GetBool(xs.debugc, false) || context.Verbose; ;
            cout.DebugToConsole = context.GetBool(xs.debugc, false);
            if (cout.DebugMode)
                context.MinOutputType = XS.OutputType.Debug;
            if (context.GetBool(xs.quiet, false))
                context.MinOutputType = XS.OutputType.Out;
            
            // Fill console properties
            cout.LogFile = context.GetString(xs.log, null);
            if (context.GetBool(xs.nocolors, false))
                cout.UseColors = false;
        }
        private static int restartAsAdmin(XS.ScriptContext context  , string[] args, bool hidden)
        {
            context.WriteLine(XS.OutputType.Info, "** Administrative privileges are required to run this script.\n** Please confirm to continue.");
            
            int n=AppDomainLoader.RunWithElevatedContext(
                    delegate(XS.ScriptContext ctx)
                    {
                        XS.ScriptContextScope.DefaultContext = ctx;
                        try
                        {
                            return MainWithContext(ctx, args);
                        }
                        finally
                        {
                            XS.ScriptContextScope.DefaultContext = null;
                        }
                    },hidden);
            if (n== -1)
                throw new XS.ScriptRuntimeException("An error occured while granting administrative privileges.");
            if (n != 0)
                throw new XS.ScriptRuntimeException("An error occured during script execution.");
            return 0;
        }

        ${INSERT_APPDOMAIN_LOADER}
    }             
}
