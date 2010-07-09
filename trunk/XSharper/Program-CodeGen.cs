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
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using XSharper.Core;
using System.Collections.Generic;

namespace XSharper
{
    partial class Program
    {

        private static void genDemoConfig(ConsoleWithColors consoleWithColors, ScriptContext context)
        {
            Script script = getSampleScript(context);
            if (context.IsSet(xs.gensample))
            {
                using (new ScriptContextScope(context))
                    script.Save(context.GetString(xs.gensample));
                consoleWithColors.WriteLine(OutputType.Info, string.Format("{0} created.", context[xs.gensample]));
            }

            if (!context.IsSet(xs.genconfig))
                return;
            string path = context.GetString(xs.genconfig);
            using (XmlTextWriter tw = new XmlTextWriter(path, Encoding.UTF8))
            {
                tw.Formatting = Formatting.Indented;
                tw.WriteStartDocument();
                tw.WriteStartElement("configuration");
                tw.WriteStartElement("configSections");
                tw.WriteStartElement("section");
                tw.WriteAttributeString("name", "xsharper");
                tw.WriteAttributeString("type", typeof(ScriptSectionHandler).FullName + "," + typeof(ScriptSectionHandler).Assembly.GetName().Name);
                tw.WriteEndElement();
                tw.WriteEndElement();
                tw.WriteComment("  Connection strings  ");
                tw.WriteStartElement("connectionStrings");
                tw.WriteStartElement("add");
                tw.WriteAttributeString("name", "mydb");
                tw.WriteAttributeString("connectionString", @"Data Source=localhost\SQLEXPRESS;Initial Catalog=mydb;Integrated Security=True;");
                tw.WriteEndElement();
                tw.WriteEndElement();
                tw.WriteComment(" ** XSharper script ** ");
                using (new ScriptContextScope(context))
                    script.Save(tw);
                tw.WriteEndElement();
                tw.WriteEndDocument();
            }
            consoleWithColors.WriteLine(OutputType.Info, string.Format("{0} created.", path));
        }

        private static Script getSampleScript(ScriptContext context)
        {
            Script script = new Script();
            script.Id = "WAIT";
            script.VersionInfo.Title = "WAIT";
            script.VersionInfo.Value = "(Generated sample script)";
            script.EngineVersion = context.CoreVersion.ToString();
            script.Parameters.Add(
                new CommandLineParameter { Default = "2000", Value = "Number of seconds to wait", Description = "seconds", Switch = "seconds", Count = CommandLineValueCount.Single }
                );
            script.Parameters.Add(
                new CommandLineParameter { Default = "0", Value = "Display this message", Switch = "help", Synonyms = "?;helpme", Unspecified = "true" }
                );


            script.Add(new Code { Value = "Context.WriteLine(\"Hello, world!\");" });
            script.Add(new Code { Value = "Context.WriteLine(\"This script will be waiting for {0} seconds.\",c[\"seconds\"]);" });
            Timer timer = new Timer { OutTo = "execTime", Format = TimerFormat.TimeSpan };
            timer.Add(new Sleep { Timeout = "${seconds}" });
            script.AddTry(timer);
            script.AddTry(new Print { Value="Execution took ${execTime}"});

            script.AddTry(new Throw { Value = "Test error" });
            script.AddTry(new Print { Value = "This will not be printed" });
            script.AddCatch(new Print { Value = "Catched exception: ${=c.CurrentException.Message}" });
            script.AddFinally(new Print { Value = "Clean up part" });
            return script;
        }

