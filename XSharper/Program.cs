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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using XSharper.Core;
using Timer=System.Threading.Timer;


namespace XSharper
{
    static class xs
    {
        public readonly static string script = "xs.script";
        public readonly static string scriptargs = "xs.scriptargs";
        public readonly static string quiet = "xs.quiet";
        public readonly static string verbose = "xs.verbose";
        public readonly static string config = "xs.config";
        public readonly static string debug = "xs.debug";
        public readonly static string debugc = "xs.debugc";
        public readonly static string wait = "xs.wait";
        public readonly static string gencs = "xs.gencs";
        public readonly static string main = "xs.main";
        public readonly static string genexe = "xs.genexe";
        public readonly static string genwinexe = "xs.genwinexe";
        public readonly static string genlibrary = "xs.genlibrary";
        public readonly static string trace = "xs.trace";
        public readonly static string utf8 = "xs.utf8";
        public readonly static string noSrc = "xs.nosrc";
        public readonly static string icon = "xs.icon";
        public readonly static string genxsd = "xs.genxsd";
        public readonly static string genconfig = "xs.genconfig";
        public readonly static string gensample = "xs.gensample";
        public readonly static string forcenet20 = "xs.forcenet20";
        public readonly static string save = "xs.save";
        public readonly static string nocolors = "xs.nocolors";
        public readonly static string log = "xs.log";
        public readonly static string codeout = "xs.codeout";
        public static readonly string last = "xs.last";
        public readonly static string help = "xs.help";
        public readonly static string requireAdmin = "xs.requireAdmin";
        public readonly static string @ref = "xs.ref";
        public readonly static string @namespace = "xs.namespace";
        public readonly static string @class = "xs.class";
        public readonly static string compilerOptions = "xs.coptions";
        public readonly static string path = "xs.path";
        public readonly static string upgrade = "xs.upgrade";
        public readonly static string validate = "xs.validate";
        public readonly static string version = "xs.version";

        public readonly static string testElevation = "xs.testElevation";
        public readonly static string updateStage = "xs.updateStage";

        public static readonly string execRest = "xs.execRest";
        public readonly static string execRestD = "xs.execRestD";
        public readonly static string execRestP = "xs.execRestP";
        public readonly static string zip = "xs.zip";
        public readonly static string unzip = "xs.unzip";
        public readonly static string download = "xs.download";
    }

    partial class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern int SetErrorMode(int newMode);

        // Main must not call anything from embedded assemblies
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                // Disable fatal error message popups by SetErrorMode(SEM_FAILCRITICALERRORS)
                try
                {
                    SetErrorMode(1);
                }
                catch(System.EntryPointNotFoundException)
                {
                    
                }

