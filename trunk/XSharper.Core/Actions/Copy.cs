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
using System.ComponentModel;
using System.IO;

namespace XSharper.Core
{
    /// Fake file info for URIs
    internal class UriFileInfo : IFileSystemInfo
    {
        Uri _uri;
        DateTime _time,_timeUtc;
        public UriFileInfo(Uri uri)
        {
            _uri = uri;
            _time=DateTime.Now;
            _timeUtc=DateTime.UtcNow;
        }
        #region IFileSystemInfo Members

        public bool IsDirectory         {   get { return false; }        }
        public bool IsFile              {   get { return true; }        }
        public bool Exists              {   get { return true; }    }
        public long Length              {   get { return -1; }}
        public string Name              {   get { return Path.GetFileName(_uri.GetComponents(UriComponents.Path,UriFormat.Unescaped)); } }
        public string FullName          { get { return _uri.OriginalString; } }
        public string Extension         {   get { return Path.GetExtension(Name); }}
        public FileAttributes Attributes {  get { return FileAttributes.Normal; }}
        public DateTime CreationTime    { get { return _time; }}
        public DateTime CreationTimeUtc { get { return _timeUtc; } }
        public DateTime LastWriteTime   { get { return _time ;}}
        public DateTime LastWriteTimeUtc { get { return _timeUtc; }}
        public DateTime LastAccessTime   { get { return _time; }}
        public DateTime LastAccessTimeUtc { get { return _timeUtc; }}
        #endregion

        public override string ToString()
        {
            return _uri.ToString();
        }
    }
    /// How to deal with already existing files
    public enum OverwriteMode
    {
        /// Never overwrite
        [Description("Never overwrite")]
        Never,

        /// Overwrite if the copied over file is never
        [Description("Overwrite if the copied over file is never")]
        IfNewer,

        /// Always overwrite
        [Description("Always overwrite")]
        Always,

        /// Confirm whether to overwrite
        [Description("Confirm whether to overwrite")]
        Confirm
    }

    /// Copy files and directories
    [XsType("copy", ScriptActionBase.XSharperNamespace)]
    [Description("Copy files and directories")]
    public class Copy : ActionWithFilters 
    {
        /// File or directory from where to copy.
        [Description("File or directory from where to copy.")]
        [XsRequired]
        public string From { get; set; }

        /// File or directory where to copy.
        [Description("File or directory where to copy.")]
        [XsRequired]
        public string To { get; set; }

        /// True to recurse directories. Default false.
        [Description("True to recurse directories. Default false.")]
        public bool Recursive { get; set; }

        /// True to copy empty directories (default)
        [Description("True to copy empty directories (default)")]
        public bool EmptyDirectories { get; set; }

        /// Whether to overwrite existing files (default is always)
        [Description("Whether to overwrite existing files (default is always)")]
        public OverwriteMode Overwrite { get; set; }

        /// True if move operation should be done instead of copying. Default false (copy)
        [Description("True if move operation should be done instead of copying. Default false (copy)")]
        public bool Move { get; set; }

        /// Constructor
        public Copy()
        {
            EmptyDirectories = true;
            Overwrite = OverwriteMode.Always;
            Move = false;
        }