        private static void doCodeGeneration(ScriptContext context, Script script)
        {
            if (!isCodeGeneration(context))
                return;

            context.Compiler.AddReference(null,typeof(System.Runtime.Remoting.Channels.Ipc.IpcChannel).Assembly.FullName,false,false,null);

            if (context.IsSet(xs.save) && script != null)
            {
                context.WriteLine(OutputType.Info, string.Format("Saving script to {0} ...", context[xs.save]));
                using (new ScriptContextScope(context))
                    script.Save(context.GetString(xs.save));
                context.WriteLine(OutputType.Info, string.Format("Script file {0} saved...", context[xs.save]));
            }

            if (context.IsSet(xs.genxsd))
            {
                context.WriteLine(OutputType.Info, string.Format("Generating XML schema  ..."));

                XmlSchema x = generateSchema(context);
                string sf = context.GetString(xs.genxsd);
                sf=Path.GetFullPath((sf == "*") ? x.Id + ".xsd" : sf);
                using (StreamWriter target = new StreamWriter(sf, false))
                    x.Write(target);

                context.WriteLine(OutputType.Info, string.Format("XML schema saved to {0} ...", sf));
            }

            // Generate source code
            StringWriter source = new StringWriter();
            string entryPoint = null;
            if (script != null && (context.IsSet(xs.genlibrary) || context.IsSet(xs.genexe) || context.IsSet(xs.genwinexe) || context.IsSet(xs.gencs)))
            {
                context.WriteLine(OutputType.Info, "Generating C# source code...");
                SharpCodeGenerator codeGenerator = new SharpCodeGenerator(context.Compiler);

                if (context.IsSet(xs.@namespace))
                    codeGenerator.Namespace = context.GetString(xs.@namespace);

                string baseName = Path.GetFileNameWithoutExtension(script.Location).ToLower();
                if (script.Id!=null)
                    baseName = script.Id;
                baseName = Utils.FixFilename(baseName);
                
                if (context.IsSet(xs.@class))
                    codeGenerator.Class = context.GetString(xs.@class);
                else
                {
                    string cl;
                    cl = baseName;
                    if (char.IsDigit(cl[0]))
                        cl = "C" + cl;
                    if (!char.IsUpper(cl[0]))
                        cl = cl.Substring(0, 1).ToUpperInvariant() + cl.Substring(1);
                    cl = SharpCodeGenerator.ToValidName(cl);
                    if (cl == "Script" || cl == "Run")
                        cl = "C" + cl;
                    codeGenerator.Class = cl;
                }

                string pref = string.Empty;
                if (!string.IsNullOrEmpty(context.CodeOutputDirectory))
                    pref = Path.Combine(context.CodeOutputDirectory, "bin\\Debug\\");
                if (context.IsSet(xs.genexe) && context.GetString(xs.genexe) == "*")
                    context[xs.genexe] = pref+baseName + ".exe";
                if (context.IsSet(xs.genwinexe) && context.GetString(xs.genwinexe) == "*")
                    context[xs.genwinexe] = pref + baseName + ".exe";
                if (context.IsSet(xs.genlibrary) && context.GetString(xs.genlibrary) == "*")
                    context[xs.genlibrary] = pref + baseName + ".dll";
                if (context.IsSet(xs.gencs) && context.GetString(xs.gencs) == "*")
                    context[xs.gencs] = baseName + ".cs";
                
                GeneratorOptions options = GeneratorOptions.None;
                if (context.IsSet(xs.genexe))
                    options |= GeneratorOptions.IncludeSource | GeneratorOptions.ForExe | GeneratorOptions.CreateMain;
                if (context.IsSet(xs.genwinexe))
                    options |= GeneratorOptions.IncludeSource | GeneratorOptions.ForExe | GeneratorOptions.CreateMain | GeneratorOptions.WinExe;
                if (context.IsSet(xs.genlibrary))
                    options |= GeneratorOptions.IncludeSource | GeneratorOptions.ForExe;
                if (context.GetBool(xs.main, false))
                    options |= GeneratorOptions.CreateMain;
                if (context.GetBool(xs.forcenet20, false))
                    options |= GeneratorOptions.ForceNet20;
                if (context.CodeOutputDirectory == null && !context.IsSet(xs.gencs))
                    options |= GeneratorOptions.ForceNet20; // this is a bit faster
                if (context.GetBool(xs.noSrc, false))
                    options &= ~GeneratorOptions.IncludeSource;
                
                
                codeGenerator.Generate(context, source, script, options);
                if (context.IsSet(xs.genexe) || context.IsSet(xs.genwinexe))
                    entryPoint = codeGenerator.Namespace + "." + codeGenerator.Class + "Program";
            }

            // Save it to disk, if necessary
            string code = source.GetStringBuilder().ToString();
            if (script != null && context.IsSet(xs.gencs))
            {
                using (StreamWriter sourceDisk = new StreamWriter(context.GetString(xs.gencs), false))
                {
                    sourceDisk.Write(code);
                }
                context.WriteLine(OutputType.Info, string.Format("C# source code saved to {0} ...", context[xs.gencs]));

            }

            // Load the other part from resources
            if (script != null && (context.IsSet(xs.genexe) || context.IsSet(xs.genwinexe) || context.IsSet(xs.genlibrary)))
            {
                CompiledOutputType outType;
                string e;
                if (context.IsSet(xs.genexe))
                {
                    e = context.GetString(xs.genexe);
                    outType = CompiledOutputType.ConsoleExe;
                }
                else if (context.IsSet(xs.genwinexe))
                {
                    e = context.GetString(xs.genwinexe);
                    outType = CompiledOutputType.WindowsExe;
                }
                else
                {
                    e = context.GetString(xs.genlibrary);
                    outType = CompiledOutputType.Library;
                }

                context.WriteLine(OutputType.Info, string.Format("Compiling {0}...", e));

                
                var copt = new CompileOptions
                    {
                        ExtraOptions = context.GetString(xs.compilerOptions, null),
                        CodeOutputDirectory=context.CodeOutputDirectory,
                        StreamProvider=context.FindResourceMemoryStream,
                        FilesToEmbed = context.GetFilesToEmbed(),
                        EntryPoint=entryPoint,
                    };
                if (context.IsSet(xs.genexe) || context.IsSet(xs.genwinexe))
                {
                    if (script.RequireAdmin)
                    {
                        copt.Compiled = AppDomainLoader.TryLoadResourceStream(@"Manifests.requireAdministrator.res").ToArray();
                        copt.Manifest = AppDomainLoader.TryLoadResourceStream(@"Manifests.requireAdministrator.manifest").ToArray();
                    }
                    else
                    {
                        copt.Compiled = AppDomainLoader.TryLoadResourceStream(@"Manifests.asInvoker.res").ToArray();
                        copt.Manifest = AppDomainLoader.TryLoadResourceStream(@"Manifests.asInvoker.manifest").ToArray();
                    }
                    if (context.IsSet(xs.icon))
                        copt.Icon = context.ReadBytes(context.GetStr(xs.icon));
                    else
                        copt.Icon = AppDomainLoader.TryLoadResourceStream(@"Source.xsh.ico").ToArray();
                }


                // If we're building .EXE, add a reference to ZipLib. We don't want to do it 
                // unnecessarily to save time, and also allow XSharper.Core use w/o ZipLib, so the reference is added only if it's loaded
                    
                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                    if (ass.FullName != null && ass.FullName.Contains("ICSharpCode.SharpZipLib"))
                    {
                        context.Compiler.AddReference(null, ass.FullName, true, false,null);
                        break;
                    }
            
                context.Compiler.Compile(outType,code,e,copt);
                context.WriteLine(OutputType.Info, string.Format("Executable saved to {0} ...", e));
            }
        }

        
        

 

