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
using System.Collections;
using System.Collections.Generic;

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
        public string To { get; set; }

        /// Location where to save the compressed data. 
        [Description("Location where to save the compressed data. ")]
        public string OutTo { get; set; }

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
                nf = new SelfIgnoreFilenameOnlyFilter(trg, FilterSyntax.Pattern, Regex.Escape(Path.GetFileName(f)));
                df = new StringFilter(null);
            }
            else
            {
                src = new DirectoryInfo(f);
                nf = new SelfIgnoreFilenameOnlyFilter(trg, Syntax, Context.TransformStr(Filter, Transform));
                df = new FullPathFilter(Syntax, Context.TransformStr(DirectoryFilter, Transform), BackslashOption.Add);
            }

            if (!src.Exists)
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist", src));

            bool deleteTrg = false;
            Stream str = null; 
            var outTo = Context.TransformStr(OutTo, Transform);
            try
            {
                if (outTo != null)
                    str = new MemoryStream();
                else
                {
                    str = Context.CreateStream(trg);
                    deleteTrg = true;
                }

                var ret = createZip(str, src.FullName, nf, df);
                if (trg != null && outTo != null)
                {
                    deleteTrg = true;
                    using (var ctx2 = Context.CreateStream(trg))
                        ((MemoryStream)str).WriteTo(ctx2);
                }
                if (outTo != null)
                    Context.OutTo(outTo, ((MemoryStream)str).ToArray());
                deleteTrg = false;
                if (ReturnValue.IsBreak(ret))
                    return null;
                ReturnValue.RethrowIfException(ret);
                return ret;
            }
            finally
            {
                if (str != null)
                    str.Dispose();

                if (deleteTrg)
                    File.Delete(trg);
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
                
                return scanDir(sourceDirectory, new scanDirParams(zip, sourceDirectory, entryFactory, progress, nf, df));
            }
        }

        class scanDirParams
        {
            public scanDirParams(ZipOutputStream zip, string sourceDirectory, ZipEntryFactory entryFactory, ProgressHandler progress, IStringFilter nf, IStringFilter df)
            {
                Zip = zip;
                EntryFactory = entryFactory;
                Progress = progress;
                NameFilter = nf;
                DirFilter = df;
                DirEntries = new Dictionary<string, ZipFSEntry>(StringComparer.InvariantCultureIgnoreCase);
                SourceDirectory = Utils.BackslashAdd(new DirectoryInfo(sourceDirectory).FullName);
                Buffer = new byte[16384];
            }
            public readonly IDictionary<string, ZipFSEntry> DirEntries ;
            public readonly ZipOutputStream Zip;
            public readonly ZipEntryFactory EntryFactory;
            public readonly ProgressHandler Progress;
            public readonly IStringFilter NameFilter;
            public readonly IStringFilter DirFilter;
            public readonly string SourceDirectory;
            public readonly byte[] Buffer;
        }

        object scanDir(string directory, scanDirParams scanDirParams)
        {
            directory = Utils.BackslashAdd(directory);
            bool isRoot = (scanDirParams.SourceDirectory == directory);
            
            DirectoryInfo di = new DirectoryInfo(directory);
            if (!CheckHidden(di) && !isRoot)
                return null;
            
            bool processFiles = true;
            if (!scanDirParams.DirFilter.IsMatch(directory))
            {
                processFiles = false;
                VerboseMessage("{0} did not pass directory filter", directory);
            }

            // Process files in this directory
            if (processFiles)
            {
                foreach (FileInfo f in di.GetFiles())
                {
                    object r = compressSingleFile(f,scanDirParams);
                    if (r != null)
                        return r;
                }
                if (EmptyDirectories)
                {
                    object r = ensureDirectoryExists(di, scanDirParams);
                    if (r != null)
                        return r;
                }
            }
            if (Recursive)
            {
                foreach (DirectoryInfo d in di.GetDirectories())
                {
                    object r = scanDir(d.FullName, scanDirParams);
                    if (r != null)
                        return r;
                }
            }

            return null;
        }

        object compressSingleFile(FileInfo fi, scanDirParams scanDirParams)
        {
            if (!CheckHidden(fi) || !scanDirParams.NameFilter.IsMatch(fi.FullName))
                return null;

            object r=ensureDirectoryExists(fi.Directory, scanDirParams);
            if (r != null || scanDirParams.DirEntries[Utils.BackslashAdd(fi.Directory.FullName)] == null)
                return r;

            var ze=new ZipFSEntry(scanDirParams.EntryFactory, fi, ZipTime);
            r = ProcessPrepare(new FileOrDirectoryInfo(fi), ze, () => null);
            if (r != null)
                return r;
            
            r = ProcessComplete(new FileOrDirectoryInfo(fi), ze, false,
                                          skip2 =>
                                          {
                                              if (!skip2)
                                              {
                                                  using (Stream stream = Context.OpenStream(fi.FullName))
                                                  {
                                                      ZipEntry zentry = scanDirParams.EntryFactory.MakeFileEntry(fi.FullName);
                                                      scanDirParams.Zip.PutNextEntry(zentry);
                                                      StreamUtils.Copy(stream, scanDirParams.Zip, scanDirParams.Buffer, scanDirParams.Progress, ProgressInterval, this, fi.FullName);
                                                  }
                                              }
                                              return null;
                                          });
            return r;
        }
        object ensureDirectoryExists(DirectoryInfo di, scanDirParams scanDirParams)
        {
            var s = Utils.BackslashAdd( di.FullName);
            if (scanDirParams.DirEntries.ContainsKey(s))
                return null;

            bool isRoot = (scanDirParams.SourceDirectory == s);
            object r;
            if (!isRoot)
            {
                var p = di.Parent;
                if (p != null)
                {
                    r = ensureDirectoryExists(di.Parent, scanDirParams);
                    if (r != null || scanDirParams.DirEntries[Utils.BackslashAdd(di.Parent.FullName)] == null)
                        return r;
                }
            }

            ZipFSEntry ze = null;
            if (isRoot)
            {
                ZipEntry zen = new ZipEntry("");
                zen.ExternalFileAttributes |= 16;
                ze = new ZipFSEntry(zen, ZipTime);
            }
            else
                ze = new ZipFSEntry(scanDirParams.EntryFactory, di, ZipTime);

            r=ProcessPrepare(new FileOrDirectoryInfo(di), ze, () =>
            {
                scanDirParams.DirEntries[s] = ze;
                return null;
            });
            if (r == null)
                r = ProcessComplete(new FileOrDirectoryInfo(di), ze, false, skip => 
                {
                    scanDirParams.DirEntries[s] = skip?null:ze;
                    if (!skip && !isRoot)
                        scanDirParams.Zip.PutNextEntry(isRoot?new ZipEntry(string.Empty):scanDirParams.EntryFactory.MakeDirectoryEntry(s));
                    return null;
                });
            return r;
        }
    }
}