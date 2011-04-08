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
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;

namespace XSharper.Core
{
    /// <summary>
    /// Unzip an archive to the specified directory
    /// </summary>
    [XsType("unzip", ScriptActionBase.XSharperNamespace)]
    [Description("Unzip an archive to the specified directory")]
    public class Unzip : ZipBase
    {
        /// .ZIP archive filename or byte[]
        [Description(".ZIP archive filename or byte[]")]
        public string From { get; set; }

        /// Directory where to extract the files
        [Description("Directory where to extract the files")]
        public string To { get; set; }

        /// What to do with existing files. Default value - overwrite always.
        [Description("What to do with existing files")]
        public OverwriteMode Overwrite { get; set; }

        /// true, if all files should be deleted from the directory before extracting
        [Description("true, if all files should be deleted from the directory before extracting")]
        public bool Clean { get; set; }

        /// True, if path in the archive affects extraction directory
        [Description("True, if path in the archive affects extraction directory")]
        public bool UsePath { get; set; }

        /// false, if files are not to be extracted but just listed
        [Description("false, if files are not to be extracted but just listed")]
        public bool Extract { get; set; }


        /// Constructor
        public Unzip()
        {
            Overwrite = OverwriteMode.Always;
            UsePath = true;
            Extract = true;
        }

        /// Execute action
        public override object Execute()
        {
            DirectoryInfo d = new DirectoryInfo(Context.TransformStr(To, Transform));
            d.Create();
            if (Clean)
            {
                Delete del = new Delete { From = d.ToString(), Recursive = true, ReadOnly = true };
                Context.Execute(del);
            }

            var zip = Context.Transform(From, Transform);
            
        
            var nf = new FileNameOnlyFilter(Syntax, Context.TransformStr(Filter,Transform));
            var df = new StringFilter(Syntax, Context.TransformStr(DirectoryFilter,Transform));

            var ret=extractZip(zip, d.FullName, nf, df);
            if (ReturnValue.IsBreak(ret))
                return null;
            ReturnValue.RethrowIfException(ret);
            return ret;
        }

        object extractZip(   object zipFileName, string rootDirectory,IStringFilter nf, IStringFilter df)
        {
            object ret = null;
            WindowsNameTransform extractNameTransform = new WindowsNameTransform(rootDirectory);
            Dictionary<string, bool> dirs = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

            Stream str;
            if (zipFileName is byte[])
                str = new MemoryStream((byte[])zipFileName);
            else
                str=new SeekableStream(Context.OpenStream(zipFileName.ToString()),true);
            using (ZipFile zip = new ZipFile(str))
            {
                if (Password != null)
                    zip.Password = Context.TransformStr(Password, Transform);

                foreach (ZipEntry entry in zip)
                {
                    
                    string targetName = null;
                    if (entry.IsFile)
                    {
                        targetName = extractNameTransform.TransformFile(entry.Name);
                        if (!UsePath)
                            targetName = Path.Combine(rootDirectory, Path.GetFileName(targetName));
                    }
                    else if (entry.IsDirectory)
                    {
                        if (UsePath)
                            targetName = extractNameTransform.TransformDirectory(entry.Name);
                        else
                            targetName = rootDirectory;
                    }
                    if (string.IsNullOrEmpty(targetName))
                        continue;
                    if (!Hidden)
                    {   
                        if (isDos(entry) && (((FileAttributes) entry.ExternalFileAttributes) & (FileAttributes.Hidden)) != 0)
                            continue;
                    }
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;
                    var n = new ZipFSEntry(entry, ZipTime);
                    if ((entry.IsFile && df.IsMatch(Path.GetDirectoryName(n.FullName)) && nf.IsMatch(n.Name)) ||
                        (entry.IsDirectory && df.IsMatch(n.FullName)))
                    {
                        object r = extract(zip, rootDirectory, targetName, entry, dirs);
                        if (r!=null)
                            return r;
                    }
                }
            }
            return ret;
        }

        object extract(ZipFile zip, string rootDirectory,string targetName, ZipEntry entry, Dictionary<string, bool> dirs)
        {
            string dirName;
            if (UsePath)
                dirName = (entry.IsFile) ? Path.GetDirectoryName(Path.GetFullPath(targetName)) : targetName;
            else
                dirName = rootDirectory;

            // Get date time from entry
            DateTime? entryTimeUtc = getEntryTimeUtc(entry);

            // Create directory
            bool skip = false;
            string zipDirName = string.Empty;
            ZipEntry dirZipEntry = null;
        