        private static bool isCodeGeneration(ScriptContext context)
        {
            return (context.IsSet(xs.genexe) ||
                    context.IsSet(xs.genwinexe) ||
                    context.IsSet(xs.genlibrary) ||
                    context.IsSet(xs.gencs) ||
                    context.IsSet(xs.genxsd) ||
                    context.IsSet(xs.save));
        }

        private static Script createEmptyScript(ScriptContext context, string id)
        {
            Script s = context.CreateNewScript(Process.GetCurrentProcess().MainModule.FileName);
            s.Id = id;
            s.EngineVersion = context.CoreVersion.ToString();

            if (context.Compiler.DefaultNETVersion>new Version(3,5))
                s.NetVersion = "3.5";
            else
                s.NetVersion = context.Compiler.DefaultNETVersion.ToString();

            return s;
        }

        private static XmlSchema generateSchema(ScriptContext context)
        {
            // Ensure that all objects are from one namespace
            string ns = null;
            foreach (var type in context.GetKnownTypes())
            {
                var na = (CustomAttributeHelper.First<XsTypeAttribute>(type));
                if (na == null)
                    continue;

                if (ns == null && na.Namespace != ScriptActionBase.XSharperNamespace)
                    ns = na.Namespace;
                else if (ns != na.Namespace && na.Namespace != ScriptActionBase.XSharperNamespace)
                    context.Info.WriteLine("Warning: Object " + na.Name + " has unexpected namespace " + na.Namespace + ". All types must use the same namespace.");
            }

            if (ns == null)
                ns = ScriptActionBase.XSharperNamespace;

            var xmlSchema = XsXsdGenerator.BuildSchema(ns, context.GetKnownTypes(), typeof(Script), new Type[] { typeof(IScriptAction) });
            xmlSchema.Id = ns == ScriptActionBase.XSharperNamespace ? "xsharper" : "custom";

            // Compile it, just to make sure it's valid
            XmlSchemaSet s = new XmlSchemaSet();
            s.Add(xmlSchema);
            s.Compile();

            return xmlSchema;
        }
        private static Script getInlineScript(ScriptContext context, List<string> filteredArgs)
        {
            string name = null;
            string vn = null;
            if (context.IsSet(xs.execRest))
            {
                name = "//";
                vn = xs.execRest;
            }
            else if (context.IsSet(xs.execRestD))
            {
                name = "//#";
                vn = xs.execRestD;
            }
            else if (context.IsSet(xs.execRestP))
            {
                name = "//p";
                vn = xs.execRestP;
            }
            if (name!=null)
            {
                string c = Environment.CommandLine;
                int n = c.IndexOf("/" + name, StringComparison.OrdinalIgnoreCase);
                if (n == -1)
                    throw new ScriptRuntimeException("Invalid command line");
                string s = c.Substring(n + name.Length + 1).Trim();
                if (s.Length>0 && s[0]=='"')
                {
                    var args=context.GetArrayT<string>(vn);
                    s = args[0];
                    filteredArgs.Clear();
                    for (int i = 1; i < args.Length;++i)
                        filteredArgs.Add(args[i]);
                }
                if (context.IsSet(xs.execRestD) && s[0] == '?')
                    s = s.Substring(1);
                if (context.IsSet(xs.execRestD))
                    return execGenerator(context, s, "c.Dump(", ")");
                if (context.IsSet(xs.execRestP))
                    return execGenerator(context, s, "c.WriteLine(", ")");
                return execGenerator(context, s, null, null);
            }
            if (context.IsSet(xs.download))
                return genDownload(context, filteredArgs);
            if (context.IsSet(xs.zip))
                return genZip(context, filteredArgs);
            if (context.IsSet(xs.unzip))
                return genUnzip(context, filteredArgs);
            if (context.GetBool(xs.version, false))
                return genVersion(context);

            return null;
        }


