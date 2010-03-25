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
using System.IO;
using XSharper.Core;

namespace XSharper.Core
{
    /// <summary>
    /// File or directory information interface. 
    /// 
    /// It has fields with similar meaning to <see cref="FileSystemInfo"/>, but is an interface. This is needed to provide the same fields to entries in ZIP archives.
    /// </summary>
    public interface IFileSystemInfo
    {
        /// <summary>
        /// true if this is a directory
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// true if this is a file
        /// </summary>
        bool IsFile { get; }

        /// <summary>
        /// true if this file or directory exists
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Length of file in bytes, -1 for directories
        /// </summary>
        long Length { get; }

        /// <summary>
        /// File name (w/o path)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Complete file name (with path)
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// File extension, starting with . 
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// File attributes
        /// </summary>
        FileAttributes Attributes { get; }

        ///<summary>
        /// Creation time (local)
        ///</summary>
        DateTime CreationTime { get; }
        ///<summary>
        /// Creation time (UTC)
        ///</summary>
        DateTime CreationTimeUtc { get; }

        ///<summary>
        /// Modification time (local). 
        ///</summary>
        DateTime LastWriteTime { get; }

        ///<summary>
        /// Modification time (UTC)
        ///</summary>
        DateTime LastWriteTimeUtc { get; }

        ///<summary>
        /// Last access time (local). 
        ///</summary>
        DateTime LastAccessTime { get; }

        ///<summary>
        /// Last access time (UTC). 
        ///</summary>
        DateTime LastAccessTimeUtc { get; }

    }

    /// <summary>
    /// <see cref="FileSystemInfo"/> to <see cref="IFileSystemInfo"/> adapter.
    /// </summary>
    public class FileOrDirectoryInfo : IFileSystemInfo
    {
        private readonly FileSystemInfo _fsi;

        /// Constructor
        public FileOrDirectoryInfo(FileSystemInfo fsi)
        {
            _fsi = fsi;
        }

        /// <summary>
        /// true if this is a directory
        /// </summary>
        public bool IsDirectory { get { return _fsi is DirectoryInfo; } }

        /// <summary>
        /// true if this is a file
        /// </summary>
        public bool IsFile { get { return _fsi is FileInfo; } }

        /// <summary>
        /// true if this file or directory exists
        /// </summary>
        public bool Exists { get { return _fsi.Exists; } }

        /// <summary>
        /// Length of file in bytes, -1 for directories
        /// </summary>
        public long Length { get { return IsFile ? ((FileInfo)_fsi).Length : -1; } }

        /// <summary>
        /// File name (w/o path)
        /// </summary>
        public string Name { get { return _fsi.Name; } }

        /// <summary>
        /// Complete file name (with path)
        /// </summary>
        public string FullName { get { return IsDirectory ? Utils.BackslashAdd(_fsi.FullName) : _fsi.FullName; } }

        /// <summary>
        /// File extension, starting with . 
        /// </summary>
        public string Extension { get { return _fsi.Extension; } }

        /// <summary>
        /// File attributes
        /// </summary>
        public FileAttributes Attributes { get { return _fsi.Attributes; } }

        ///<summary>
        /// Creation time (local)
        ///</summary>
        public DateTime CreationTime { get { return _fsi.CreationTime; } }

        ///<summary>
        /// Creation time (UTC)
        ///</summary>
        public DateTime CreationTimeUtc { get { return _fsi.CreationTimeUtc; } }

        ///<summary>
        /// Modification time (local). 
        ///</summary>
        public DateTime LastWriteTime { get { return _fsi.LastWriteTime; } }

        ///<summary>
        /// Modification time (UTC)
        ///</summary>
        public DateTime LastWriteTimeUtc { get { return _fsi.LastWriteTimeUtc; } }

        ///<summary>
        /// Last access time (local). 
        ///</summary>
        public DateTime LastAccessTime { get { return _fsi.LastAccessTime; } }

        ///<summary>
        /// Last access time (UTC). 
        ///</summary>
        public DateTime LastAccessTimeUtc { get { return _fsi.LastAccessTimeUtc; } }

        /// Get internal FileSystemInfo object
        public FileSystemInfo Info { get { return _fsi; } }

        /// Return FullName
        public override string ToString()
        {
            return _fsi.FullName;
        }
    }
}