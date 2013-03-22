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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using XSharper.Core;

namespace XSharper
{
    partial class Program
    {
        private static int updateStage(ScriptContext cout, string[] args)
        {
            string procName = Process.GetCurrentProcess().MainModule.FileName;

            try
            {
                if (args[0] == "overwrite")
                    cout.WriteLine(OutputType.Info, "XSharper update started.");

                cout.WriteLine(OutputType.Info, "Terminating parent process #" + args[1]);
                Process p = Process.GetProcessById(Utils.To<int>(args[1]));
                if (p != null)
                {
                    p.Kill();
                    p.Close();
                }

                Stopwatch sw = Stopwatch.StartNew();
                cout.Write(OutputType.Info, "Waiting for program to close...");
                for (int i = 0; i < 15; ++i)
                {
                    cout.Write(OutputType.Info, ".");
                    Thread.Sleep(1000);

                    try
                    {
                        FileInfo fi = new FileInfo(args[2]);
                        if (!fi.Exists)
                            break;
                        fi.Attributes = FileAttributes.Normal;
                        using (var q = new FileStream(fi.FullName, FileMode.Open, FileAccess.Write, FileShare.None))
                            break;
                    }
                    catch
                    {
                    }
                }
                cout.WriteLine(OutputType.Info);

                switch (args[0])
                {
                    case "overwrite":
                        cout.WriteLine(OutputType.Info, "Copying " + procName + " to " + args[2] + "...");
                        File.Copy(procName, args[2], true);
                        cout.WriteLine(OutputType.Info, "Waiting for 2 seconds...");
                        Thread.Sleep(2000);
                        ProcessStartInfo pi = new ProcessStartInfo
                        {
                            WorkingDirectory = Environment.CurrentDirectory,
                            FileName = args[2],
                            Arguments = "//" + xs.updateStage.Replace("xs.", "") + " delete " + Process.GetCurrentProcess().Id + " " + Utils.QuoteArg(procName),
                            UseShellExecute = false,
                            CreateNoWindow = false
                        };
                        cout.WriteLine(OutputType.Info, "Executing final part...");
                        Process pr = Process.Start(pi);
                        if (pr != null)
                            pr.WaitForExit();
                        return 0;

                    case "delete":
                        cout.WriteLine(OutputType.Info, "Deleting a temporary file " + args[2]);
                        File.Delete(args[2]);
                        Thread.Sleep(2000);
                        cout.WriteLine(OutputType.Info, string.Empty);
                        cout.WriteLine(OutputType.Info);
                        cout.WriteLine(OutputType.Bold, HelpHelper.GetLogo(cout));
                        cout.WriteLine(OutputType.Info, "Update successful. The program will terminate in 5 seconds.");
                        Thread.Sleep(5000);
                        return 0;
                    default:
                        return -1;
                }
            }
            catch (Exception e)
            {
                cout.WriteLine(OutputType.Error, e.Message);
                cout.WriteLine(OutputType.Error, "Software update failed. The program will terminate in 5 seconds.");
                Thread.Sleep(5000);
                return -1;
            }
        }

        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        private static extern bool StrongNameSignatureVerificationEx(string wszFilePath,
                                                                     byte fForceVerification,
                                                                     ref byte pfWasVerified);