        private static Script genVersion(ScriptContext context)
        {

            Script script = createEmptyScript(context, "xsharper //version"); ;
            script.Id = "version";
            script.Add(new Set("process","${=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}"));
            script.Add(new Set("net","${=string.Join(', ',XS.Utils.GetInstalledNETVersions())}"));
            script.Add(new Print { OutTo = "^bold", Value = HelpHelper.GetLogo(context) });
            script.Add(new Print());
            script.Add(new Print {Transform = TransformRules.Trim | TransformRules.Expand, NewLine = false, Value = @"
Environment: 
====================
Operating system    : ${=Environment.OSVersion.VersionString}
.NET Framework      : ${net}
Current directory   : ${=.CurrentDirectory}
Privileges          : ${=.IsAdministrator?'Administrator':'Not administrator (use //requireAdmin)'}
XSharper executable : ${process}
Configuration file  : ${=AppDomain.CurrentDomain.SetupInformation.ConfigurationFile} ${=File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)?'':'(does not exist)'}

Environment variables: 
=======================
XSH_PATH            : ${%XSH_PATH%|''}
XSH_REF             : ${%XSH_REF%|''}
XSH_COLORS          : ${%XSH_COLORS%|''}"
            });
            script.Add(new Print());
            return script;
        }

        private static Script genUnzip(ScriptContext context, List<string> filteredArgs)
        {
            Script script;
            string[] s = context.GetStringArray(xs.unzip);
            if (s.Length < 1 || s.Length > 3)
                throw new ArgumentException("Invalid arguments to " + xs.unzip + " command");

            script = createEmptyScript(context, "xsharper //unzip");
            script.Id = "unzip";
            filteredArgs.AddRange(s);

            script.Usage.Options = UsageOptions.IfHelp | UsageOptions.IfNoArguments | UsageOptions.UsageLine | UsageOptions.AutoSuffix;
            script.Parameters.Add(new CommandLineParameter("zip", CommandLineValueCount.Single, null, null) { Required = true, Description = "archive.zip", Value = "Zip archive to extract" });
            script.Parameters.Add(new CommandLineParameter("destination", CommandLineValueCount.Single, ".", null) { Description = "directory", Value = "Directory where to decompress files" });
            script.Parameters.Add(new CommandLineParameter("filter", CommandLineValueCount.Single, "*.*", null) { Description = "filter", Value = "File wildcard" });
            script.Parameters.Add(new CommandLineParameter("dirfilter", CommandLineValueCount.Single, "*", null) { Description = "directory-filter", Value = "Directory wildcard" });
            script.Parameters.Add(new CommandLineParameter(null, "zipTime", CommandLineValueCount.Single, "fileTime", null) { Synonyms = "zt", Value = "How to process time for files created in zip entries ( fileTime/utcFileTime = set to entry time, now/utcNow =ignore)" });
            script.Parameters.Add(new CommandLineParameter(null, "overwrite", CommandLineValueCount.Single, OverwriteMode.Always.ToString(), null) { Synonyms="o",Value = "Overwrite mode" });
            script.Parameters.Add(new CommandLineParameter(null, "password", CommandLineValueCount.Single, null, null) { Synonyms = "p", Value = "Archive password" });
            script.Parameters.Add(new CommandLineParameter(null, "ignore", CommandLineValueCount.None, "0", "1") { Synonyms = "i", Value = "Ignore errors" });
            script.Parameters.Add(new CommandLineParameter(null, "hidden", CommandLineValueCount.None, "0", "1") { Synonyms = "i", Value = "Extract hidden files" });

            script.Add(new PathOperation { Value = "${zip}", Operation = PathOperationType.GetFullPath, OutTo = "zip", Existence = Existence.FileExists});
            script.Add(new If(new Set("zip", "${=Path.ChangeExtension(${zip},'.zip')}"))
            {
                IsEmpty = "${=Path.GetExtension(${zip})}"
            });
            script.Add(new PathOperation { Value= "${destination}", Operation = PathOperationType.ToDirectoryInfo, Backslash = BackslashOption.Add, OutTo = "destination" });
            script.Add(new SetAttr("z", "zipTime", "${zipTime}"));
            script.Add(new SetAttr("z", "overwrite", "${overwrite}"));
            script.Add(new SetAttr("z", "hidden", "${hidden}"));

            script.Add(new Print { Value = "Extracting ${zip} => ${destination} ... " });

            Unzip z = new Unzip()
                          {
                              Id="z",
                              From = "${zip}",
                              To = "${destination}",
                              Transform = TransformRules.Expand,
                              Syntax = FilterSyntax.Auto,
                              Filter = "${filter}",
                              DirectoryFilter = "${dirfilter}",
                              Password = "${password|=null}"
                          };
            z.Add(new Print { Value = "  ${to}", OutTo = "^info" });
            
            If f=new If(new[] { new Print("Failed: '${}' : ${=c.CurrentException.Message}") { OutTo = "^error"}});
            f.IsTrue = "${ignore}";
            f.AddElse(new Throw());
            z.AddCatch(f);

            script.Add(z);
            script.Add(new Print { Value = "Completed" });
            return script;
        }