                AppDomainLoader.progress("Main: Starting");
                AppDomain.CurrentDomain.UnhandledException += (a, b) => { AppDomainLoader.progress("Main: !!!" + b.IsTerminating); };
                int exitCode = AppDomainLoader.Loader(args) ?? MainWithoutContext(args);
                AppDomainLoader.progress("Main: Exiting with exit code "+exitCode);
                return exitCode;
            }
            catch(System.Security.SecurityException)
            {
                Console.Error.WriteLine("Code security exception. Please copy XSharper to a local hard drive and try again.");
                return -3;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static public int MainWithoutContext(string[] args)
        {
            AppDomainLoader.progress("MainWithoutContext: Entering");
            ScriptContext context = new ScriptContext(AppDomainLoader.ResourceAssembly);
            return MainWithContext(context, args);
        }

        static int MainWithContext(ScriptContext context, string[] args)
        {
            CommandLineParameters xsParams = getXsParams();
            int exitCode = 0;
            
            ConsoleRedirector redir = null;
            AppDomainLoader.progress("MainWithContext: Entering --------------------");
            bool utf8 = false;
            foreach (string arg in args)
                if (arg.Equals(xs.utf8.Replace("xs.", "//"), StringComparison.OrdinalIgnoreCase))
                    utf8 = true;
            using (ConsoleWithColors cout = new ConsoleWithColors(Environment.GetEnvironmentVariable("XSH_COLORS"),utf8))
            using (CtrlCInterceptor ctrl = new CtrlCInterceptor())
            {
                context.Output += cout.OnOutput;
                ctrl.Output = context.Error;
                ctrl.Abort += delegate { context.Abort(); };
                
                Stopwatch w = Stopwatch.StartNew();
                try
                {
                    // Parse arguments
                    var usage = new UsageGenerator() { Options = UsageOptions.None };
                    xsParams.Parse(context, args, false);
                    setOutputOptions(context, cout);
                    
                    if (context.IsSet(xs.path))
                        context.ScriptPath = context.GetString(xs.path);

                    // Load references
                    List<IScriptAction> preScript = getCommandlineReferences(context);

                    // Print help
                    if (args.Length == 0 || context.IsSet(xs.help))
                    {
                        loadReferences(context,preScript);
                        exitCode = HelpHelper.Help(context, usage , xsParams);
                        goto end;
                    }

                    // Handle upgrade
                    if (context.GetBool(xs.upgrade, false))    { return upgrade(context);}
                    if (context.IsSet(xs.updateStage))              { return updateStage(context, context.GetStringArray(xs.updateStage));}
                    
                    AppDomainLoader.progress("MainWithContext: Processing options");
                    // Process the remaining options
                    context.Compiler.AddRequireAdmin(Utils.To<RequireAdminMode>(context.GetStr(xs.requireAdmin, RequireAdminMode.User.ToString())));
                    
                    if (context.IsSet(xs.codeout))
                        context.CodeOutputDirectory = Path.GetFullPath(context.GetString(xs.codeout));
                    if (context.IsSet(xs.genconfig) || context.IsSet(xs.gensample))
                        genDemoConfig(cout, context);
                    if (context.IsSet(xs.forcenet20))
                        context.Compiler.DefaultNETVersion = new Version(2,0);
                    
                     AppDomainLoader.progress("MainWithContext: Processing options, continuing");

                    List<string> filteredArgs = new List<string>();
                    if (context.IsSet(xs.scriptargs))
                        filteredArgs.AddRange(context.GetStringArray(xs.scriptargs));
                    
                    
                    // Run utilities, like //download etc
                    Script script= getInlineScript(context, filteredArgs);
                    string scriptName = context.GetString(xs.script, null);
                    if (script == null)
                    {
                        // Load the script
                        if (scriptName == "/?" || scriptName == "-?")
                        {
                            for (int i = 0; i < args.Length;++i )
                                if (args[i] == "/?" || args[i]=="-?")
                                {
                                    if (i != args.Length - 1)
                                        context[xs.help] = args[i + 1];
                                    break;
                                }
                            loadReferences(context, preScript);
                            return HelpHelper.Help(context, usage, xsParams);
                        }
                        if (scriptName != null)
                        {
                            AppDomainLoader.progress("MainWithContext: Loading script " + scriptName);
                            script = loadScript(scriptName, context);
                            AppDomainLoader.progress("MainWithContext: Loading completed");
                        }
                    }
                    
                    AppDomainLoader.progress("MainWithContext: About to initialize");
                    // Attach script
                    if (script != null)
                    {
                        // Insert pre-script before the script body
                        int n = 0;
                        foreach (var list in preScript)
                            script.Items.Insert(n++, list);

                        AppDomainLoader.BaseDirectory = script.DirectoryName;

                        RequireAdminMode mode = context.Compiler.RequireAdmin;
                        if ((!context.IsAdministrator || context.GetBool(xs.testElevation, false)) && !isCodeGeneration(context) && mode!=RequireAdminMode.User)
                        {
                            return restartAsAdmin(context, args, mode==RequireAdminMode.Hidden && !(context.GetBool(xs.testElevation, false)));
                        }

                        AppDomainLoader.progress("MainWithContext: Before script initialization");
                        if (isCodeGeneration(context))
                        {
                            if (!context.EnableCodePrecompilation && (context.IsSet(xs.genexe) ||
                                                                      context.IsSet(xs.genwinexe) ||
                                                                      context.IsSet(xs.genlibrary) ||
                                                                      context.IsSet(xs.gencs)))
                                throw new ParsingException("One of the loaded scripts has precompilation disabled. Executable cannot be generated from this script.");
                            context.EnableCodePrecompilation = false;
                        }
                        ctrl.IgnoreCtrlC = script.IgnoreCtrlC;
                        ctrl.AbortDelay = Utils.ToTimeSpan(script.AbortDelay) ?? ctrl.AbortDelay;
                        ctrl.ExitDelay = Utils.ToTimeSpan(script.ExitDelay) ?? ctrl.ExitDelay;
                        context.Initialize(script);

                        AppDomainLoader.progress("MainWithContext: Script initialization completed");
                    }

                    // After precompilation we're ready to write .exe, if requested
                    if (isCodeGeneration(context))
                    {
                        doCodeGeneration(context, script);
                    }
                    else if (script != null)
                    {
                        // Run the script
                        AppDomainLoader.progress("MainWithContext: Before script execution");
                        redir = new ConsoleRedirector(context);
                        try
                        {
                            object r = context.ExecuteScript(script, filteredArgs.ToArray(), CallIsolation.High);
                            if (r != null)
                                int.TryParse(r.ToString(), out exitCode);
                        }
                        finally
                        {
                            ctrl.KillAbortTimer();
                            redir.Dispose();
                            redir = null;
                        }
                        
                        AppDomainLoader.progress("MainWithContext: Script execution completed");
                    }
                }
                catch (ThreadAbortException ae)
                {
                    AppDomainLoader.progress("MainWithContext: ThreadAbortException is being aborted");
                    resetAbort(context);
                    context.WriteException(ae);
                    exitCode = -1;
                }
                catch(ScriptTerminateException te)
                {
                    exitCode = te.ExitCode;
                    resetAbort(context);
                    if (te.InnerException != null)
                    {
                        context.WriteException(te.InnerException);  
                        AppDomainLoader.progress("MainWithContext: " + te.InnerException);
                    }
                    AppDomainLoader.progress("MainWithContext: Terminating with exit code " + exitCode);
                }
                catch (Exception e)
                {
                    exitCode = -1;
                    resetAbort(context);
                    context.WriteException(e);
                    AppDomainLoader.progress("MainWithContext: " + e);
                }
                finally
                {
                    resetAbort(context);
                    AppDomainLoader.BaseDirectory = null;
                }
            end:
                // Display how long did it take
                w.Stop();
                if (context.GetBool(xs.wait,false))
                {
                    cout.WriteLine(OutputType.Info,string.Format("Completed in {0} with exit code={1}. Press Enter to close...", w.Elapsed, exitCode));
                    Console.ReadLine();
                    
                }
            }
            AppDomainLoader.progress("MainWithContext: Exiting with code "+exitCode);
            return exitCode;
        }

        private static void resetAbort(ScriptContext context)
        {
            if (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.AbortRequested)
                Thread.ResetAbort();
            context.ResetAbort();
        }
        // If there were any references in the command line, load them
        private static void loadReferences(ScriptContext context, ICollection<IScriptAction> actions)
        {
            if (actions.Count > 0)
            {
                var script = context.CreateNewScript(null);
                script.Items.AddRange(actions);
                context.Initialize(script);
            }
        }


        private static void setOutputOptions(ScriptContext context, ConsoleWithColors cout)
        {
            // Set output level
            context.Verbose = context.GetBool(xs.verbose, false);
            cout.DebugMode = context.GetBool(xs.debug, false) || context.Verbose || context.GetBool(xs.debugc, false) || context.Verbose; ;
            cout.DebugToConsole = context.GetBool(xs.debugc, false);
            if (cout.DebugMode)
                context.MinOutputType = OutputType.Debug;
            if (context.GetBool(xs.quiet, false))
                context.MinOutputType = OutputType.Out;
            
            if (context.GetBool(xs.trace, false))
                context.Progress += cout.OnOutputProgress;

            
            // Fill console properties
            string log = context.GetString(xs.log, null);
            cout.LogFile = log;
            if (context.GetBool(xs.nocolors, false))
                cout.UseColors = false;
            
        }

        private static int restartAsAdmin(ScriptContext context, string[] args, bool hidden)
        {
            context.WriteLine(OutputType.Info, "** Administrative privileges are required to run this script.\n** Please confirm to continue.");
            if (context.IsSet(xs.testElevation))
            {
                List<string> f = new List<string>();
                foreach (string s in args)
                    if (string.Compare(s, xs.testElevation.Replace("xs.", "//"), StringComparison.OrdinalIgnoreCase) != 0)
                        f.Add(s);
                args = f.ToArray();
            }
            
            AppDomainLoader.progress("MainWithContext: About to restart with CmdLine="+string.Join(" ",args));

            
            int n=AppDomainLoader.RunWithElevatedContext(
                    delegate(ScriptContext ctx)
                        {
                            ScriptContextScope.DefaultContext = ctx;
                            try
                            {
                                if (!ctx.IsAdministrator)
                                    throw new ScriptRuntimeException("Administrator privileges are required");
                                return MainWithContext(ctx, args);
                            }
                            finally
                            {
                                ScriptContextScope.DefaultContext = null;
                            }
                        }, hidden );
            if (n== -1)
                throw new ScriptRuntimeException("An error occured while granting administrative privileges.");
            if (n != 0)
                throw new ScriptRuntimeException("An error occured during script execution.");
        
            return 0;
        }

        
        private static CommandLineParameters getXsParams()
        {
            CommandLineParameters args = new CommandLineParameters(
                new[]
                    {
                        new CommandLineParameter {Value = "Syntax: XSharper [<script>] [//engine parameters] [<script parameters>]"},
                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "Parameters may be specified as /name value, or as /name=value, or as /name:value. Parameter names are case insensitive."},
                        new CommandLineParameter {},

                        new CommandLineParameter(xs.script, null, CommandLineValueCount.Single, null, null) {Description = "script.xsh", Value = "XML file to execute. If . (dot) is specified, <xsharper> section from the configuration file is loaded."},
                        new CommandLineParameter(xs.scriptargs, null, CommandLineValueCount.Multiple, null, null) {Description = "<script parameters>"},

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- basic parameters ---"},
                        new CommandLineParameter(xs.config, CommandLineValueCount.None, null, "true") {Value = "Use an alternative configuration file", Description = "app.config"},
                        new CommandLineParameter(xs.quiet, CommandLineValueCount.None, null, "true") {Value = "Don't display informational messages"},
                        new CommandLineParameter(xs.debug, CommandLineValueCount.None, "0", "true") {Value = "Turn on additional debug output (visible with dbgview)"},
                        new CommandLineParameter(xs.debugc, CommandLineValueCount.None, "0", "true") {Value = "Turn on additional debug output (written to console)"},
                        new CommandLineParameter(xs.verbose, CommandLineValueCount.None, "0", "true") {Value = "Turn on script engine debug output"},
                        new CommandLineParameter(xs.trace, CommandLineValueCount.None, null, "true") {Value = "Trace script progress"},
                        new CommandLineParameter(xs.wait, CommandLineValueCount.None, null, "true") {Value = "Display execution time and wait for user input before terminating"},
                        new CommandLineParameter(xs.nocolors, CommandLineValueCount.None, null, "true") {Value = "Output to console in the same color"},
                        new CommandLineParameter(xs.path, CommandLineValueCount.Single, null, "") {Value = "; separated list of directories to search for scripts", Description = "directories", Default = Environment.GetEnvironmentVariable("XSH_PATH")},
                        new CommandLineParameter(xs.@ref, CommandLineValueCount.Single, null, "") {Value = "; separated list of assemblies or assembly filenames to load", Description = "references", Default = Environment.GetEnvironmentVariable("XSH_REF")},
                        new CommandLineParameter(xs.log, CommandLineValueCount.Single, null, "xsharper.log") {Value = "Copy all output to the specified file", Description = "log.txt"},
                        new CommandLineParameter(xs.requireAdmin, CommandLineValueCount.None, null, "Admin") {Value = "Force process elevation to obtain admin privileges (RunAs)"},
                        new CommandLineParameter(xs.validate, CommandLineValueCount.None, null, "true") {Value = "Validate script signature"},
                        new CommandLineParameter(xs.utf8, CommandLineValueCount.None, "false", "true") {Value = "Force UTF8 console output. ANSI is used if not specified."},
                        new CommandLineParameter(xs.last, CommandLineValueCount.None, null, "true") {Last = true, Value = "Stop looking for //* parameters after this"},
                        

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- code & schema generation ---"},
                        new CommandLineParameter(xs.save, CommandLineValueCount.Single, null, "xsharper_save.xsh") {Value = "Save script to the specified xml file", Description = "x.xsh"},
                        new CommandLineParameter(xs.gensample, CommandLineValueCount.Single, null, "sample.xsh") {Value = "Generate a sample script file x.xsh", Description = "x.xsh"},
                        new CommandLineParameter(xs.genconfig, CommandLineValueCount.Single, null, "app.config") {Value = "Generate a sample config file", Description = "app.config"},
                        new CommandLineParameter(xs.genxsd, CommandLineValueCount.Single, null, "*") {Value = "Generate and save XML schema", Description = "schema.xsd"},
                        new CommandLineParameter(xs.gencs, CommandLineValueCount.Single, null, "*") {Value = "Generate C# source code into file x.cs", Description = "x.cs"},
                        new CommandLineParameter(xs.forcenet20, CommandLineValueCount.None, null, "true") {Value = "Generate C# source code using .NET 2.0 syntax"},
                        new CommandLineParameter(xs.@namespace, CommandLineValueCount.Single, null, null) {Value = "Namespace for generated C# code (GUID if not specified)", Description = "namespace"},
                        new CommandLineParameter(xs.@class, CommandLineValueCount.Single, null, null) {Value = "Class name of the generated C# code"},
                        new CommandLineParameter(xs.main, CommandLineValueCount.None, null, "true") {Value = "Create Program.Main entry point"},

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- executable generation ---"},
                        new CommandLineParameter(xs.genexe, CommandLineValueCount.Single, null, "*") {Value = "Generate standalone console executable", Description = "x.exe"},
                        new CommandLineParameter(xs.genwinexe, CommandLineValueCount.Single, null, "*") {Value = "Generate standalone Windows executable", Description = "x.exe"},
                        new CommandLineParameter(xs.genlibrary, CommandLineValueCount.Single, null, "*") {Value = "Generate standalone assembly", Description = "x.dll"},
                        new CommandLineParameter(xs.icon, CommandLineValueCount.Single) {Value = "Icon file for the produced executable (requires .NET 3.5 compiler)", Description = "x.ico"},
                        new CommandLineParameter(xs.compilerOptions, CommandLineValueCount.Single, null, null) {Value = "Extra C# compiler options", Description = "options"},
                        new CommandLineParameter(xs.noSrc, CommandLineValueCount.None, null, "true") {Value = "Do not include C# source code into the executable"},
                        new CommandLineParameter(xs.codeout, CommandLineValueCount.Single, null, null) {Value = "Save temporary .cs files in this directory", Description = "directory"},

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- inline scripts ---"},
                        new CommandLineParameter(xs.execRest, "/", CommandLineValueCount.Multiple, null, "true") {Last = true, Value = "Execute a piece of xml or C# code until the end of the line, replacing ` with \"", Description = "code"},
                        new CommandLineParameter(xs.execRestD, "/#", CommandLineValueCount.Multiple, null, "true") {Synonyms = "/#?", Last = true, Value = "Same as above, but evaluate and dump the result", Description = "code"},
                        new CommandLineParameter(xs.execRestP, "/p", CommandLineValueCount.Multiple, null, "true") {Last = true, Value = "Same as above, but evaluate and print the result", Description = "code"},

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- built-in scripts (use /" + xs.save.Replace("xs.", "/") + " to save instead of executing) ---"},
                        new CommandLineParameter(xs.download, CommandLineValueCount.Multiple, null, null) {Unspecified = "/?", Description = "uri", Value = "Download from url, and save to file"},
                        new CommandLineParameter(xs.zip, CommandLineValueCount.Multiple, null, null) {Unspecified = "/?", Value = "Archive directory to a .zip file", Description = "archive.zip"},
                        new CommandLineParameter(xs.unzip, CommandLineValueCount.Multiple, null, null) {Unspecified = "/?", Value = "Extract a .zip file to directory", Description = "archive.zip",},
                        new CommandLineParameter(xs.version, CommandLineValueCount.None, null, "true") {Value = "Display XSharper version"},

                        new CommandLineParameter {},
                        new CommandLineParameter {Value = "   --- utilitites ---"},
                        new CommandLineParameter(xs.upgrade, CommandLineValueCount.None, null, "true") {Value = "Upgrade this executable to the latest XSharper version"},
                        new CommandLineParameter(xs.help, CommandLineValueCount.Single, null, "") {Value = "If action or class name is not specified, display this text. If action is *, display the list of actions. Otherwise display specific action/class help", Description = "action/class"},

                        // These are undocumented test commands
                        new CommandLineParameter(xs.testElevation, CommandLineValueCount.None, null, "true"),
                        new CommandLineParameter(xs.updateStage, CommandLineValueCount.Multiple, null, null)
                    }, "//", false);
            foreach (var a in args)
                if (!string.IsNullOrEmpty(a.Name) && string.IsNullOrEmpty(a.Switch) && a.Name!=xs.script && a.Name!=xs.scriptargs)
                    a.Switch = a.Name.Replace("xs.","");
            return args;
        }
        
        
        private static Script loadScript(string location,ScriptContext context)
        {
            AppDomainLoader.progress("MainWithContext: Parsing XML");
            try
            {
                byte[] data;
                bool validate = context.GetBool(xs.validate, false);
                if (location == ".")
                {
                    location = loadFromConfig(out data);
                    return context.LoadScript(new MemoryStream(data), location, validate);
                }
                else
                {
                    return context.LoadScript(location, validate);
                }
            }
            finally
            {
                AppDomainLoader.progress("MainWithContext: Parsing completed");
            }
            
        }

        // No inlining, to prevent System.Configuration loading (which takes at least 0.1-0.2s). It's big enough method to be inlined, but just in case
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string loadFromConfig(out byte[] data)
        {
            string location= AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            if (string.IsNullOrEmpty(location))
                location = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
            data = ConfigurationManager.GetSection("xsharper") as byte[];
            if (data == null)
                throw new FileNotFoundException(string.Format("<xsharper> section not found in {0} (use //config switch to specify a different configuration file)",location));
            return location;
        }

    }
}