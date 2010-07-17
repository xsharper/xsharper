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
using System.Net.Cache;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace XSharper.Core
{
    /// <summary>
    /// Script context
    /// </summary>
    public partial class ScriptContext  
    {
        #region -- Current directory helpers --

        /// Get current directory with backslash at the end. For example: C:\
        public string CurrentDirectory
        {
            get { return Utils.BackslashAdd(Path.GetFullPath(Directory.GetCurrentDirectory())); }
        }



        /// Get script that is currently executed.
        public Script Script
        {
            get
            {
                return _callStack.GetCurrentScript();
            }
        }

        /// Get root script, from which the execution started
        public Script MainScript
        {
            get
            {
                return _callStack.GetMainScript();
            }
        }
        
        #endregion

        #region -- Usage helpers --

        /// Return script usage
        public string GetUsage()
        {
            return GetUsage(-1);
        }

        /// Get script usage, not exceeding the given width (-1 = autodetect width)
        public string GetUsage(int width)
        {
            Script s = _callStack.GetCurrentScript();
            if (s == null)
                return null;
            return GetAutoUsage(s, width);
        }

        /// Get script usage, not exceeding the given width (-1 = autodetect width)
        public string GetAutoUsage(Script s, int width)
        {
            string id = s.Id;
            if (string.IsNullOrEmpty(id))
                id=Path.GetFileNameWithoutExtension(s.Location).ToUpperInvariant();
            

            StringWriter sw = new StringWriter();
            string desc = null;
            if ((s.Usage.Options & UsageOptions.UseVersionInfo)!=0)
                desc=s.VersionInfo.GenerateInfo(this, true);
            

            CommandLineParameters c=new CommandLineParameters(s.Parameters,s.SwitchPrefixes,s.UnknownSwitches);
            sw.Write(s.Usage.GetUsage(this,desc, id, width, c));
            return sw.ToString();
        }
        #endregion

        #region -- Call subroutione --

        /// Call script subroutine with a given ID with parameters
        public object Call(string id, params object[] parameters)
        {
            IScriptAction f = Find<Sub>(id);
            if (f == null)
                throw new ParsingException("A subroutine with id=" + id + " not found");
            List<CallParam> param=new List<CallParam>();
            foreach (var p in parameters)
                param.Add(new CallParam(null,p,TransformRules.None));
            object r = ExecuteAction(f, param, CallIsolation.Default);
            return ReturnValue.Unwrap(r);
        }

        /// Execute script included by include directory with the specified ID
        public object Exec(string includeId, params string[] parameters)
        {
            Include f = Find<Include>(includeId);
            if (f == null)
                throw new ParsingException("Include with id=" + includeId + " not found");

            object r = ExecuteScript(f.IncludedScript, parameters, CallIsolation.Default);
            return ReturnValue.Unwrap(r);
        }

        /// Execute script from file with parameters
        public object ExecFile(string file, bool validate, params string[] parameters)
        {
            string fFound = FindScriptPartFileName(file,null);
            if (fFound == null)
                fFound = file;
            Script s=LoadScript(fFound, validate);
            Initialize(s);
            object r = ExecuteScript(s, parameters, CallIsolation.Default);
            return ReturnValue.Unwrap(r);
        }
        #endregion

        #region -- File management helpers --


        /// Try to resolve location using ScriptPath
        public string FindScriptPartFileName(string loc, string path)
        {
            if (string.IsNullOrEmpty(loc))
                return loc;
            if (string.IsNullOrEmpty(path))
                path = ScriptPath;
            else
            {
                if (!path.EndsWith(";"))
                    path += ";";
                path+= ScriptPath;
            }
            if (Script != null)
                path = Script.DirectoryName + ";" + path;

            string str = SearchPath(loc, path);
            return str ?? loc;
        }

        /// Search for a file in the specified location using the path provided
        public virtual string SearchPath(string location, string path)
        {
            // For URLs - no change
            Uri url;
            if (Uri.TryCreate(location, UriKind.Absolute, out url) && !url.IsFile)
                return location;

            if (location.StartsWith("." + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || location.StartsWith("." + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                location = Path.GetFullPath(location);
            
            List<string> loc = new List<string>();
            
            if (Path.IsPathRooted(location))
                loc.Add(location);
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    foreach (string p in path.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        if (!string.IsNullOrEmpty(p))
                        {
                            string dir = Path.Combine(p, location);
                            if (!loc.Contains(dir))
                                loc.Add(dir);
                        }
                }
            }
            foreach (string s in loc)
            {
                try
                {
                    FileInfo fi = new FileInfo(s);
                    if (fi.Exists)
                        return fi.FullName;
                }
                catch (IOException)
                {
                }
            }
            return null;
        }
        
        public Stream OpenStream(string fileName)
        {
            return OpenStream(fileName, FileMode.Open, true);
        }

        /// Open file in specified mode for writing.
        public virtual Stream OpenStream(string fileName, FileMode mode, bool shared)
        {
            WriteVerbose("OpenStream> Opening '"+fileName+"' for "+mode+(shared?"(shared)":string.Empty));
                
            Uri u;
            if (Uri.TryCreate(fileName, UriKind.Absolute, out u) && !u.IsFile)
            {
                if (mode!=FileMode.Open)
                    throw new ArgumentOutOfRangeException("mode","Only FileMode.Open mode is allowed for URIs");
                WriteVerbose("OpenStream> Reading from " + Utils.SecureUri(u));
                if (u.Scheme == "embed")
                {
                    fileName = u.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                    Stream str = FindResourceStream(fileName);
                    if (str == null)
                        throw new FileNotFoundException("Embedded resource file not found", fileName);
                    return str;
                }
                bool active = false;
                if (u.Scheme == "ftpa")
                {
                    u = new UriBuilder(u) { Scheme = "ftp" }.Uri;
                    active = true;
                }

                WriteVerbose("OpenStream> Starting download");
                using (var w = new WebClientEx(!active, true))
                {
                    w.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate);
                    u = w.SetCredentials(u, null, null);

                    var ms = new MemoryStream(w.DownloadData(u));
                    WriteVerbose("OpenStream> Download completed");
                    return ms;
                }
            }
                
            FileShare share = shared ? FileShare.Read : FileShare.None;
            if (mode == FileMode.Open)
            {
                // Uri constructor does not work well with C:\Windows\..\X.txt producing C:\Windows\X.txt
                if (u != null && fileName.StartsWith(Uri.UriSchemeFile + ":", StringComparison.OrdinalIgnoreCase))
                    fileName = u.LocalPath;

                return new FileStream(fileName, mode, FileAccess.Read, share, 16384);
            }
            if (mode == FileMode.Append)
                return new FileStream(fileName, mode, FileAccess.Write, share, 16384);
            return new FileStream(fileName, mode, FileAccess.ReadWrite, share, 16384);
        }

        /// Create file stream, overwriting an existing file
        public Stream CreateStream(string fileName)
        {
            return OpenStream(fileName, FileMode.Create,false);
        }

        /// Read all text from file. Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        public string ReadText(string fileName)
        {
            return ReadText(fileName, null);
        }

        /// Read all text from file in the given encoding (null=autodetect). Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        public string ReadText(string fileName, Encoding encoding)
        {
            if (encoding==null)
            {
                string[] comp = fileName.Split('|');
                fileName = comp[0];
                if (comp.Length >= 2)
                    encoding = Utils.GetEncoding(comp[1]);
            }
            using (Stream fs = OpenStream(fileName))
            using (StreamReader sr = createSR(fs, encoding))
                return sr.ReadToEnd();
        }

        /// Read all lines from file. Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        public string[] ReadAllLines(string fileName)
        {
            return ReadAllLines(fileName, null);
        }

        /// Read all lines from file. Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        public string[] ReadAllLines(string fileName, Encoding encoding)
        {
            return new List<string>(ReadLines(fileName,encoding)).ToArray();
        }

        /// Read all lines from file. Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        /// This is synonym for <see cref="ReadAllLines(string)"/>
        public IEnumerable<string> ReadLines(string fileName)
        {
            return ReadLines(fileName, null);
        }

        /// Read all lines from file. Encoding may be specified in the filename itself by attaching it after |. For example, c:\file.txt|utf-8
        /// This is synonym for <see cref="ReadAllLines(string,Encoding)"/>
        public IEnumerable<string> ReadLines(string fileName, Encoding encoding)
        {
            using (Stream fs = OpenStream(fileName))
            using (StreamReader sr = createSR(fs, encoding))
            {
                string s;
                while ((s=sr.ReadLine())!=null)
                    yield return s;
            }
        }

        /// Read all bytes, up to 4GB, from a give file
        public byte[] ReadBytes(string fileName)
        {
            return ReadBytes(fileName, 0, int.MaxValue, false);
        }

        /// Read all bytes, up to 4GB, from a give file from the specified offset
        public byte[] ReadBytes(string fileName, long fileOffset, int count)
        {
            return ReadBytes(fileName, fileOffset, count, false);
        }

        /// Read all bytes, up to 4GB, from a give file from the specified offset. If exactCount is set, throw if EOF is found earlier
        public byte[] ReadBytes(string fileName,long fileOffset, int count, bool exactCount)
        {
            using (Stream fs = OpenStream(fileName))
            {
                if (fileOffset > 0)
                    fs.Seek(fileOffset, SeekOrigin.Begin);
                else if (fileOffset<0)
                    fs.Seek(fileOffset, SeekOrigin.End);
                return Utils.ReadBytes(fs, count, exactCount);
            }
        }

        /// Write the given byte array to a file, using <see cref="OpenFileStream"/> to open the file
        public void WriteBytes(string fileName, byte[] data)
        {
            WriteBytes(fileName, data, 0,true);
        }

        /// Write the given byte array to a file, starting from the specified file offset, using <see cref="OpenFileStream"/> to open the file
        public void WriteBytes(string fileName, byte[] data, long fileOffset, bool overwrite)
        {
            using (Stream fs = OpenStream(fileName, overwrite ? FileMode.Create : FileMode.OpenOrCreate, false))
            {
                if (fileOffset != 0)
                    fs.Seek(fileOffset, SeekOrigin.Begin);
                fs.Write(data, 0,data.Length);
            }
        }

        /// Write value as text to the file
        public void WriteText(string fileName, object value)
        {
            WriteText(fileName, value, (Encoding)null,false);
        }

        /// Write value as text to the file, in the specified encoding.
        public void WriteText(string fileName, object value, string encoding)
        {
            WriteText(fileName,value,encoding,false);
        }

        /// Write value as text to the file, in the specified encoding. Encoding may be appended to the filename after |. For example file.txt|utf-8
        public void WriteText(string fileName, object value, string encoding, bool append)
        {
            WriteText(fileName, value, (encoding == null) ? (Encoding)null : Utils.GetEncoding(encoding), append);
        }

        /// Write value as text to the file, in the specified encoding. Encoding may be appended to the filename after |. For example file.txt|utf-8
        public void WriteText(string fileName, object value, Encoding encoding, bool append)
        {
            if (encoding==null)
            {
                string[] comp = fileName.Split('|');
                fileName = comp[0];
                if (comp.Length >= 2)
                    encoding = Utils.GetEncoding(comp[1]);
            }
            if (encoding == null)
                encoding = Encoding.UTF8;

            using (StreamWriter sw = (encoding==null) ? new StreamWriter(OpenStream(fileName, append ? FileMode.Append : FileMode.Create, true)) :
                                                        new StreamWriter(OpenStream(fileName, append ? FileMode.Append : FileMode.Create, true), encoding))
            {
                sw.Write(Utils.To<string>(value));
            }
        }


        private static StreamReader createSR(Stream s, Encoding encoding)
        {
            if (encoding==null)
                return new StreamReader(s, true);
            return new StreamReader(s, encoding);
        }

        #endregion

        /// Return true if current user is administrator
        public bool IsAdministrator
        {
            get
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                if (id==null)
                    return false;
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// Copy file from source to destination, while calling progress routine
        public void CopyFile(string source, string destination)
        {
            CopyFile(source, destination, false);
        }

        /// Copy file from source to destination, while calling progress routine
        public void CopyFile(string source, string destination, bool overwrite)
        {
            Utils.CopyFile(source, destination, overwrite, copy_callback);
        }
        
        /// Move file from source to destination, while calling progress routine
        public void MoveFile(string source, string destination)
        {
            MoveFile(source, destination, false);
        }

        /// Move file from source to destination, while calling progress routine
        public void MoveFile(string source, string destination, bool overwrite)
        {
            Utils.MoveFile(source, destination, overwrite, copy_callback);
        }

        /// Calculate SHA1 over file contents
        public byte[] SHA1File(string filename)
        {
            using (SHA1 hash = SHA1Managed.Create())
            using (var s = OpenStream(filename))
                return hash.ComputeHash(s);
        }

        /// Get environment variable
        public string GetEnv(string name)
        {
            var targ = getTarget(ref name);
            return Environment.GetEnvironmentVariable(name, targ);
        }

        /// Set environment variable
        public void SetEnv(string name,string value)
        {
            var targ = getTarget(ref name);
            Environment.SetEnvironmentVariable(name, value, targ);
        }

        
        private void copy_callback(string source, string destination, object state, long totalFileSize, long totalBytesTransferred)
        {
            OnProgress(1,source);
        }

    }
       
    
}