        private static int upgrade(ScriptContext cout)
        {
            string procName = Process.GetCurrentProcess().MainModule.FileName;

            string tmp = null;
            try
            {
                cout.WriteLine(OutputType.Bold, HelpHelper.GetLogo(cout));
                cout.WriteLine(OutputType.Info, "Currently executing " + procName);
                if (BitConverter.ToString(Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken()).Length==0)
                {
                    cout.Write(OutputType.Error, "This XSharper build is not digitally signed and cannot be upgraded automatically. Please upgrade manually...");
                    return -1;
                }

                // Find out if upgrade is due
                var current = Assembly.GetExecutingAssembly().GetName().Version;
                cout.Write(OutputType.Info, "Checking the latest XSharper version...");

                using (var wc = new WebClientEx())
                {
                    wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate);

                    var verBytes = wc.DownloadData("http://www.xsharper.com/xsharper-version.txt");
                    var latest = new Version(Encoding.ASCII.GetString(verBytes));
                    cout.WriteLine(OutputType.Info, string.Empty);
                    cout.WriteLine(OutputType.Info, "The latest available version is " + latest);
                    if (latest <= current)
                    {
                        cout.WriteLine("Installed XSharper version is up to date.");
                        return 0;
                    }

                    cout.WriteLine(OutputType.Info, "Downloading the latest XSharper binary...");
                    byte[] exe = wc.DownloadData(Environment.Version.Major>=4?
                        "http://www.xsharper.com/xsharper4.exe" :
                        "http://www.xsharper.com/xsharper.exe");
                    Assembly a=Assembly.Load(exe);


                    tmp = Utils.BackslashAdd(Path.GetTempPath()) + "xsharper" + latest + ".exe";
                    File.WriteAllBytes(tmp, exe);
                    

                    // Verify signature
                    cout.WriteLine(OutputType.Info, "Verifying digital signature...");
                    
                    if (BitConverter.ToString(a.GetName().GetPublicKeyToken()) != BitConverter.ToString(Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken()))
                    {
                        cout.Write(OutputType.Error, "Failed. The downloaded XSharper binary is signed with a different key. Please upgrade manually.");
                        return -1;
                    }
                    cout.WriteLine(OutputType.Info, "Done.");

                    byte wasVerified = 0;
                    if (!StrongNameSignatureVerificationEx(tmp, 1, ref wasVerified))
                    {
                        cout.Write(OutputType.Error, "Downloaded XSharper binary has invalid signature. Upgrade is aborted.");
                        return -1;
                    }

                    cout.WriteLine(OutputType.Info, string.Empty);
                }

                // Run it
                cout.WriteLine(OutputType.Info, "Starting update...");
                var pi = new ProcessStartInfo
                {
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = tmp,
                    Arguments = xs.updateStage.Replace("xs.", "//") + " overwrite " + Process.GetCurrentProcess().Id + " " + Utils.QuoteArg(procName),
                    UseShellExecute = true,
                };
                Process pr = Process.Start(pi);
                if (pr != null)
                    pr.WaitForExit();


                // If the script is successful, this process will be killed and this line never executed
                throw new ScriptRuntimeException("Failed to start upgrade");
            }
            catch (Exception e)
            {
                cout.WriteLine(OutputType.Error, e.Message);
                cout.WriteLine(OutputType.Error, "Software update is cancelled.");
                return -1;
            }
            finally
            {
                if (tmp != null && File.Exists(tmp))
                    File.Delete(tmp);
            }

        }


        private static List<IScriptAction> getCommandlineReferences(ScriptContext context)
        {
            List<IScriptAction> preScript = new List<IScriptAction>();
            if (context.IsSet(xs.@ref))
            {
                foreach (var param in context.GetStr(xs.@ref ?? string.Empty).Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    string r = param.Trim();
                    bool withTypes = false;
                    bool file = false;

                    if (r.StartsWith("#", StringComparison.Ordinal))
                    {
                        withTypes = true;
                        r = r.Substring(1);
                    }
                    if (r.EndsWith(".dll",StringComparison.OrdinalIgnoreCase))
                        file = true;

                    if (!string.IsNullOrEmpty(r))
                    {
                        Reference rr = null;
                        bool addUsing = false;
                        if (r.StartsWith("@", StringComparison.Ordinal))
                        {
                            r = r.Substring(1);
                            addUsing = true;
                        }
                        if (file)
                        {
                            rr = new Reference {From = r, WithTypes = withTypes, Transform = TransformRules.None, AddUsing = addUsing};
                            preScript.Add(new Embed { From = r, IsAssembly = true, Transform = TransformRules.None });
                        }
                        else
                            rr = new Reference { Name = r, WithTypes = withTypes, Transform = TransformRules.None, AddUsing = addUsing };
                        if (withTypes)
                            context.AddAssembly(rr.AddReference(context, true),true);
                        preScript.Add(rr);
                    }
                }
            }
            return preScript;
        }

        
    }
}
