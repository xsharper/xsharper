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
using XS = XSharper.Core;
using System.Threading;

namespace XSharper
{
    #region Application domain loader

    public static class AppDomainLoader
    {
        #region Delegates

        public delegate int DoWithContext(XS.ScriptContext context);
        public delegate int EntryPoint(string[] args);

        #endregion

        public static string BaseDirectory
        {
            get { return s_BaseDirectory; }
            set { s_BaseDirectory = value; }
        }
        public static Assembly ResourceAssembly = Assembly.GetExecutingAssembly();
        private static readonly int s_Progress;

        private const string XsCreateElevatedScriptContext = "//xs.createElevatedScriptContext";
        private const string XSharperDomainPrefix = "XSharperDomain:";
        private static readonly Dictionary<string, Assembly> _embeddedLibraries = new Dictionary<string, Assembly>();
        private static readonly Dictionary<string, Assembly> _loadedLibraries = new Dictionary<string, Assembly>();
        private static string s_BaseDirectory;
        private static System.Diagnostics.Stopwatch s_startTick;
        static AppDomainLoader()
        {
            string s = Environment.GetEnvironmentVariable("XSH_LOADER_DEBUG");
            int n = 0;
            if (!string.IsNullOrEmpty(s) && int.TryParse(s, out n))
                s_Progress = n;
            s_startTick = System.Diagnostics.Stopwatch.StartNew();
        }

        
        public static int? Loader(string[] args)
        {
            progress("==============");
            progress("AppDomainLoader: Loader started with options [" + string.Join(" ", args) + "]");
            progress("==============");
            
            AppDomain newDomain = null;
            string appDomainTempFile = null;
            bool forceAppDomain = shouldForce();
            try
            {
                if (AppDomain.CurrentDomain.FriendlyName.StartsWith(XSharperDomainPrefix))
                {
                    progress("AppDomainLoader: Already in domain");
                    AppDomain.CurrentDomain.AssemblyResolve += resolver;
                    AppDomain.CurrentDomain.AssemblyLoad += loadTracer;

                    progress("AppDomainLoader: args:" + string.Join(" ", args));
                    if (args.Length > 0 && args[0] == XsCreateElevatedScriptContext)
                    {
                        progress("AppDomainLoader: Elevated remote context requested");
                        return createRemotingScriptContext(args);
                    }
                    return null;
                }

                // Load from GAC no problem, but load nothing from the current directory
                // (because script directory and XSharper directory are likely to differ)
                AppDomainSetup setup = new AppDomainSetup();
                setup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                setup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                setup.PrivateBinPathProbe = "true";
                setup.PrivateBinPath = Guid.NewGuid().ToString();
                setup.DisallowApplicationBaseProbing = true;
                

                // Parse command line. If it has config specified, use that config
                string config = null;

                // Check if we have embedded app.config. If yes, we'll use that and ignore whatever
                // is in the system. 
                using (System.IO.MemoryStream appConfig = TryLoadResourceStream(".config"))
                {
                    if (appConfig != null)
                    {
                        appDomainTempFile = System.IO.Path.GetTempFileName();
                        using (System.IO.FileStream fs = new System.IO.FileStream(appDomainTempFile, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                            appConfig.WriteTo(fs);
                        config = appDomainTempFile;
                    }
                    else
                        progress("AppDomainLoader: Embedded config not found");
                }
                args = loadConfigFromCmdLine(args, ref config);

                if (config != null)
                {
                    setup.ConfigurationFile = System.IO.Path.GetFullPath(config);
                    progress("AppDomainLoader: Set config file to " + setup.ConfigurationFile);
                }
                else if (!forceAppDomain)
                {
                    progress("AppDomainLoader: Skipping domain creation");
                    AppDomain.CurrentDomain.AssemblyResolve += resolver;
                    AppDomain.CurrentDomain.AssemblyLoad += loadTracer;
                    return null;
                }
                newDomain = AppDomain.CreateDomain(XSharperDomainPrefix + System.IO.Path.GetRandomFileName(), null, setup);
                return newDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, null, args);
            }
            catch (ThreadAbortException)
            {
                progress("AppDomainLoader: thread is being aborted");
                if (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.AbortRequested)
                    Thread.ResetAbort();
                return -1000;
            }
            catch (Exception ex)
            {
                progress("AppDomainLoader: "+ ex);
                Console.WriteLine("Error: " + ex);
                return -1;
            }
            finally
            {
                if (appDomainTempFile != null)
                    System.IO.File.Delete(appDomainTempFile);

                try
                {
                    if (newDomain != null)
                        AppDomain.Unload(newDomain);
                }
                catch (Exception ex)
                {
                    progress("AppDomainLoader: " + ex);
                    Console.WriteLine("Error: " + ex);
                }
            }
        }


        private static string[] loadConfigFromCmdLine(string[] args, ref string config)
        {
            List<string> newArgs = new List<string>();
            bool last = false;
            for (int i = 0; i < args.Length; ++i)
            {
                string c = args[i];
                if (!last)
                {
                    if (c.StartsWith("//last", StringComparison.OrdinalIgnoreCase))
                    {
                        last = true;
                        continue;
                    }

                    if (c.StartsWith("//config", StringComparison.OrdinalIgnoreCase))
                    {
                        if (c.Substring(8).StartsWith(":", StringComparison.Ordinal) || c.Substring(8).StartsWith("=", StringComparison.Ordinal))
                        {
                            Console.WriteLine("2");
                            config = c.Substring(9);
                            continue;
                        }
                        if (i < args.Length - 1)
                        {
                            config = args[i + 1];
                            i++;
                            continue;
                        }
                    }
                }
                newArgs.Add(c);
            }
            return newArgs.ToArray();
        }

        private static bool shouldForce()
        {
            bool force=true;
            using (System.IO.Stream s = ResourceAssembly.GetManifestResourceStream("XSharper.Embedded.Assemblies.AllStrongName.flag"))
            {
                if (s!=null && s.ReadByte()=='1')
                    force = false;
            }
            if (force)
                progress("AppDomainLoader> There are assemblies without strong name, so new AppDomain is required");
            else
                progress("AppDomainLoader> New AppDomain not required");
            return force;
        }

        public static System.IO.MemoryStream TryLoadResourceStream(string streamName)
        {
            if (ResourceAssembly == null)
                return null;
            foreach (string name in ResourceAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(streamName, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to get as is
                    using (System.IO.Stream s = ResourceAssembly.GetManifestResourceStream(name))
                        if (s != null)
                            return new System.IO.MemoryStream(new System.IO.BinaryReader(s).ReadBytes((int)s.Length));
                }
                if (name.EndsWith(streamName + ".gz", StringComparison.OrdinalIgnoreCase))
                {
                    // M'kay, maybe it's compressed
                    using (System.IO.Stream s = ResourceAssembly.GetManifestResourceStream(name))
                    {
                        if (s != null)
                            using (System.IO.Compression.GZipStream gz = new System.IO.Compression.GZipStream(s, System.IO.Compression.CompressionMode.Decompress, true))
                            {
                                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                                byte[] bytes = new byte[16384];
                                int n;
                                while ((n = gz.Read(bytes, 0, bytes.Length)) != 0)
                                    ms.Write(bytes, 0, n);
                                ms.Position = 0;
                                return ms;
                            }
                    }
                }
            }
            // Better luck next time
            return null;
        }

        
        private static Assembly resolver(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = args.Name;
                progress("resolver : searching for " + name);
                string shortName = new AssemblyName(name).Name;
                progress("resolver : shortName=" + shortName);
                if (shortName == Assembly.GetExecutingAssembly().GetName().Name)
                {
                    progress("resolver : executing assembly returned");
                    return Assembly.GetExecutingAssembly();
                }

                // Try already loaded assemblies
                foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (loaded.FullName == args.Name || loaded.GetName().Name == args.Name)
                    {
                        progress("resolver : assembly is already loaded");
                        return loaded;
                    }

                }

                // Try embedded
                lock (_embeddedLibraries)
                {
                    if (_embeddedLibraries.ContainsKey(shortName))
                    {
                        progress("resolver : returning cached embedded assembly");
                        return _embeddedLibraries[shortName];
                    }
                }

                string streamName = "Embedded.Assemblies." + shortName;
                if (!streamName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    streamName += ".dll";
                progress("resolver : trying to load stream " + streamName);
                System.IO.MemoryStream data = TryLoadResourceStream(streamName);
                if (data != null)
                {   
                    byte[] dataBytes = data.ToArray();
                    progress("resolver: loading embedded " + shortName + " to the current domain");
                    Assembly a = Assembly.Load(dataBytes);

                    lock (_embeddedLibraries)
                    {
                        _embeddedLibraries[shortName] = a;
                    }
                    return a;
                }
                progress("resolver : not embedded");
                if (BaseDirectory != null)
                {
                    lock (_loadedLibraries)
                    {
                        progress("resolver : base directory specified");
                        if (_loadedLibraries.ContainsKey(args.Name))
                        {
                            progress("resolver : it's already loaded");
                            return _loadedLibraries[args.Name];
                        }
                        if (_loadedLibraries.ContainsKey(shortName))
                        {
                            progress("resolver : it's already loaded");
                            return _loadedLibraries[shortName];
                        }
                        string loc = resolveAssemblyDllFromBaseDirectory(args.Name, BaseDirectory);
                        if (loc != null)
                        {
                            Assembly a = Assembly.LoadFrom(loc);
                            if (a != null)
                            {
                                progress("resolver : assembly loaded from base directory.");
                                _loadedLibraries[shortName] = a;
                                _loadedLibraries[args.Name] = a;
                                return a;
                            }
                        }
                        progress("resolver : cannot resolve even with base directory");
                        _loadedLibraries[shortName] = null;
                        _loadedLibraries[args.Name] = null;
                    }
                }
                else
                    progress("resolver : base directory is not specified");
                progress("resolver : could not resolve " + args.Name);
                return null;
            }
            catch (Exception e)
            {
                progress("resolver : " + e.ToString());
                throw;
            }
            finally
            {
                progress("resolver : completed");
            }
        }

