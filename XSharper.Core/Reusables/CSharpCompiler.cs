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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.CSharp;
using System.Xml;

namespace XSharper.Core
{
    /// What type of module should be generated
    public enum CompiledOutputType
    {
        /// internal in-memory assembly
        InMemoryAssembly,

        /// .NET class library
        Library,

        /// Console executable
        ConsoleExe,

        /// Windows executable
        WindowsExe
    }

    /// Information about embedded file
    public class EmbeddedFileInfo
    {
        /// Stream name, how script will access it
        public string StreamName { get; set;}

        /// Stream location in the filesystem
        public string Location { get; set; }

        /// true, if file is assembly that will be loaded 
        public bool IsAssembly { get; set; }
    }

    /// Internal attribute to store version of the headers, to prevent unneccessary recompilations
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class XsHeadersIdentifierAttribute : Attribute
    {
        /// Constructor
        public XsHeadersIdentifierAttribute(string headersId)
        {
            HeadersId = headersId;
        }
        /// Headers hash
        public string HeadersId { get; private set; }
    }

    /// Manifest information
    public class CompileOptions
    {
        /// <summary>
        /// Read data from the resource stream with given name into a memory stream
        /// </summary>
        /// <param name="name">Stream name</param>
        /// <returns>null if not found, or memory stream with data otherwise</returns>
        public delegate MemoryStream EmbeddedMemoryStreamProvider(string name);

        /// Where to write generated C# code. It is specified, the files are not deleted after compilation
        public string CodeOutputDirectory { get; set;}

        /// Provider of the embedded files
        public EmbeddedMemoryStreamProvider StreamProvider { get; set; }

        /// List of files to embed into the compiled assembly
        public IEnumerable<EmbeddedFileInfo> FilesToEmbed { get; set; }

        /// Entry point name of the generated assembly
        public string EntryPoint { get; set; }

        /// Extra C# compiler options
        public string ExtraOptions { get; set; }

        /// Compiled Win32 resources
        public byte[] Compiled { get; set; }

        /// Icon
        public byte[] Icon { get; set; }

        /// Manifest
        public byte[] Manifest { get; set; }
    }

    /// Interface represending a verbose writer for debugging purposes
    public interface IWriteVerbose
    {
        /// Write test to verbose output
        void WriteVerbose(string text);
    }

    /// Ignore verbose writes
    public class NullVerboseWriter : IWriteVerbose
    {
        /// Write test to verbose output
        public void WriteVerbose(string text) { }
    }
    /// <summary>
    /// Class that handles code requirements and does C# module compilation
    /// </summary>
    public class CSharpCompiler : MarshalByRefObject
    {
        class Ref
        {
            public string From;
            public string Name;
            public bool Embed;
            
            public Assembly ForceLoad(IWriteVerbose writer)
            {
                writer.WriteVerbose("Loader> Loading " + Name + " from " + From);
                Assembly a = null;
                
                if (!string.IsNullOrEmpty(From))
                {
                    string dll = Path.GetFullPath(From);
                    if (From.IndexOfAny(new char[]{'/','\\'})!=-1 || File.Exists(dll))
                        a = Assembly.LoadFrom(dll);
                    
                }
                if (a==null && !string.IsNullOrEmpty(Name))
                {
                    try
                    {
                        // Yeah, I know it's deprecated, and it's best to use full assembly names, but
                        // but I don't really want to scan GAC myself
#pragma warning disable 618
                        a = Assembly.LoadWithPartialName(Name);
#pragma warning restore 618
                    }
                    catch (FileNotFoundException)
                    {
                        writer.WriteVerbose("Loader> Failed to load partial " + Name);
                    }
                }

                // If we have an assembly, store full path to it
                if (a != null)
                {
                    From = a.Location;
                    Name = a.FullName;
                    writer.WriteVerbose("Loader> Successfully loaded " + a.FullName + " at " + a.Location);
                    return a;
                }
                throw new FileNotFoundException("Failed to load assembly " + Name+" or one of its dependencies",From);
            }
        }
        private readonly Dictionary<string, bool> _using;
        private readonly List<Ref> _references;
        private readonly List<string> _headers;
        private Version _neededVersion = new Version(2,0);
        private Version _availableVersion = null;
        private readonly IWriteVerbose _verboseWriter = null;
        private Version _defaultNETVersion=null;
        private readonly Dictionary<Ref,string> _notLoadedReferences;

