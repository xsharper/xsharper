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
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace XSharper.Core
{
    /// <summary>
    /// How to interpret time in ZIP files
    /// </summary>
    public enum ZipTime
    {
        /// Use current local time
        [Description("Use current local time")]
        Now,

        /// Use current UTC time
        [Description("Use current UTC time")]
        UtcNow,

        /// ZIP file time is local time
        [Description("ZIP file time is local time")]
        FileTime,

        /// ZIP file time is UTC time
        [Description("ZIP file time is UTC time")]
        UtcFileTime
    }

    /// <summary>
    /// Compress a directory
    /// </summary>
    [XsType("zip", ScriptActionBase.XSharperNamespace)]
    [Description("Compress a directory")]
    public class Zip : ZipBase
    {
        /// Directory or file to compress
        [Description("Directory or file to compress")]
        [XsRequired]
        public string From { get; set; }

        /// ZIP file to create
        [Description("ZIP file to create")]
        [XsRequired]
        public string To { get; set; }

        /// ZIP comment
        [Description("ZIP comment")]
        public string Comment { get; set; }

        /// Compression level (0-9). Default 6
        [Description("Compression level (0-9)")]
        public int Level { get; set; }

        /// True, if directory must be scanned recursively
        [Description("True, if directory must be scanned recursively")]
        public bool Recursive { get; set; }

        /// True, if unicode filenames should be used
        [Description("True, if unicode filenames should be used")]
        public bool Unicode { get; set; }

        /// True, if empty directories must be archived
        [Description("True, if empty directories must be archived")]
        public bool EmptyDirectories { get; set; }

        /// Constructor
        public Zip()
        {
            Level = 6;
        }
        /// Execute action
        public override object Execute()
        {
            string f = Context.TransformStr(From, Transform);
            DirectoryInfo src;
            IStringFilter nf, df;
            string trg = Context.TransformStr(To, Transform);

            if (File.Exists(f))
            {
                src = new DirectoryInfo(new FileInfo(f).DirectoryName);
                nf = new SelfIgnoreFilenameOnlyFilter(trg,FilterSyntax.Pattern, Regex.Escape(Path.GetFileName(f)));
                df = new StringFilter(null);
            }
            else
            {
                src = new DirectoryInfo(f);
                nf = new SelfIgnoreFilenameOnlyFilter(trg, Syntax, Context.TransformStr(Filter, Transform));
                df = new FullPathFilter(Syntax, Context.TransformStr(DirectoryFilter, Transform));
            }
            
            if (!src.Exists)
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist",src));

            
            try
            {
                using (Stream fs = Context.CreateWriteStream(trg))
                {
                    var ret = createZip(fs, src.FullName, nf, df);
                    if (ReturnValue.IsBreak(ret))
                        return null;
                    ReturnValue.RethrowIfException(ret);
                    return ret;
                }
            }
            catch 
            {
                File.Delete(trg);
                throw;
            }
        }

        class SelfIgnoreFilenameOnlyFilter : FileNameOnlyFilter
        {
            private string _except;
            public SelfIgnoreFilenameOnlyFilter(string except, FilterSyntax syntax, string filter) : base(syntax, filter)
            {
                _except = Path.GetFullPath(except);
            }
            public override bool IsMatch(string name)
            {
                if (string.Compare(Path.GetFullPath(name),_except,StringComparison.InvariantCultureIgnoreCase)==0)
                    return false;
                return base.IsMatch(name);
            }
        }
        private object createZip(Stream fileStream, string sourceDirectory, IStringFilter nf, IStringFilter df)
        {
            ZipEntryFactory entryFactory;
            switch (ZipTime)
            {
                default:
                case ZipTime.Now:
                    entryFactory = new ZipEntryFactory(DateTime.Now);
                    break;
                case ZipTime.UtcNow:
                    entryFactory = new ZipEntryFactory(DateTime.UtcNow);
                    break;
                case ZipTime.FileTime:
                    entryFactory = new ZipEntryFactory(ZipEntryFactory.TimeSetting.LastWriteTime);
                    break;
                case ZipTime.UtcFileTime:
                    entryFactory = new ZipEntryFactory(ZipEntryFactory.TimeSetting.LastWriteTimeUtc);
                    break;
            }
            entryFactory.NameTransform = new ZipNameTransform(sourceDirectory);
            entryFactory.IsUnicodeText = Unicode;
            
            ProgressHandler progress = delegate(object x, ProgressEventArgs y) { Context.OnProgress(1, y.Name); };
            using (ZipOutputStream zip = new ZipOutputStream(fileStream))
            {
                if (Comment!=null)
                    zip.SetComment(Context.TransformStr(Password, Transform));
                if (Password != null)
                    zip.Password = Context.TransformStr(Password, Transform);
                zip.SetLevel(Level);    


                return scanDir(sourceDirectory, sourceDirectory, new scanDirParams(zip, entryFactory, progress, nf, df));
            }
        }

        class scanDirParams
        {
            public scanDirParams(ZipOutputStream zip, ZipEntryFactory entryFactory, ProgressHandler progress, IStringFilter nf, IStringFilter df)
            {
                Zip = zip;
                EntryFactory = entryFactory;
                Progress = progress;
                NameFilter = nf;
                DirFilter = df;
            }
            public ZipOutputStream Zip { get; private set; }
            public ZipEntryFactory EntryFactory { get; private set; }
            public ProgressHandler Progress { get; private set; }
            public IStringFilter NameFilter { get; private set; }
            public IStringFilter DirFilter { get; private set; }
        }

        object scanDir(string sourceDirectory, string directory, scanDirParams scanDirParams)
        {
            if (!scanDirParams.DirFilter.IsMatch(directory))
                return null;

            // Process this directory
            DirectoryInfo di = new DirectoryInfo(directory);
            ZipFSEntry ze = null;
            if (sourceDirectory == directory)
            {
                ZipEntry zen = new ZipEntry("");
                zen.ExternalFileAttributes |= 16;
                ze = new ZipFSEntry(zen, ZipTime);
            }
            else
            {
                if (!CheckHidden(di))
                    return null;
                ze = new ZipFSEntry(scanDirParams.EntryFactory, di, ZipTime);
            }

            return ProcessPrepare(new FileOrDirectoryInfo(di), ze, () => prepareDir(di, sourceDirectory, directory, scanDirParams));
        }

        object prepareDir(DirectoryInfo di, string sourceDirectory, string directory, scanDirParams scanDirParams)
        {
            var entries = di.GetFileSystemInfos();
            bool match = false;
            for (int i = 0; i < entries.Length; ++i)
            {
                if (!CheckHidden(entries[i]))
                {
                    entries[i] = null;
                    continue;
                }

                FileInfo fi = entries[i] as FileInfo;
                if (fi != null && scanDirParams.NameFilter.IsMatch(fi.FullName))
                {
                    match = true;
                    continue;
                }

                DirectoryInfo dir = entries[i] as DirectoryInfo;
                if (Recursive && dir != null && scanDirParams.DirFilter.IsMatch(dir.FullName))
                {
                    match = true;
                    continue;
                }
                entries[i] = null;
            }

            if (sourceDirectory != directory && (match || EmptyDirectories))
            {
                ZipEntry entry = scanDirParams.EntryFactory.MakeDirectoryEntry(directory);
                scanDirParams.Zip.PutNextEntry(entry);
            }
            
            for (int i = 0; i < entries.Length; ++i)
            {
                FileInfo fi = entries[i] as FileInfo;
                DirectoryInfo dir = entries[i] as DirectoryInfo;
                object r = null;
                if (fi != null)
                {
                    r = ProcessComplete(new FileOrDirectoryInfo(fi), new ZipFSEntry(scanDirParams.EntryFactory, fi, ZipTime), false,
                                          skip2 =>
                                              {
                                                  if (!skip2)
                                                  {
                                                      using (Stream stream = Context.OpenReadStream(fi.FullName))
                                                      {
                                                          byte[] buffer=new byte[16384];
                                                          ZipEntry zentry = scanDirParams.EntryFactory.MakeFileEntry(fi.FullName);
                                                          scanDirParams.Zip.PutNextEntry(zentry);
                                                          StreamUtils.Copy(stream, scanDirParams.Zip, buffer, scanDirParams.Progress, ProgressInterval, this, fi.FullName);
                                                      }
                                                  }
                                                  return null;
                                              });
                }
                else if (dir != null)
                {
                    r = ProcessComplete(new FileOrDirectoryInfo(dir), new ZipFSEntry(scanDirParams.EntryFactory, dir, ZipTime), false,
                                          skip2 =>
                                              {
                                                  if (!skip2)
                                                  {
                                                      return scanDir(sourceDirectory, dir.FullName, scanDirParams);
                                                  }
                                                  return null;
                                              });
                }
                if (r != null)
                    return r;
            }
            return null;
        }
    }
}