        /// Execute action
        public override object Execute()
        {
            string fromExpanded = Context.TransformStr(From, Transform);
            string toExpanded = Context.TransformStr(To, Transform);

            var nf = new FileNameOnlyFilter(Syntax, Context.TransformStr(Filter, Transform));
            var df = new FullPathFilter(Syntax, Context.TransformStr(DirectoryFilter, Transform), BackslashOption.Add);

            object ret = null;
            Uri uri;
            if (Uri.TryCreate(fromExpanded, UriKind.Absolute, out uri))
                if (uri.IsFile)
                {
                    fromExpanded=uri.LocalPath;
                    uri=null;
                }

            if (uri!=null)
            {
                FileInfo to;
                toExpanded= Download.UrlToLocalFileName(fromExpanded,toExpanded);
                to = new FileInfo(toExpanded);
                VerboseMessage("Copying from URI {0} to {1}", fromExpanded, to);
                ret=downloadSingleFile(nf,uri,to);
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(fromExpanded);
                if (fromExpanded.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                    fromExpanded.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                    di.Exists)
                {
                    VerboseMessage("Copying a directory {0} to {1}", di, toExpanded);
                    ret = copy(di, di, new DirectoryInfo(toExpanded), nf, df);
                }
                else
                {

                    FileInfo fr = new FileInfo(fromExpanded);
                    FileInfo to;
                    if (toExpanded.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                        toExpanded.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                        new DirectoryInfo(toExpanded).Exists)
                    {
                        to = new FileInfo(Path.Combine(new DirectoryInfo(toExpanded).FullName, fr.Name));
                    }
                    else
                    {
                        to = new FileInfo(toExpanded);
                    }
                    VerboseMessage("Copying a single file {0} to {1}", fr, to);
                    ret = copySingleFile(nf, fr, to);
                }
            }
            if (ReturnValue.IsBreak(ret))
                return null;
            ReturnValue.RethrowIfException(ret);
            return ret;
        }

        private object copy(DirectoryInfo rootFrom, DirectoryInfo fromDir, DirectoryInfo toDir,IStringFilter nf, IStringFilter df)
        {
            bool isRoot = (rootFrom == fromDir);
            bool isVisible = (isRoot || CheckHidden(fromDir));
            bool processFiles = isVisible;
            
            if (processFiles && (df != null && !df.IsMatch(fromDir.FullName)))
            {
                processFiles = false;
                VerboseMessage("{0} did not pass directory filter", fromDir.FullName);
            }

            var from = new FileOrDirectoryInfo(fromDir);
            var to = new FileOrDirectoryInfo(toDir);
                
            return ProcessPrepare(from, to,
                delegate
                    {
                        if (processFiles)
                        {
                            if (EmptyDirectories && !to.Exists)
                            {
                                bool created;
                                object r = createDir(fromDir, toDir, out created);
                                if (r != null || !created)
                                    return r;
                            }

                            foreach (FileInfo f in fromDir.GetFiles())
                            {
                                FileInfo toFile = new FileInfo(Path.Combine(to.FullName, f.Name));
                                object r = copySingleFile(nf, f, toFile);
                                if (r != null)
                                    return r;
                            }
                        }
                        if (Recursive)
                        {
                            foreach (DirectoryInfo d in fromDir.GetDirectories())
                            {
                                object r = copy(rootFrom, d, new DirectoryInfo(Path.Combine(to.FullName, d.Name)), nf, df);
                                if (r != null)
                                    return r;
                            }
                        }
                        return null;
                    });
        }

        private object createDir(DirectoryInfo fromDir, DirectoryInfo toDir, out bool created)
        {
            var from = new FileOrDirectoryInfo(fromDir);
            var to = new FileOrDirectoryInfo(toDir);
            bool createdTmp=false;
            object ret = ProcessComplete(from, to, false, delegate(bool skip)
                   {
                       if (!skip)
                       {
                           toDir.Create();
                           toDir.Attributes = fromDir.Attributes;
                           toDir.LastWriteTimeUtc = fromDir.LastWriteTimeUtc;
                           createdTmp = true;
                       }
                       return null;
                   });
            created = createdTmp;
            return ret;
        }

        private object downloadSingleFile(IStringFilter nf, Uri single, FileInfo toFile)
        {
            var from = new UriFileInfo(single);
            var ff = from.Name;
            if (string.IsNullOrEmpty(ff))
                ff = toFile.Name;
            if (nf != null && (!nf.IsMatch(ff)))
            {
                VerboseMessage("{0} did not pass filter", single);
                return null;
            }
            