        private static Script genZip(ScriptContext context, List<string> filteredArgs)
        {
            Script script;
            script = createEmptyScript(context,"xsharper //zip");
            script.Id = "zip";
            filteredArgs.AddRange(context.GetStringArray(xs.zip));
            script.Usage.Options = UsageOptions.IfHelp | UsageOptions.IfNoArguments | UsageOptions.UsageLine | UsageOptions.AutoSuffix;
            script.Parameters.Add(new CommandLineParameter("zip", CommandLineValueCount.Single, null, null) { Required = true, Description = "archive.zip", Value = "Zip archive to create" });
            script.Parameters.Add(new CommandLineParameter("source", CommandLineValueCount.Single, ".",null) { Description = "directory", Value = "Directory to compress" });
            script.Parameters.Add(new CommandLineParameter("filter", CommandLineValueCount.Single, "*.*", null) { Description = "filter", Value = "File wildcard" });
            script.Parameters.Add(new CommandLineParameter("dirfilter", CommandLineValueCount.Single, "*", null) { Description = "directory-filter", Value = "Directory wildcard" });
            script.Parameters.Add(new CommandLineParameter(null,"zipTime", CommandLineValueCount.Single, "fileTime", null) { Synonyms="zt", Value = "What time to store in zip ( fileTime/utcFileTime/now/utcNow )" });
            script.Parameters.Add(new CommandLineParameter(null,"password", CommandLineValueCount.Single, null, null) { Synonyms = "p", Value = "Archive password" });
            script.Parameters.Add(new CommandLineParameter(null,"recursive", CommandLineValueCount.None, "0", "1") { Synonyms = "r", Value = "Recursive" });
            script.Parameters.Add(new CommandLineParameter(null, "ignore", CommandLineValueCount.None, "0", "1") { Synonyms = "i", Value = "Ignore errors" });
            script.Parameters.Add(new CommandLineParameter(null,"emptyDirectories", CommandLineValueCount.None, "0", "1") { Value = "Include empty directories" });
            script.Parameters.Add(new CommandLineParameter(null, "hidden", CommandLineValueCount.None, "0", "1") { Synonyms = "i", Value = "Extract hidden files" });
                
            script.Add(new PathOperation {Value = "${zip}", Operation = PathOperationType.GetFullPath, OutTo = "zip"});
            script.Add(new If(new Set( "zip", "${=Path.ChangeExtension(${zip},'.zip')}"))
                {
                    IsEmpty = "${=Path.GetExtension(${zip})}"
                });
            
            script.Add(new SetAttr("z", "zipTime", "${zipTime}"));
            script.Add(new SetAttr("z", "recursive", "${recursive}"));
            script.Add(new SetAttr("z", "hidden", "${hidden}"));
            script.Add(new SetAttr("z", "emptyDirectories", "${emptyDirectories}"));
            script.Add(new Print("Compressing ${source} => ${zip} ... "));
            Zip z=new Zip {
                              Id="z",
                              From = "${source}",
                              To = "${zip}",
                              Transform = TransformRules.Expand,
                              Recursive = true,
                              Syntax = FilterSyntax.Wildcard,
                              Filter = "${filter}",
                              DirectoryFilter = "${dirfilter}",
                              Password = "${password|=null}"
                          };
            If oif = new If(new Print { Value = "  ${from}", OutTo = "^info" })
                        {
                             IsTrue = "${=$.IsFile}"
                         };

            If f = new If(new[] { new Print("Failed: '${}' : ${=c.CurrentException.Message}") { OutTo = "^error" } });
            f.IsTrue = "${ignore}";
            f.AddElse(new Throw());
            z.AddCatch(f);

            z.Add(oif);
            script.Add(z);
            script.Add(new Print { Value = "Completed" });
            return script;
        }