        private static string resolveAssemblyDllFromBaseDirectory(string name, string baseDirectory)
        {
            progress("dllFromBase: Resolve  [" + name + "] from " + baseDirectory);
            // Resolve name to the dll as if .exe in baseDirectory was executed
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = baseDirectory;
            setup.DisallowApplicationBaseProbing = false;

            AppDomain dtest = AppDomain.CreateDomain("temp" + Guid.NewGuid(), null, setup);
            progress("dllFromBase: Domain created");
            try
            {
                System.Runtime.Remoting.ObjectHandle v = Activator.CreateInstanceFrom(dtest, Assembly.GetExecutingAssembly().Location, typeof(AppDomainWorker).FullName);
                if (v != null)
                {
                    AppDomainWorker appDomainWorker = v.Unwrap() as AppDomainWorker;
                    progress("dllFromBase: Resolver created");
                    if (appDomainWorker != null)
                    {
                        string s = appDomainWorker.ResolveDllFromAssemblyName(name);
                        progress("dllFromBase: " + name + " >> " + s);
                        return s;
                    }
                }
                progress("dllFromBase: Resolve  " + name + " failed");
                return null;
            }
            finally
            {
                AppDomain.Unload(dtest);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeConsole();


        public static void progress(string s)
        {
            if (s_Progress!=0)
            {
                string s1 = s_startTick.Elapsed.TotalMilliseconds.ToString("F1").PadLeft(10)+ ": AD=" + AppDomain.CurrentDomain.FriendlyName + " : ";
                if (s_Progress>1)
                    Console.WriteLine(s1 + s);

                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Log(0, s1, s + Environment.NewLine);
                else
                    OutputDebugString(s1 + s + Environment.NewLine);
            }
        }

        private static void loadTracer(object sender, AssemblyLoadEventArgs args)
        {
            progress("load: " + args.LoadedAssembly.FullName + " from " + args.LoadedAssembly.Location);
        }

        private static void registerChannel(string name, string port)
        {
            System.Runtime.Remoting.Channels.BinaryServerFormatterSinkProvider serverProv = new System.Runtime.Remoting.Channels.BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            System.Runtime.Remoting.Channels.BinaryClientFormatterSinkProvider clientProv = new System.Runtime.Remoting.Channels.BinaryClientFormatterSinkProvider();
            System.Collections.IDictionary properties = new System.Collections.Hashtable();
            properties["name"] = name;
            properties["priority"] = "20";
            properties["portName"] = port;
            properties["secure"] = true;
            properties["tokenImpersonationLevel"] = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            properties["includeVersions"] = false;
            properties["strictBinding"] = false;
            System.Security.Principal.SecurityIdentifier sidAdmin = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.InteractiveSid, null);
            System.Security.Principal.NTAccount nt = (System.Security.Principal.NTAccount) sidAdmin.Translate(typeof (System.Security.Principal.NTAccount));
            progress("registerChannel: " + port + " with authorized group " + nt);
            properties["authorizedGroup"] = nt.Value;
        
            System.Runtime.Remoting.Channels.Ipc.IpcChannel ipcCh = new System.Runtime.Remoting.Channels.Ipc.IpcChannel(properties, clientProv, serverProv);
            System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(ipcCh, true);
        }

        private static int createRemotingScriptContext(string[] args)
        {
            progress("remoteClient: createRemotingScriptContext");
            for (int i = 0; i < args.Length;++i )
                progress("remoteClient: arg"+i+": ["+args[i]+"]");
            try
            {
                progress("remoteClient: Setting up ipc client");
                Random r=new Random();
                registerChannel(args[1], args[1] + ":"+r.Next(9000,15000));
                if (!string.IsNullOrEmpty(args[2]))
                    BaseDirectory = args[2];
                RemotingCallback callback = (RemotingCallback)Activator.GetObject(typeof(RemotingCallback), "ipc://" + args[1] + "/RemotingCallback");

                progress("remoteClient: attaching console");
                uint pid = uint.Parse(args[3]);
                if (pid != 0)
                {
                    FreeConsole();
                    bool b=AttachConsole(pid);
                    if (b)
                        progress("remoteClient: attaching console was successful");
                    else
                        progress("remoteClient: attaching console failed");
                }
                progress("remoteClient: ipc setup. About to instantiate context");
                XS.ScriptContext sc = new XS.ScriptContext(ResourceAssembly);
                if (sc.IsAdministrator)
                {
                    progress(sc.GetType().FullName + " instantiated");

                    using (XS.ConsoleCtrl ctrl = new XS.ConsoleCtrl())
                    {
                        // Ignore Ctrl+C
                        ctrl.ControlEvent += delegate(object sender, XS.ConsoleCtrlEventArgs e)
                            {
                                progress("remoteClient: Ctrl+C received");
                                try
                                {
                                    sc.Abort();
                                }
                                catch
                                {
                                    
                                }
                            };
                        return callback.OnContextReady(sc);
                    }
                }
                progress("remoteClient: Administrator privileges required!");
            }
            catch (Exception e)
            {
                progress("remoteClient: " + e);
                throw;
            }

            return -1;
        }

        public static int RunWithElevatedContext(DoWithContext whatToCall, bool hideWindow)
        {


            progress("rwec: started");

            System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotingCallback), "RemotingCallback", System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            RemotingCallback.Callback = whatToCall;

            // Prepare channel
            string channelName = "XSharper" + Guid.NewGuid();
            registerChannel(channelName, channelName);

            // Prepare runas command line
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string arg = string.Empty;
            if (AppDomain.CurrentDomain.SetupInformation.ConfigurationFile != null)
            {
                arg += ("//config ");
                arg += ("\"" + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile + "\" ");
            }
            arg += XsCreateElevatedScriptContext + " " + channelName + " \"" + BaseDirectory.TrimEnd('\\', '/') + "\" " + System.Diagnostics.Process.GetCurrentProcess().Id + " *** EXECUTED FROM *** " + Environment.CommandLine;
            progress("rwec: arguments: [" + arg + "]");
            startInfo.Arguments = arg;
            startInfo.Verb = "runas";
            startInfo.WindowStyle = hideWindow ? System.Diagnostics.ProcessWindowStyle.Hidden : System.Diagnostics.ProcessWindowStyle.Normal;

            // run
            try
            {
                progress("rwec: Starting " + startInfo.FileName + " " + startInfo.Arguments);
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(startInfo);
                if (p != null)
                {
                    progress("rwec: started");
                    p.WaitForExit();
                    progress("rwec: terminated with exit code " + p.ExitCode);
                    return p.ExitCode;
                }
                progress("rwec: failed to start");
                return -1;
            }
            catch (Exception e)
            {
                progress("rwec: " + e);
                return -2;
            }

        }

