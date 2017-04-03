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
using System.Collections.Generic;

namespace XSharper.Core
{
    /// Run a callback block for every matching file or directory
    [XsType("dir", ScriptActionBase.XSharperNamespace)]
    [Description("Run a callback block for every matching file or directory")]
    public class Dir : ActionWithFilters
    {
        /// Where to start searching (default: current directory)
        [Description("Where to start searching")]
        public string From { get; set; }

        /// true, if directories should be scanned recursively
        [Description("true, if directories should be scanned recursively")]
        public bool Recursive { get; set; }

        /// true, if files should be included
        [Description("true, if files should be listed")]
        public bool Files { get; set; }

        /// true, if directories should be included
        [Description("true, if directories should be listed")]
        public bool Directories { get; set; }

        /// <summary>
        /// How to sort the found files and directories. Uses the same letters as CMD.EXE DIR 
        /// 
        /// N  By name (alphabetic)       S  By size (smallest first)
        /// E  By extension (alphabetic)  D  By date/time (oldest first)
        /// G  Group directories first    -  Prefix to reverse order
        /// 
        /// plus few more:
        /// A  Access time
        /// C  Creation time
        /// W  Modification time (same as D)
        /// 
        /// Default value is 'GN'
        /// </summary>
        [Description("How to sort the found files and directories. Uses the same letters as CMD.EXE DIR ")]
        public string Sort { get; set; }

        /// <summary>
        /// Block to execute if nothing was found
        /// </summary>
        [XsElement("noMatch", SkipIfEmpty = true, Ordering = -1)]
        [Description("Block to execute if nothing was found")]
        public Block NoMatch { get; set; }

        /// Constructor
        public Dir()
        {
            Files = true;
            Sort = "GN";
            From = ".";
        }
        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            return base.ForAllChildren(func,isFind) || func(NoMatch);
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Context.Initialize(NoMatch);
        }

        /// Execute action
        public override object Execute()
        {
            string path = Context.TransformStr(From, Transform);

            
            object ret = null;
            Dirctx ctx = new Dirctx
                             {
                                 DirFilter = new FullPathFilter(Syntax, Context.TransformStr(DirectoryFilter, Transform), BackslashOption.Add), 
                                 NameFilter = new FileNameOnlyFilter(Syntax, Context.TransformStr(Filter, Transform)), 
                                 Entries = 0
                             };

            if (File.Exists(path))
                ret = listEntry(ctx, new FileInfo(path));
            else
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                if (directory.Exists)
                {
                    bool files = (ctx.DirFilter == null || ctx.DirFilter.IsMatch(directory.FullName));
                    ret = dir(ctx, directory,files);
                }
            }
            if (ctx.Entries==0)
            {
                if (NoMatch != null)
                    ret=Context.Execute(NoMatch);
            }
            if (ReturnValue.IsBreak(ret))
                return null;
            ReturnValue.RethrowIfException(ret);
            return ret;
        }
        
        private class Dirctx
        {
            public int Entries;
            public IStringFilter NameFilter;
            public IStringFilter DirFilter;
        }
        private object dir(Dirctx ctx, DirectoryInfo directoryInfo, bool listFiles)
        {
            return ProcessPrepare(new FileOrDirectoryInfo(directoryInfo), null,
                                    delegate
                                        {
                                            FileSystemInfo[] contents = directoryInfo.GetFileSystemInfos();
                                            Array.Sort(contents, comparison);

                                            object r = null;
                                            List<DirectoryInfo> rec = new List<DirectoryInfo>();

                                            // Display
                                            foreach (FileSystemInfo fs in contents)
                                            {
                                                DirectoryInfo di = (fs as DirectoryInfo);
                                                if (di != null)
                                                {
                                                    r = listEntry(ctx, di);
                                                    if (Recursive && CheckHidden(di))
                                                        rec.Add(di);
                                                    if (r != null)
                                                        return r;
                                                    continue;
                                                }
                                                FileInfo fi = (fs as FileInfo);
                                                if (fi == null || !listFiles)
                                                    continue;
                                                r = listEntry(ctx, fi);
                                                if (r != null)
                                                    return r;
                                            }


                                            foreach (DirectoryInfo di in rec)
                                            {
                                                DirectoryInfo di1 = di;
                                                object ret = dir(ctx, di1, true);
                                                if (ret != null)
                                                    return ret;
                                            }

                                            return null;
                                        });
        }


        private int comparison(FileSystemInfo x, FileSystemInfo y)
        {
            int sign = 1;
            
            foreach (char c in Sort.ToLower().ToCharArray())
            {
                long n=0;
                switch (c)
                {
                    case '-':
                        sign = -sign;
                        break;
                    case 'n':
                        n = string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                        break;
                    case 'e':
                        n = string.Compare(x.Extension, y.Extension, StringComparison.CurrentCultureIgnoreCase);
                        break;
                    case 's':
                        {
                            FileInfo fi1 = x as FileInfo;
                            FileInfo fi2 = y as FileInfo;
                            long len1 = (fi1 == null) ? -1 : fi1.Length;
                            long len2 = (fi2 == null) ? -1 : fi2.Length;
                            n = len1 - len2;
                            break;
                        }
                    case 'g':
                        {
                            FileInfo fi1 = x as FileInfo;
                            FileInfo fi2 = y as FileInfo;
                            if (fi1 == null && fi2 != null)
                                n = -1;
                            else if (fi1 != null && fi2 == null)
                                n = 1;
                            break;
                        }
                    case 'c':
                        n = (x.CreationTime - y.CreationTime).Ticks;
                        break;
                    case 'a':
                        n = (x.LastAccessTime - y.LastAccessTime).Ticks;
                        break;
                    case 'w':
                    case 'd':
                        n = (x.LastWriteTime - y.LastWriteTime).Ticks;
                        break;
                }
                if (n!=0)
                    return Math.Sign(n*sign);
                if (c != '-')
                    sign = 1;
            }
            return 0;
        }

        private object listEntry(Dirctx ctx, FileSystemInfo fsi)
        {
            if (!CheckHidden(fsi))
                return null;
            
            if (fsi is FileInfo)
            {
                if (!Files)
                    return null;

                if (ctx.DirFilter != null && !ctx.DirFilter.IsMatch(fsi.FullName))
                {
                    VerboseMessage("{0} did not pass directory filter", fsi.FullName);
                    return null;
                }
                if (ctx.NameFilter != null && !ctx.NameFilter.IsMatch(fsi.FullName))
                {
                    return null;
                }
            }
            if (fsi is DirectoryInfo)
            {
                if (!Directories)
                    return null;
                if (ctx.DirFilter != null && !ctx.DirFilter.IsMatch(fsi.FullName))
                {
                    return null;
                }
                
            }
            
            ctx.Entries++;
            object r = ProcessComplete(new FileOrDirectoryInfo(fsi), null, false, delegate(bool s) {return null; });
            return r;
        }
    }
}