        private static Script genDownload(ScriptContext context, List<string> filteredArgs)
        {
            Script script;
            script = createEmptyScript(context,"xsharper //download");
            script.Id = "download";
            filteredArgs.AddRange(context.GetStringArray(xs.download));
            script.Usage.Options = UsageOptions.IfHelp | UsageOptions.IfNoArguments | UsageOptions.UsageLine | UsageOptions.AutoSuffix;
            script.Parameters.Add(new CommandLineParameter("uri", CommandLineValueCount.Single, null, null) { Required = true,  Value = "Source URL" });
            script.Parameters.Add(new CommandLineParameter("file", CommandLineValueCount.Single, ".",null) { Value = "Destination file or directory" });
            script.Parameters.Add(new CommandLineParameter(null, "cache", CommandLineValueCount.Single, Utils.LowercaseFirstLetter(RequestCacheLevel.Default.ToString()), null) { Description = "cache-level", Value = "Cache level, one of "+Utils.AllEnumValuesToString(typeof(RequestCacheLevel)," / ") });
            script.Parameters.Add(new CommandLineParameter("passive", "activeFtp", CommandLineValueCount.None, true, false) { Value = "Use active FTP" });
            script.Parameters.Add(new CommandLineParameter("userAgent","userAgent", CommandLineValueCount.Single, null,null) { Value = "User agent string (http://)" });
            script.Parameters.Add(new CommandLineParameter("post", "post", CommandLineValueCount.Single, null, null) { Value = "HTTP Post string (evaluated as XSharper expression)"});
            script.Parameters.Add(new CommandLineParameter("postContentType", "postContentType", CommandLineValueCount.Single, null, null) { Value = "HTTP Post content type (default is application/x-www-form-urlencoded)" });
            script.Parameters.Add(new CommandLineParameter("timeout", "timeout", CommandLineValueCount.Single, null, null) { Value = "Timeout" });
            script.Parameters.Add(new CommandLineParameter("ignoreCertificateErrors", "ignoreCertificateErrors", CommandLineValueCount.None, true, false) { Value = "Ignore SSL certificate errors " });
                
            script.Add(new Set("oldReceived", "0"));
            script.Add(new SetAttr("d","cacheLevel","${cache}"));
            script.Add(new SetAttr("d", "passiveFtp", "${passive}"));
            script.Add(new Set("fn", "${=XS.Download.UrlToLocalFilename($uri,$file)}",TransformRules.Expand));
            script.Add(new Print { Value = "Downloading ${=XS.Utils.SecureUri($uri)} => ${fn} ... "});
            If ifs1 = new If()  {   IsTrue = "${ignoreCertificateErrors}"};

            ifs1.Add(new Code("System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };") { Dynamic = true });
            script.Add(ifs1);
            Download d = new Download
                             {
                                 Id = "d",
                                 From="${uri}",
                                 To = "${file}",
                                 UserAgent = "${userAgent|=null}",
                                 Post = "${=.Expand(${post|=null})}",
                                 PostContentType = "${postContentType|=null}",
                                 Timeout = "${timeout|=null}",
                                 Transform = TransformRules.Expand
                             };

            If ifs = new If()
                {
                    Condition = "${= $.bytesReceived/1024/100 #GT# $oldReceived/1024/100}"
                };
            ifs.Add(new Print(".") { OutTo="^info" , NewLine = false});
            d.Add(ifs);


            d.Add(new Set("oldReceived","${= $.bytesReceived}",TransformRules.Expand));
            
                        
            script.Add(d);
            script.Add(new Print { Value = "Completed. ${oldReceived} bytes downloaded." });
            return script;
        }