        #region Nested type: AppDomainWorker

        public class AppDomainWorker : MarshalByRefObject
        {
            public override object InitializeLifetimeService()
            {
                return null;
            }

            public void InitDomain(string resource)
            {
                progress("dllResolver: Initializing domain " + AppDomain.CurrentDomain.FriendlyName);
                if (resource != null && resource != ResourceAssembly.Location)
                {
                    // We just load a file, w/o dependencies, for resource purposes
                    progress("dllResolver: Loading " + resource);
                    ResourceAssembly = Assembly.LoadFrom(resource);
                }
                AppDomain.CurrentDomain.AssemblyResolve += AppDomainLoader.resolver;
                AppDomain.CurrentDomain.AssemblyLoad += AppDomainLoader.loadTracer;
                AppDomain.CurrentDomain.DomainUnload += delegate(object sender, EventArgs e)
                {
                    progress("dllResolver: Terminating domain " + AppDomain.CurrentDomain.FriendlyName);
                };
                progress("dllResolver: domain initialized " + AppDomain.CurrentDomain.FriendlyName);
            }


            public string ResolveDllFromAssemblyName(string name)
            {
                try
                {
                    progress("dllResolver: ResolveDllFromAssemblyName: " + name);
                    Assembly a = Assembly.ReflectionOnlyLoad(name);
                    if (a != null)
                        return a.Location;
                }
                catch
                {
                }
                finally
                {
                    progress("dllResolver: ResolveDllFromAssemblyName completed");
                }
                return null;
            }
        }

        #endregion

        #region Nested type: RemotingCallback

        public class RemotingCallback : MarshalByRefObject
        {
            public static DoWithContext Callback;

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public int OnContextReady(XS.ScriptContext context)
            {
                if (Callback != null)
                    return Callback(context);
                return -1;
            }
        }

        #endregion
    }

    #endregion
}