            if (entry.IsFile)
            {
                zipDirName = Path.GetDirectoryName(entry.Name);
                dirZipEntry = zip.GetEntry(zipDirName);
                if (dirZipEntry == null)
                    dirZipEntry = zip.GetEntry(zipDirName + "/");
            }
            else
            {
                zipDirName = entry.Name;
                dirZipEntry = entry;
            }
            
            if (dirs.TryGetValue(zipDirName, out skip) && skip)
                return null;

            
            if (entry.IsDirectory)
            {
                DirectoryInfo dir = new DirectoryInfo(dirName);
                var zz = new ZipFSEntry(entry, ZipTime);
                var to=new FileOrDirectoryInfo(dir);
                object r = ProcessPrepare(zz, to, () => null);
                if (r==null)
                    r = ProcessComplete(new ZipFSEntry(entry, ZipTime), new FileOrDirectoryInfo(dir), skip, skp=>{
                        dirs[zipDirName] = skip = skp;
                        if (Extract && !skp)
                        {
                            DirectoryInfo di = Directory.CreateDirectory(dirName);
                            setAttributes(di,entry);
                        }
                        return null;
                    });
                if (r != null || skip)
                    return r;                
            }
            if (entry.IsFile)
            {
                
                
                var pfrom = new ZipFSEntry(entry, ZipTime);
                var fi = new FileInfo(targetName);
                var pto = new FileOrDirectoryInfo(fi);
                if (fi.Exists)
                {
                    if (Overwrite == OverwriteMode.Never)
                        skip = true;
                    if (Overwrite == OverwriteMode.IfNewer)
                    {
                        if (entryTimeUtc == null || entryTimeUtc < File.GetLastWriteTimeUtc(targetName))
						{
                            VerboseMessage("Ignoring never file {0}", targetName);
                            skip = true;
						}
                    }
                }

                if ((skip && Overwrite != OverwriteMode.Confirm))
                    return null;
                object r = ProcessPrepare(pfrom, pto, () => null);
                if (r != null)
                    return r;
                    
                return ProcessComplete(pfrom, pto, skip,
                    sk =>
                        {
                            if (sk || !Extract)
                                return null;

                            if (!fi.Directory.Exists)
                            {
                                DirectoryInfo di = Directory.CreateDirectory(dirName);
                                if (dirZipEntry != null)
                                    setAttributes(di,dirZipEntry);
                            }

                            const FileAttributes mask = (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
                            if (fi.Exists && (fi.Attributes & mask)!=0)
                                fi.Attributes = fi.Attributes & ~mask;

                            using (Stream outputStream = Context.CreateStream(targetName))
                            {
                                StreamUtils.Copy(zip.GetInputStream(entry), outputStream, new byte[16384],
                                                 delegate(object x, ProgressEventArgs y) { Context.OnProgress(1, y.Name); }, ProgressInterval, this, entry.Name, entry.Size);
                            }

                            setAttributes(fi, entry);
                            return null;
                        });
            }
            return null;
        }

        bool isDos(ZipEntry entry)
        {
            // There is some opaque bug with InfoZip or SharpZipLib where zipEntry header is written as PK 0B 17 0A 00
            return entry.IsDOSEntry || entry.HostSystem == 0xb;
        }
        private void setAttributes(FileSystemInfo fi, ZipEntry entry)
        {
            DateTime? entryTimeUtc = getEntryTimeUtc(entry);
            if (entryTimeUtc != null)
                fi.LastWriteTimeUtc = entryTimeUtc.Value;
            if (isDos(entry) && (entry.ExternalFileAttributes != -1))
            {
                const FileAttributes mask = (FileAttributes.Archive | FileAttributes.Normal | FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
                var entryAttributes = (FileAttributes) entry.ExternalFileAttributes;
                fi.Refresh();
                fi.Attributes = (fi.Attributes & ~mask) | (entryAttributes & mask);
            }
        }

        private DateTime? getEntryTimeUtc(ZipEntry entry)
        {
            DateTime? entryTimeUtc;
            switch (ZipTime)
            {
                case ZipTime.FileTime:
                    entryTimeUtc = entry.DateTime.ToUniversalTime();
                    break;
                case ZipTime.UtcFileTime:
                    entryTimeUtc = new DateTime(entry.DateTime.Year,
                                                entry.DateTime.Month,
                                                entry.DateTime.Day,
                                                entry.DateTime.Hour,
                                                entry.DateTime.Minute,
                                                entry.DateTime.Second,
                                                entry.DateTime.Millisecond, DateTimeKind.Utc);
                    ;
                    break;
                default:
                    entryTimeUtc= null;
                    break;
            }
            return entryTimeUtc;
        }
    }
}