        /// Constructor
        public CSharpCompiler(IWriteVerbose logWriter)
        {
            _verboseWriter = logWriter??new NullVerboseWriter();
            _using = new Dictionary<string, bool>();
            _references = new List<Ref>();
            _headers = new List<string>();
            _notLoadedReferences = new Dictionary<Ref, string>();
        }


        /// Returns maximum available .NET version
        public Version MaxAvailableNETVersion
        {
            get
            {
                if (_availableVersion==null)
                {
                    List<Version> versions = new List<Version>(Utils.GetInstalledNETVersions());
                    if (versions.Count == 0)
                        versions.Add(new Version(2, 0));
                    _availableVersion = versions[versions.Count - 1];
                }
                return _availableVersion;
            }
        }

        /// True, if administrative privileges are required for script execution
        public bool RequireAdmin { get; private set; }

        /// Get default .NET version
        public Version DefaultNETVersion
        {
            get { return _defaultNETVersion??MaxAvailableNETVersion; }
            set { _defaultNETVersion = value; }
        }

        /// True if file headers do not require compilation, i.e. they consist merely of using directives and blanks
        public bool IsTrivialFileHeader()
        {
            foreach (string u in _headers)
                if (u.Trim().Length!=0)
                    return false;
            return true;
        }
        
        /// Generate C# file header
        public string GenerateFileHeader()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string u in _using.Keys)
            {
                sb.Append("using " + u + ";");
                sb.AppendLine();
            }
            sb.AppendLine();
            string currentId = GetFileHeaderCodeId();
            