            var to = new FileOrDirectoryInfo(toFile);
            bool skip = false;
            object ret = ProcessPrepare(from, to,
                delegate
                {
                    if (toFile.Directory != null && !toFile.Directory.Exists)
                    {
                        bool created;
                        object r = createDir(new DirectoryInfo(Path.GetTempPath()), toFile.Directory, out created);
                        if (!created || r != null)
                            return r;
                    }


                    bool overwrite = (Overwrite == OverwriteMode.Always);
                    if (Overwrite == OverwriteMode.IfNewer)
                    {
                        if (toFile.Exists && toFile.LastWriteTimeUtc >= from.LastWriteTimeUtc)
                        {
                            VerboseMessage("Ignoring never file {0} ", toFile.FullName);
                            return null;
                        }
                        overwrite = true;
                    }

                    skip = (toFile.Exists && !overwrite);
                    return null;
                });
            if (ret != null)
                return ret;
            if (skip && Overwrite != OverwriteMode.Confirm)
            {
                VerboseMessage("Ignoring existing file {0} ", toFile.FullName);
                return null;
            }
            ret = ProcessComplete(from, to, skip, delegate(bool skip1)
            {
                if (!skip1)
                {
                    // Continue with copy
                    if (toFile.Directory != null && !toFile.Directory.Exists)
                        toFile.Directory.Create();
                    VerboseMessage("Downloading {0} => {1}", from.FullName, toFile.FullName);
                    Download dn = new Download
                    {
                        From = single.OriginalString,
                        To = toFile.FullName,
                        Transform = TransformRules.None
                    };
                    return Context.Execute(dn);
                }
                return null;
            });
            return ret;
        }
    
        private object copySingleFile(IStringFilter nf, FileInfo f, FileInfo toFile)
        {
            if (nf != null && (!nf.IsMatch(f.FullName) || !CheckHidden(f)))
            {
                VerboseMessage("{0} did not pass filter", f.FullName);
                return null;
            }
            var from=new FileOrDirectoryInfo(f);
            var to = new FileOrDirectoryInfo(toFile);
            bool skip = false;
            object ret = ProcessPrepare(from, to,
                delegate
                    {
                        if (toFile.Directory != null && !toFile.Directory.Exists)
                        {
                            bool created;
                            object r = createDir(f.Directory, toFile.Directory, out created);
                            if (!created || r != null)
                                return r;
                        }

                        
                        bool overwrite = (Overwrite == OverwriteMode.Always);
                        if (Overwrite == OverwriteMode.IfNewer)
                        {
                            if (toFile.Exists && toFile.LastWriteTimeUtc >= f.LastWriteTimeUtc)
                            {
                                VerboseMessage("Ignoring never file {0} ", toFile.FullName);
                                return null;
                            }
                            overwrite = true;
                        }

                        skip = (toFile.Exists && !overwrite);
                        return null;
                    });
            if (ret!=null)
                return ret;
            if (skip && Overwrite != OverwriteMode.Confirm)
            {
                VerboseMessage("Ignoring existing file {0} ", toFile.FullName);
                return null;
            }
            ret = ProcessComplete(from,to, skip, delegate(bool skip1) {
                                      if (!skip1)
                                      {
                                          // Continue with copy
                                          if (toFile.Directory != null && !toFile.Directory.Exists)
                                              toFile.Directory.Create();

                                          if (toFile.Exists && (toFile.Attributes & (FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                                              toFile.Attributes &= ~(FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);

                                          if (Move)
                                          {
                                              if (toFile.FullName != f.FullName)
                                                  toFile.Delete();
                                              VerboseMessage("Move {0} => {1}", f.FullName, toFile.FullName);
                                              Context.MoveFile(f.FullName, toFile.FullName, true);
                                          }
                                          else
                                          {
                                              VerboseMessage("Copy {0} => {1}", f.FullName, toFile.FullName);
                                              Context.CopyFile(f.FullName, toFile.FullName, true);
                                          }
                                      }
                                      return null;
                                  });
            return ret;
        }
    }
}