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
    /// Delete files and directories
    [XsType("delete", ScriptActionBase.XSharperNamespace)]
    [Description("Delete files and directories")]
    public class Delete : ActionWithFilters
    {
        /// Filename or directory name to delete
        [Description("Filename or directory name to delete")]
        public string From { get; set; }

        /// True, if delete recursively. Default - false.
        [Description("True, if delete recursively. Default - false.")]
        public bool Recursive { get; set; }

        /// True, if read only files must be deleted. Default - false.
        [Description("True, if read only files must be deleted. Default - false.")]
        public bool DeleteReadOnly { get; set; }

        /// True, if the directory specified in From should be deleted. Default - true
        [Description("True, if the directory specified in From should be deleted. Default - true")]
        public bool DeleteRoot { get; set; }

        private class delctx
        {
            public IStringFilter nameFilter;
            public IStringFilter dirFilter;
        }

        /// Constructor
        public Delete()
        {
            DeleteRoot = true;
        }
        /// Execute action
        public override object Execute()
        {
            string fromExpanded = Context.TransformStr(From, Transform);
            if (string.IsNullOrEmpty(fromExpanded))
                return null;

            delctx ctx = new delctx();
            ctx.dirFilter=new FullPathFilter(Syntax, Context.TransformStr(DirectoryFilter, Transform));
            ctx.nameFilter=new FileNameOnlyFilter(Syntax, Context.TransformStr(Filter, Transform));
            
            object ret=null;
            if (File.Exists(fromExpanded))
            {
                FileInfo fr = new FileInfo(fromExpanded);
                ret = deleteSingleFile(ctx, fr);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(fromExpanded);
                if (dir.Exists)
                    ret = delete(ctx, dir, dir);
                else
                    VerboseMessage("{0} not found.", fromExpanded);
            }

            if (ReturnValue.IsBreak(ret))
                return null;
            ReturnValue.RethrowIfException(ret);
            return ret;
        }


        private object delete(delctx ctx, DirectoryInfo root, DirectoryInfo dir)
        {
            if (root != dir && (!CheckHidden(dir) || (ctx.dirFilter != null && !ctx.dirFilter.IsMatch(dir.FullName))))
            {
                VerboseMessage("{0} did not pass directory filter", dir.FullName);
                return null;
            }

            foreach (FileInfo f in dir.GetFiles())
            {
                object ret = deleteSingleFile(ctx, f);
                if (ret != null)
                    return ret;
            }
            if (Recursive)
            {
                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    object ret = delete(ctx, dir, d);
                    if (ret != null)
                        return ret;
                }
            }
            object r = null;
            if ((DeleteRoot || root.FullName != dir.FullName))
            {
                if (dir.GetFiles().Length != 0 || dir.GetDirectories().Length != 0)
                    VerboseMessage("Directory {0} contains files. Skipped.", dir.FullName);
                else
                {
                    bool skip = false;
                    if (!DeleteReadOnly && ((dir.Attributes & FileAttributes.ReadOnly) != 0))
                        skip = true;

                    r = ProcessComplete(new FileOrDirectoryInfo(dir), null, skip, skNew =>
                                                                                        {
                                                                                            skip = skNew;
                                                                                            if (!skip)
                                                                                            {
                                                                                                dir.Attributes = dir.Attributes & ~(FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                                                                                dir.Delete();
                                                                                            }
                                                                                            return null;
                                                                                        });
                }
            }

            return r;
        }

        private object deleteSingleFile(delctx ctx, FileInfo f)
        {
            if (ctx.nameFilter != null && (!ctx.nameFilter.IsMatch(f.FullName) || !CheckHidden(f)))
            {
                VerboseMessage("{0} did not pass filter", f.FullName);
                return null;
            }
            bool skip = false;
            if (!DeleteReadOnly && ((f.Attributes & FileAttributes.ReadOnly) != 0))
                skip = true;

            object ret = ProcessComplete(new FileOrDirectoryInfo(f), null, skip, skipNew =>
                                                                               {
                                                                                   if (!skipNew)
                                                                                   {
                                                                                       VerboseMessage("Deleting {0} ", f.FullName);
                                                                                       if (DeleteReadOnly)
                                                                                           f.Attributes = f.Attributes & ~(FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                                                                       f.Delete();
                                                                                   }
                                                                                   return null;
                                                                               });
            return ret;
        }
    }

 
}