            sb.AppendFormat("// This attribute is required to prevent unnecessary compilations");
            sb.AppendLine();
            sb.AppendFormat("[assembly: {0}(\"{1}\")]", typeof(XsHeadersIdentifierAttribute).FullName,currentId);
            sb.AppendLine();
            foreach (string u in _headers)
                sb.AppendLine(u);
            return sb.ToString();
        }

        /// Generate file header ID
        public string GetFileHeaderCodeId()
        {
            MemoryStream ms = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(ms,Encoding.Unicode))
            {
                foreach (string u in _headers)
                    sw.WriteLine(u);
                sw.Flush();
                ms.Position = 0;

                SHA1 sha1Managed = SHA1Managed.Create();
                return BitConverter.ToString(sha1Managed.ComputeHash(ms.ToArray())).Replace("-","");
            }
        }

        /// Make note that administator privileges are required
        public void AddRequireAdmin(bool elevate)
        {
            if (elevate)
                RequireAdmin = true;
        }

        /// Add minimal .NET version request
        public void AddVersion(Version v)
        {
            if (v == null)
                return;
            if (_neededVersion == null || _neededVersion < v)
            {
                _neededVersion = v;
                if (v >= new Version(3, 5))
                {
                    AddHeaders("using System.Linq;");
                    AddHeaders("using System.Xml.Linq;");
                    AddReference("System.Core.dll","System.Core",false,false,string.Empty);
                    AddReference("System.Xml.Linq.dll", "System.Xml.Linq", false, false, "System.Xml.Linq.");
                }
            }
        }

        /// Add reference to .NET DLL
        public Assembly AddReference(string from)
        {
            return AddReference(from,null,false,false,string.Empty);
        }
        
        
        /// Add reference to .NET assembly specified either by filename or by name
        public Assembly AddReference(string from, string name, bool embed, bool forceLoad, string loadIfStartsWith)
        {
            bool add = true;
            foreach (var r in _references)
            {
                if (!string.IsNullOrEmpty(from) && r.From == from)
                {
                    if (!forceLoad)
                        return null;
                    add = false;
                }
                if (!string.IsNullOrEmpty(name) && r.Name == name)
                {
                    if (!forceLoad)
                        return null;
                    add = false;
                }
            }
            var rNew = new Ref { From = from, Name = name, Embed = embed };
            if (add)
                _references.Add(rNew);

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.IsNullOrEmpty(name) && a.FullName == name)
                    return a;
            }

            if (forceLoad)
                return rNew.ForceLoad(_verboseWriter);
            if (loadIfStartsWith!=null)
                _notLoadedReferences.Add(rNew, loadIfStartsWith);
            return null;
        }
        
        /// Add and parse C# headers
        public void AddHeaders(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;

            StringBuilder sb = new StringBuilder();
            StringReader sr=new StringReader(s);
            
            string line;
            bool usingsAllowed = true;
            while ((line=sr.ReadLine())!=null)
            {
                line = line.TrimEnd();
                if (line.TrimStart().StartsWith("namespace", StringComparison.Ordinal))
                    usingsAllowed = false;
                string lts = line.TrimStart();
                if (lts.StartsWith("using", StringComparison.Ordinal) && usingsAllowed && !lts.Substring(5).TrimStart().StartsWith("(",StringComparison.Ordinal))
                {
                    line = line.TrimStart();
                    line = line.Replace(" ", "");
                    line = line.Replace("\t", "");
                    foreach (string u in line.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        string z= u.Trim();
                        if (z.StartsWith("using", StringComparison.Ordinal))
                            _using[u.Substring(5).Trim()] = true;
                        else
                            sb.AppendLine(u);
                    }
                }
                else if (line.TrimStart().StartsWith("[assembly:",StringComparison.Ordinal) &&
                    line.Contains(typeof(XsHeadersIdentifierAttribute).FullName))
                    continue;
                else if (line.Trim().Length>0)
                    sb.AppendLine(line);
            }
            string header=sb.ToString();
            foreach (string h in _headers)
                if (h==header)
                    return;
            _headers.Add(header);
        }

        /// Get typename of the provided type as C# string, using the registered using directives
        public string GetTypeName(Type t)
        {
            string ret = t.FullName;
            foreach (string u in _using.Keys)
            {
                string sh = string.Empty;
                string ns = u;
                Match m = Regex.Match(u, "^(?<short>.+)\\s*=\\s*(?<value>.+)\\s*$");
                if (m.Success)
                {
                    sh = m.Groups["short"].ToString()+".";
                    ns = m.Groups["value"].ToString();
                }
                if (ns == t.Namespace)
                {
                    ret = sh + t.Name;
                    break;
                }
            }
            // 
            if (t.IsGenericType)
            {
                StringBuilder x = new StringBuilder(ret.Substring(0,ret.IndexOf('`'))+"<");
                bool first = true;
                foreach (Type c in t.GetGenericArguments())
                {
                    if (!first) x.Append(",");
                    first = false;
                    x.Append(GetTypeName(c));
                }
                x.Append(">");
                ret = x.ToString();
                
            }
            
            return ret;
        }

        
        /// Compile code to in-memory assembly
        public Assembly Compile(string code)
        { return Compile(CompiledOutputType.InMemoryAssembly, code, null, null); }

        private const string str_manifest = str_resources + "manifest.manifest";
        private const string str_icon = str_resources + "xsh.ico";
        private const string str_w32resources = str_resources+"w32resources.res";
        private const string str_references = "References\\";
        private const string str_resources = "Resources\\";

        
        /// <summary>
        /// Compile code
        /// </summary>
        /// <param name="outputtype">Type of the assembly to produce</param>
        /// <param name="code">C# code</param>
        /// <param name="moduleName">Name of the generated module</param>
        /// <param name="options">Compilation options</param>
        /// <returns></returns>
        public Assembly Compile(CompiledOutputType outputtype, 
                                string code, 
                                string moduleName,
                                CompileOptions options)
        {
            string codeOutputDirectory = null;
            if (options != null && !string.IsNullOrEmpty(options.CodeOutputDirectory))
            {
                Directory.CreateDirectory(options.CodeOutputDirectory);
                codeOutputDirectory = Path.GetFullPath(options.CodeOutputDirectory);
            }
            
            // Check that we have .NET 
            if (MaxAvailableNETVersion < _neededVersion)
                throw new ParsingException(string.Format(
                                              ".NET v{0}.{1} is required to compile this script. .NET v{2}.{3} is installed.",
                                              _neededVersion.Major, _neededVersion.Minor, MaxAvailableNETVersion.Major, MaxAvailableNETVersion.Minor));

            // .NET 3.0 missing method fix
            bool prov35 = (MaxAvailableNETVersion >= new Version(3, 5));
            



            // Proceed with the gory details
            CompilerParameters param = new CompilerParameters();
            if (outputtype == CompiledOutputType.InMemoryAssembly )
            {
                param.GenerateExecutable = false;
                
                // If codeoutput directory is set, generate DLLs with debug info for <code> debuggin
                param.GenerateInMemory = false;
                param.IncludeDebugInformation = (codeOutputDirectory != null);
            }
            else
            {
                param.GenerateExecutable = (outputtype == CompiledOutputType.ConsoleExe || outputtype == CompiledOutputType.WindowsExe);
                param.GenerateInMemory = false;
                param.IncludeDebugInformation = (codeOutputDirectory != null);
                param.OutputAssembly = moduleName;

                var dir = Path.GetDirectoryName(moduleName);
                if (!string.IsNullOrEmpty(dir) && (codeOutputDirectory != null) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

            }

            param.WarningLevel = 0;
            switch (outputtype)
            {
                case CompiledOutputType.Library:
                    param.CompilerOptions += " /target:library";
                    break;
                case CompiledOutputType.ConsoleExe:
                    param.CompilerOptions += " /target:exe";
                    break;
                case CompiledOutputType.WindowsExe:
                    param.CompilerOptions += " /target:winexe";
                    break;
            }
            CompilerResults cr;
            List<string> toDelete = new List<string>();
            Dictionary<string,bool> embedded=new Dictionary<string, bool>();



            try
            {
                if (options!=null && options.Manifest!=null)
                {
                    // Only C# 3.5 can do manifests & icons separately. If compiled with .NET 2.0 there will be no file version
                    if (prov35)
                    {
                        string wm = getTempFilename(codeOutputDirectory, str_manifest);
                        toDelete.Add(wm);
                        File.WriteAllBytes(wm, options.Manifest);
                        string wi = getTempFilename(codeOutputDirectory, str_icon);
                        toDelete.Add(wi);
                        File.WriteAllBytes(wi, options.Icon);
                        param.CompilerOptions += " " + Utils.QuoteArg("/win32manifest:" + wm) + " " + Utils.QuoteArg("/win32icon:" + wi);
                    }
                    else
                    {
                        string w32 = getTempFilename(codeOutputDirectory, str_w32resources);
                        toDelete.Add(w32);
                        File.WriteAllBytes(w32, options.Compiled);
                        param.Win32Resource = w32;
                    }
                }
                
                if (options!=null && options.EntryPoint != null)
                    param.MainClass = options.EntryPoint;

                bool allStrong = true;
                Dictionary<string,string> resources=new Dictionary<string, string>();
                foreach (Ref r in _references)
                {
                    string location = r.From;
                    string dllName = string.Empty;
                    if (!string.IsNullOrEmpty(r.Name))
                    {
                        dllName = new AssemblyName(r.Name).Name + ".dll";
                    }

                    if (string.IsNullOrEmpty(location) && options != null && options.StreamProvider != null)
                    {
                        using (var v = options.StreamProvider(dllName))
                            if (v != null)
                            {
                                location = getTempFilename(codeOutputDirectory, str_references+Path.GetFileName(dllName));
                                toDelete.Add(location);
                                using (FileStream fs = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read))
                                    v.WriteTo(fs);
                            }
                    }
                    if (string.IsNullOrEmpty(location))
                    {
                        location = r.ForceLoad(_verboseWriter).Location;
                    }
                    if (string.IsNullOrEmpty(location))
                        throw new FileNotFoundException(string.Format("Referenced assembly {0} could not be found", r));

                    param.ReferencedAssemblies.Add(location);

                    if (options != null && moduleName != null && r.Embed && (CompiledOutputType.WindowsExe == outputtype || CompiledOutputType.ConsoleExe == outputtype) && options.StreamProvider != null)
                    {
                        MemoryStream v= options.StreamProvider(dllName + ".gz");
                        if (!string.IsNullOrEmpty(r.Name))
                        {
                            var n = new AssemblyName(r.Name);
                            if (n.GetPublicKeyToken() == null || n.GetPublicKeyToken().Length == 0)
                            {
                                _verboseWriter.WriteVerbose("Compile> Embedded assembly " + r.Name + " does not have a strong name.");
                                allStrong = false;
                            }
                        }
                        if (!embedded.ContainsKey(dllName))
                        {
                            string prefix = "XSharper.Embedded.Assemblies.";
                            // Let's be nice here, and compress the dll a bit
                            string complocation = createResource(resources, v, location, prefix + dllName, codeOutputDirectory, str_resources + prefix.Replace(".", "\\"));
                            if (complocation != null)
                                toDelete.Add(complocation);
                            embedded[dllName] = true;
                        }
                    }
                }
                // If user wanted any files, add them too
                if (options != null && options.FilesToEmbed != null)
                {
                    foreach (var pair in options.FilesToEmbed)
                    {
                        string name = (pair.IsAssembly) ? "XSharper.Embedded.Assemblies." : "XSharper.Embedded.Files.";
                        if (pair.IsAssembly)
                        {
                            _verboseWriter.WriteVerbose("Compile> Getting assembly name of " + pair.Location);
                            var pkt = AssemblyName.GetAssemblyName(pair.Location).GetPublicKeyToken();
                            if (pkt == null || pkt.Length == 0)
                            {
                                _verboseWriter.WriteVerbose("Compile> Embedded assembly " + pair.Location + " does not have a strong name.");
                                allStrong = false;
                            }
                        }
                        string complocation = createResource(resources, null, pair.Location, name + new FileInfo(pair.StreamName).Name, codeOutputDirectory, str_resources + name.Replace(".", "\\"));
                        if (complocation != null)
                            toDelete.Add(complocation);
                    }

                    if (allStrong)
                    {
                        string location = getTempFilename(codeOutputDirectory, str_resources+"XSharper\\Embedded\\Assemblies\\AllStrongName.flag");
                        toDelete.Add(location);
                        using (FileStream fs = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read))
                            fs.WriteByte((byte) '1');
                    }
                }

                if (codeOutputDirectory != null)
                    param.TempFiles = new TempFileCollection(codeOutputDirectory, true);
                foreach (var resource in resources)
                    param.CompilerOptions += " \"/res:" + resource.Key + "," + resource.Value + "\" ";
                if (options!=null && options.ExtraOptions != null)
                    param.CompilerOptions += " " + options.ExtraOptions;

                CSharpCodeProvider prov;
                if (!prov35)
                    prov=new CSharpCodeProvider();
                else
                {
                    Dictionary<string, string> providerOptions = new Dictionary<string, string>();
                    providerOptions.Add("CompilerVersion", string.Format("v{0}", "3.5"));

                    // Must do it this way, to prevent loading errors on machines with .net 2.0
                    prov = (CSharpCodeProvider) Activator.CreateInstance(typeof (CSharpCodeProvider), new object[] {providerOptions});
                }

                _verboseWriter.WriteVerbose("Compile> " + Dump.ToDump(param));
                cr = prov.CompileAssemblyFromSource(param, code);

                // Do some beautification
                if (outputtype != CompiledOutputType.InMemoryAssembly && codeOutputDirectory != null)
                    beautifyOutput(prov35, outputtype, options, param, resources, moduleName, _neededVersion);

                _verboseWriter.WriteVerbose("Compile> -- Completed --");
                // Fire compilation
                if (cr.Errors != null && cr.Errors.Count != 0)
                {
                    if (File.Exists(cr.PathToAssembly))
                        File.Delete(cr.PathToAssembly);
                    StringBuilder sb= new StringBuilder();
                    sb.Append("C# code compilation error:\n");
                    string[] lines = code.Split('\n');
                    foreach (CompilerError error in cr.Errors)
                    {
                        if (error != null)
                        {
                            string line = (error.Line >= 1 && error.Line <= lines.Length) ? lines[error.Line - 1] : "???";
                            sb.AppendLine(string.Format("Line: {0}\nError: {1}", line.TrimEnd(), error.ErrorText));
                        }
                    }
                    _verboseWriter.WriteVerbose("Compile> Errors: " + sb.ToString());
                    
                    throw new ParsingException(sb.ToString());
                }

                if (outputtype == CompiledOutputType.InMemoryAssembly)
                {
                    byte[] asmData = File.ReadAllBytes(cr.PathToAssembly);
                    File.Delete(cr.PathToAssembly);
                    Assembly a=Assembly.Load(asmData);
                    return a;
                }
            }
            finally
            {
                if (string.IsNullOrEmpty(codeOutputDirectory) || outputtype == CompiledOutputType.InMemoryAssembly)
                    foreach (string na in toDelete)
                        File.Delete(na);
            }
            return outputtype == CompiledOutputType.InMemoryAssembly ? cr.CompiledAssembly : null;
        }

        /// Produce solution files
        private static void beautifyOutput(bool prov35, CompiledOutputType outputtype, CompileOptions options, CompilerParameters param, Dictionary<string, string> resources, string moduleName, Version neededVersion)
        {
            string id = Path.GetFileNameWithoutExtension(moduleName);
            string tempName = null;
            string projName = null;
            string cs=null;
            
            
            foreach (string t in param.TempFiles)
            {
                if (!File.Exists(t))
                    continue;
 
                if (tempName == null)
                    tempName = Path.GetFileNameWithoutExtension(t);
                string ext = Path.GetExtension(t).ToLower().TrimStart('.');
                string newName = Path.Combine(Path.GetDirectoryName(t), id + "." + ext);
                File.Delete(newName);
                if (ext == "cs")
                {

                    File.Move(t, newName);
                    cs = newName;
                }

                if (ext == "cmdline")
                {
                    string s = File.ReadAllText(t);

                    string csc = "csc.exe";
                    // Get compiler path
                    foreach (Version v in Utils.GetInstalledNETVersions())
                    {
                        if (neededVersion > v)
                            continue;
                        DirectoryInfo d = Utils.FindNETFrameworkDirectory(v);
                        if (d == null)
                            continue;
                        string fn = Path.Combine(d.FullName, "csc.exe");
                        if (File.Exists(fn))
                        {
                            csc = Utils.QuoteArg(fn);
                            break;
                        }
                    }
                    
                    s = csc + " /noconfig " + s;
                    s = s.Replace(tempName + ".0", id);
                    s = s.Replace(tempName, id);

                    newName = Path.Combine(Path.GetDirectoryName(t), "compile_" + id + ".bat");
                    projName= Path.Combine(Path.GetDirectoryName(t), id + ".csproj");
                    File.Delete(newName);
                    File.WriteAllText(newName, s);
                }
                File.Delete(t);
            }

            // Write project file
            if (projName!=null )
            {
                Guid projGuid = Guid.NewGuid();
                string dir = Path.GetFullPath(Path.GetDirectoryName(projName));
                using (XmlWriter x = XmlWriter.Create(projName,new XmlWriterSettings() { Indent = true, IndentChars = "  "}))
                {
                    string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
                    x.WriteStartElement("Project",ns);
                    if (prov35)
                        x.WriteAttributeString("ToolsVersion", "3.5");
                    x.WriteAttributeString("DefaultTargets", "Build");
                    
                    x.WriteStartElement("PropertyGroup", ns);
                    x.WriteStartElement("Configuration");x.WriteAttributeString("Condition"," '$(Configuration)' == '' ");x.WriteValue("Debug");x.WriteEndElement();
                    x.WriteStartElement("Platform"); x.WriteAttributeString("Condition", " '$(Platform)' == '' "); x.WriteValue("AnyCPU"); x.WriteEndElement();
                    x.WriteElementString("ProductVersion","9.0.30729");
                    x.WriteElementString("SchemaVersion","2.0");
                    x.WriteElementString("ProjectGuid", projGuid.ToString("B").ToUpper());
                    
                    if (outputtype == CompiledOutputType.Library)
                        x.WriteElementString("OutputType", "Library");
                    else if (outputtype == CompiledOutputType.WindowsExe)
                        x.WriteElementString("OutputType", "WinExe");
                    else
                        x.WriteElementString("OutputType", "Exe");
                    x.WriteElementString("AssemblyName", Path.GetFileNameWithoutExtension(projName));
                    x.WriteElementString("ErrorReport", "prompt");
                    x.WriteElementString("WarningLevel", "4");
                    if (prov35)
                    {
                        if (options.Icon != null)
                            x.WriteElementString("ApplicationIcon", str_icon);
                        if (options.Manifest != null)
                            x.WriteElementString("ApplicationManifest", str_manifest);
                    }
                    else
                        x.WriteElementString("Win32Resource", str_w32resources);

                    x.WriteEndElement(); // Property group

                    x.WriteStartElement("PropertyGroup", ns);
                    x.WriteAttributeString("Condition", " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ");
                    x.WriteElementString("DebugType", "Full");
                    x.WriteElementString("DebugSymbols", "true");
                    x.WriteElementString("OutputPath", "bin\\Debug\\");
                    x.WriteElementString("DefineConstants", "DEBUG;TRACE;");
                    x.WriteElementString("Optimize", "false");
                    x.WriteEndElement(); // Property group

                    x.WriteStartElement("PropertyGroup", ns);
                    x.WriteAttributeString("Condition", " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ");
                    x.WriteElementString("DebugType", "pdbonly");
                    x.WriteElementString("DebugSymbols", "false");
                    x.WriteElementString("OutputPath", "bin\\Release\\");
                    x.WriteElementString("DefineConstants", "TRACE;");
                    x.WriteElementString("Optimize", "true");
                    x.WriteEndElement(); // Property group

                    x.WriteStartElement("Import", ns);
                    x.WriteAttributeString("Project","$(MSBuildToolsPath)\\Microsoft.CSharp.targets");
                    x.WriteEndElement(); // Import

                    x.WriteStartElement("ItemGroup", ns);
                    foreach (var r in resources)
                    {
                        x.WriteStartElement("EmbeddedResource", ns);
                        x.WriteAttributeString("Include", root(dir,r.Key));
                        x.WriteEndElement(); // EmbeddedResource
                    }
                    x.WriteEndElement(); // ItemGroup

                    x.WriteStartElement("ItemGroup", ns);
                    foreach (var parameter in param.ReferencedAssemblies)
                    {
                        x.WriteStartElement("Reference", ns);
                        x.WriteAttributeString("Include", root(dir,parameter));
                        x.WriteEndElement(); // Reference
                    }
                    x.WriteEndElement(); // ItemGroup

                    // CS
                    x.WriteStartElement("ItemGroup", ns);
                    x.WriteStartElement("Compile", ns);
                    x.WriteAttributeString("Include", root(dir,cs));
                    x.WriteEndElement();
                    x.WriteEndElement(); // CS

                    if (prov35)
                    {
                        if (options.Icon != null)
                        {
                            x.WriteStartElement("ItemGroup", ns);
                            x.WriteStartElement("None", ns);
                            x.WriteAttributeString("Include",str_icon);
                            x.WriteEndElement(); 
                            x.WriteEndElement(); // Icon
                        }
                        if (options.Manifest != null)
                        {
                            x.WriteStartElement("ItemGroup", ns);
                            x.WriteStartElement("None", ns);
                            x.WriteAttributeString("Include", str_manifest);
                            x.WriteEndElement();
                            x.WriteEndElement(); // Manifest
                        }
                    }
                    else
                    {
                        x.WriteStartElement("ItemGroup", ns);
                        x.WriteStartElement("None", ns);
                        x.WriteAttributeString("Include", str_w32resources);
                        x.WriteEndElement();
                        x.WriteEndElement(); // w32resources
                    }
                    
                    x.WriteEndElement(); // Project
                }

                const string tpl = @"Microsoft Visual Studio Solution File, Format Version 10.00
# Visual Studio 2008
Project(""${slnGuid}"") = ""${id}"", ""${id}.csproj"", ""${projGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		${projGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		${projGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		${projGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		${projGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal";
                File.WriteAllText(Path.ChangeExtension(projName, "sln"),
                                  tpl.Replace("${slnGuid}", Guid.NewGuid().ToString("B").ToUpper())
                                      .Replace("${projGuid}", projGuid.ToString("B").ToUpper())
                                      .Replace("${id}",id));

            }
        }

        static string root(string dir,string filename)
        {
            if (!Path.IsPathRooted(filename))
                return filename;
            filename = Path.GetFullPath(filename);
            if (!dir.EndsWith("\\"))
                dir += "\\";
            if (!filename.StartsWith(dir, StringComparison.CurrentCultureIgnoreCase))
                return filename;
            return filename.Substring(dir.Length);
        }

        static string getTempFilename(string codeOutputDirectory, string originalName)
        {
            if (string.IsNullOrEmpty(codeOutputDirectory))
                return Path.GetTempFileName();
            string s=Path.Combine(codeOutputDirectory, originalName);
            Directory.CreateDirectory(Path.GetDirectoryName(s));
            
            return s;
        }
        private static string createResource(   Dictionary<string,string> resources,
                                                MemoryStream sourceCompressedStream, 
                                                string uncompressedFileLocation,
                                                string friendlyName,
                                                string codeOutputDirectory,
                                                string subdirectory)
        {
            string location = uncompressedFileLocation;
            string suffix = "";
            bool useCompressed = false;
            string complocation;
            if (string.IsNullOrEmpty(codeOutputDirectory))
            {
                complocation = Path.GetTempFileName();
                File.Move(complocation, complocation + ".gz");
                complocation = complocation + ".gz";
            }
            else
            {
                var dir = codeOutputDirectory;
                if (!string.IsNullOrEmpty(subdirectory))
                    dir = Path.Combine(codeOutputDirectory, subdirectory);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                complocation = Path.Combine(dir, Path.GetFileName(location) + ".gz");
            }

            try
            {
                int totalRead = 0;
                using (FileStream fs = new FileStream(complocation, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    if (sourceCompressedStream == null)
                    {
                        using (Stream fi = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (GZipOutputStream com = new GZipOutputStream(fs, 65536))
                        {
                            com.IsStreamOwner = false;
                            
                            byte[] bytes = new byte[4096];
                            int n;
                            while ((n = fi.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                com.Write(bytes, 0, n);
                                totalRead += n;
                            }
                            com.Flush();
                        }
                    }
                    else
                    {
                        sourceCompressedStream.WriteTo(fs);

                        totalRead = (int)fs.Length;
                    }

                    fs.Flush();
                    if (fs.Length > totalRead)
                    {
                        // GZip sucks!
                        File.Delete(complocation);
                    }
                    else
                    {
                        location = complocation;
                        suffix = ".gz";
                        useCompressed = true;
                    }
                }
                
            }
            catch
            {
                // Compression failed, but we don't care and use uncompressed version instead
                try
                {
                    File.Delete(complocation);
                }
                catch
                {
                }
            }

            // param.EmbeddedResources works incorrectly :(
            resources[location] = friendlyName + suffix;
            if (useCompressed)
                return complocation;
            return null;
        }


        private Type tryResolveTypeName(string fullClassName, IEnumerable<Assembly> extraAssemblies)
        {
            Type t = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = assembly.GetType(fullClassName, false, true);
                if (t != null)
                    break;
            }
            if (t == null && extraAssemblies != null)
                foreach (Assembly a in extraAssemblies)
                {
                    t = a.GetType(fullClassName, false, true);
                    if (t != null)
                        break;
                }
            if (t == null)
            {
                bool newLoaded = false;
                if (_notLoadedReferences.Count > 0)
                {
                    List<Ref> rem = null;
                    foreach (var r in _notLoadedReferences)
                    {
                        if (r.Value.Length == 0 || fullClassName.StartsWith(r.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            r.Key.ForceLoad(_verboseWriter);
                            newLoaded = true;
                            if (rem == null)
                                rem = new List<Ref>();
                            rem.Add(r.Key);
                        }
                    }
                    if (rem!=null)
                        foreach (var r in rem)
                            _notLoadedReferences.Remove(r);
                }
                if (newLoaded)
                    return TryResolveTypeNameWithUsing(fullClassName, extraAssemblies);
            }
            return t;
        }

        
        /// <summary>
        /// Try to find a type with given name in the list of assemblies loaded into the current domain, or
        /// referenced in the using directories
        /// </summary>
        /// <param name="fullClassName">Class name, for example XS.ScriptContext</param>
        /// <param name="extraAssemblies">Collection of assemblies to search in addition to assemblies already loaded into the current domain</param>
        /// <returns></returns>
        public Type TryResolveTypeNameWithUsing(string fullClassName, IEnumerable<Assembly> extraAssemblies)
        {
            int n = fullClassName.LastIndexOf('.');
            
            string typeNamespace =null;
            string typeName = null;
            if (n != -1 && n!=fullClassName.Length-1)
            {
                // There may be some prefixes, that change XS.Utils => XSharper.Core.Utils
                typeNamespace = fullClassName.Substring(0, n+1);
                typeName = fullClassName.Substring(n + 1);
            
                foreach (string u in _using.Keys)
                {
                    Match m = Regex.Match(u, "^(?<short>.+)\\s*=\\s*(?<value>.+)\\s*$",RegexOptions.Compiled);
                    if (m.Success)
                    {
                        string sh = m.Groups["short"] + ".";
                        if (typeNamespace.StartsWith(sh, StringComparison.OrdinalIgnoreCase))
                        {
                            typeNamespace = m.Groups["value"].Value + typeNamespace.Substring(sh.Length - 1);
                        }
                    }
                }

                fullClassName = typeNamespace + typeName;
            }

            Type t = tryResolveTypeName(fullClassName, extraAssemblies);
            if (t != null)
                return t;

            foreach (string u in _using.Keys)
            {
                if (u.IndexOf('=') != -1)
                    continue;
                string name = u + "." + fullClassName;
                t = tryResolveTypeName(name, extraAssemblies);
                if (t != null)
                    return t;
            }

            // Last chance - try internal classes
            if (n!=-1)
            {
                typeNamespace = fullClassName.Substring(0, n );
                var tParent=TryResolveTypeNameWithUsing(typeNamespace, extraAssemblies);
                if (tParent!=null)
                {
                    t=tParent.GetNestedType(typeName);
                }
            }

            
            return t;
        }



        
    }
}