        private static Script execGenerator(ScriptContext context, string c, string prefix, string suffix)
        {
            Script script = createEmptyScript(context,"generated");
            if (context.Compiler.DefaultNETVersion>=new Version(3,5))
            {
                // Yeah, I know it's deprecated, and it's best to use full assembly names, but
                // but I don't really want to scan GAC myself
#pragma warning disable 618
               context.AddAssembly(Assembly.LoadWithPartialName("System.Xml.Linq"),false);
               context.AddAssembly(Assembly.LoadWithPartialName("System.Core"),false);
#pragma warning restore 618
            }
            script.Add(new Rem { Text = "----- Generated from command line -------"+Environment.NewLine+"\t"+ c + Environment.NewLine+"\t ------------"});
            script.Parameters.Add(new CommandLineParameter("arg", CommandLineValueCount.Multiple) {Last = true, Default = "",Required = false});
            script.UnknownSwitches = true;

            c = c.Trim();
            if (c.StartsWith("\"", StringComparison.Ordinal) && c.EndsWith("\"", StringComparison.Ordinal))
                c = c.Substring(1, c.Length - 2);
            if (c == "-")
                c = Console.In.ReadToEnd();
            if (c.StartsWith("="))
            {
                prefix = "=" + prefix;
                c = "${=" + c.Substring(1) + "}";
            }
            if (prefix!=null || suffix!=null)
                c = prefix + Environment.NewLine + c + Environment.NewLine + suffix;
            c = c.Replace("`", "\"");

            AppDomainLoader.progress("Read script begin");
            using (new ScriptContextScope(context))
                script.Load(c);
            AppDomainLoader.progress("Read script end");
            return script;
        }
    }
}
