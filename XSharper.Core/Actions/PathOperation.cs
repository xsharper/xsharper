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
    /// <summary>
    /// Path operation to perform
    /// </summary>
    public enum PathOperationType
    {
        /// Do not change path
        [Description("Do not change path")]
        None,

        /// Return Path.GetFullPath(path)
        [Description("Return Path.GetFullPath(path)")]
        GetFullPath,

        /// Return Path.GetDirectoryName(path)
        [Description("Return Path.GetDirectoryName(path)")]
        GetDirectoryName,

        /// Return Path.GetExtension(path)
        [Description("Return Path.GetExtension(path)")]
        GetExtension,

        /// Return Path.GetFileName(path)
        [Description("Return Path.GetFileName(path)")]
        GetFileName,

        /// Return Path.GetFileName(path)
        [Description("Return Path.GetFileName(path)")]
        GetFileNameWithoutExtension,

        /// Return Path.GetRandomFileName(). path parameter is ignored.
        [Description("Return Path.GetRandomFileName(). path parameter is ignored.")]
        GetRandomFileName,

        /// Return Path.GetTempFileName(). path parameter is ignored.
        [Description("Return Path.GetTempFileName() is path parameter is specified, or GetTempPath()+path otherwise.")]
        GetTempFileName,

        /// Return Path.Combine(path,param). 
        [Description("Return Path.Combine(path,param). ")]
        Combine,

        /// Return new DirectoryInfo(path).FullName
        [Description("Return new DirectoryInfo(path).FullName")]
        ToDirectoryInfo,

        /// Return new FileInfo(path).FullName
        [Description("Return new FileInfo(path).FullName")]
        ToFileInfo,

        /// Return new DirectoryInfo(path).Parent.FullName
        [Description("Return new DirectoryInfo(path).Parent.FullName")]
        ToDirectoryInfoParent,

        /// Return new FileInfo(path).DirectoryName
        [Description("Return new FileInfo(path).DirectoryName")]
        ToFileInfoDirectoryName,

        /// Search for file param in semicolon-separated path string, provided in path
        [Description("Search for file param in semicolon-separated path string, provided in path")]
        FindFileInPath,

        /// Returns Path.GetTempPath()
        [Description("Returns Path.GetTempPath()")]
        GetTempPath,

        /// Returns Path.ChangeExtension(path,param)
        [Description("Returns Path.ChangeExtension(path,param)")]
        ChangeExtension
    }

    /// <summary>
    /// Enumeration specifying how to check file existence
    /// </summary>
    public enum Existence
    {
        /// No existence check
        [Description("No existence check")]
        NoCheck,

        /// Check that file or directory 'path' does NOT exists
        [Description("Check that file or directory 'path' does NOT exists")]
        NotExists,

        /// Check that file or directory 'path' exists
        [Description("Check that file or directory 'path' exists")]
        Exists,

        /// Check that file 'path' exists
        [Description("Check that file 'path' exists")]
        FileExists,

        /// Check that directory 'path' exists
        [Description("Check that directory 'path' exists")]
        DirectoryExists,

        /// Create empty file
        [Description("Create empty file")]
        CreateFile,

        /// Create empty directory, if it does not exist
        [Description("Create empty directory, if it does not exist")]
        CreateDirectory
    }


    /// <summary>
    /// Do filename transformations with possibility to add/remove trailing backslashes and check file/directory existence
    /// </summary>
    [XsType("path", ScriptActionBase.XSharperNamespace)]
    [Description("Do filename transformations with possibility to add/remove trailing backslashes and check file/directory existence")]
    public class PathOperation : ScriptActionBase
    {
        /// Path parameter 
        [Description("Path parameter ")]
        [XsAttribute("path")]
        [XsAttribute("value")]
        [XsAttribute("")]
        public string Value { get; set; }

        /// True if Transform property should be ignored and value used verbatim (it would still apply to string attributes)
        [Description("True if Transform property should be ignored and value used verbatim (it would still apply to string attributes)")]
        public bool Verbatim { get; set; }


        /// Additional argument for operations that require it
        [Description("Additional argument for operations that require it")]
        public string Param { get; set; }

        /// Operation to perform, required
        [Description("Operation to perform, required")]
        [XsRequired]
        public PathOperationType Operation { get; set; }

        /// Where to output the result
        [Description("Where to output the result")]
        public string OutTo { get; set; }

        /// Whether to check existence of the the resulted file/directory. Default NoCheck
        [Description("Whether to check existence of the the resulted file/directory")]
        public Existence Existence { get; set; }

        /// Whether to append backslashes to the result. Default AsIs
        [Description("Whether to append backslashes to the result")]
        public BackslashOption Backslash { get; set; }

        /// Constructor
        public PathOperation()
        {
            Existence = Existence.NoCheck;
            Backslash = BackslashOption.AsIs;
        }
        /// Execute action
        public override object Execute()
        {
            string path = Verbatim?Value:Context.TransformStr(Value, Transform);
            string parm = Context.TransformStr(Param, Transform);

            string ret = path;
            switch (Operation)
            {
                case PathOperationType.GetFullPath:
                    ret = System.IO.Path.GetFullPath(path);
                    break;
                case PathOperationType.GetDirectoryName:
                    ret = System.IO.Path.GetDirectoryName(path);
                    break;
                case PathOperationType.GetExtension:
                    ret = System.IO.Path.GetExtension(path);
                    break;
                case PathOperationType.GetFileName:
                    ret = System.IO.Path.GetFileName(path);
                    break;
                case PathOperationType.GetFileNameWithoutExtension:
                    ret = System.IO.Path.GetFileNameWithoutExtension(path);
                    break;
                case PathOperationType.GetTempFileName:
                    if (string.IsNullOrEmpty(path))
                        ret = System.IO.Path.GetTempFileName();
                    else
                        ret = System.IO.Path.Combine(System.IO.Path.GetTempPath(),path);
                    break;
                case PathOperationType.GetTempPath:
                    ret = System.IO.Path.GetTempPath();
                    break;
                case PathOperationType.GetRandomFileName:
                    ret = System.IO.Path.GetRandomFileName();
                    break;
                case PathOperationType.Combine:
                    ret = System.IO.Path.Combine(path, parm);
                    break;
                case PathOperationType.ToDirectoryInfoParent:
                    DirectoryInfo di= new DirectoryInfo(path);
                    if (di.Parent==null)
                        throw new DirectoryNotFoundException(string.Format("Directory {0} does not have a parent directory", path));
                    ret=di.Parent.FullName;
                    break;
                case PathOperationType.ToDirectoryInfo:
                    ret = new DirectoryInfo(path).FullName;
                    break;
                case PathOperationType.ToFileInfoDirectoryName:
                    ret = new FileInfo(path).DirectoryName;
                    break;
                case PathOperationType.ToFileInfo:
                    ret = new FileInfo(path).FullName;
                    break;
                case PathOperationType.FindFileInPath:
                    ret = Context.SearchPath(parm, path);
                    break;
                case PathOperationType.ChangeExtension:
                    ret = System.IO.Path.ChangeExtension(path, parm);
                    break;
                case PathOperationType.None:
                    ret = path;
                    break;
                default:
                    throw new InvalidOperationException("Invalid operation");
            }
            ret = Utils.Backslash(ret, Backslash);
            switch (Existence)
            {
                case Existence.NoCheck:
                    break;
                case Existence.NotExists:
                    if (File.Exists(ret))
                        throw new InvalidOperationException(string.Format("File {0} already exists", ret));
                    if (Directory.Exists(ret))
                        throw new InvalidOperationException(string.Format("Directory {0} already exists", ret));
                    break;
                case Existence.Exists:
                    if (!File.Exists(ret) && !Directory.Exists(ret))
                        throw new FileNotFoundException(string.Format("File or directory {0} not found", ret));
                    break;
                case Existence.DirectoryExists:
                    if (!new DirectoryInfo(ret).Exists)
                        throw new DirectoryNotFoundException(string.Format("Directory {0} not found", ret));
                    break;
                case Existence.FileExists:
                    if (!new FileInfo(ret).Exists)
                        throw new FileNotFoundException(string.Format("File {0} not found", ret),ret);
                    break;
                case Existence.CreateDirectory:
                    new DirectoryInfo(ret).Create();
                    break;
                case Existence.CreateFile:
                    new FileInfo(ret).Create();
                    break;
                default:
                    throw new InvalidOperationException("Invalid existence");
            }
            Context.OutTo(Context.TransformStr(OutTo, Transform), ret);
            return null;
        }

        
    }
}