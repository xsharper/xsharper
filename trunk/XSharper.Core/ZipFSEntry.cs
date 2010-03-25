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
using ICSharpCode.SharpZipLib.Zip;

namespace XSharper.Core
{
    /// <summary>
    /// Adds IFileSystemInfo interface to ZipEntry
    /// </summary>
    public class ZipFSEntry : IFileSystemInfo
    {
        private readonly ZipEntry _ze;
        private readonly ZipTime _ztime;
        private readonly bool _exists;
        
        /// <summary>
        /// Create from existing ZipEntry 
        /// </summary>
        /// <param name="ze"></param>
        /// <param name="ztime"></param>
        public ZipFSEntry(ZipEntry ze, ZipTime ztime)
        {
            _ze = ze;
            _ztime = ztime;
            _exists = true;
        }

        /// <summary>
        /// Create from a file
        /// </summary>
        /// <param name="ef"></param>
        /// <param name="original"></param>
        /// <param name="ztime"></param>
        public ZipFSEntry(IEntryFactory ef, FileSystemInfo original, ZipTime ztime)
        {
            _ze = null;
            _ztime = ztime;
            if (original is FileInfo)
                _ze = ef.MakeFileEntry(original.FullName, true);
            else
            {
                string nobs = Utils.BackslashRemove(original.FullName);
                _ze = ef.MakeDirectoryEntry(nobs, true);
            }
        }

        ///<summary>
        /// Creation time (local)
        ///</summary>
        public DateTime CreationTime { get { return getTime(true); } }
        ///<summary>
        /// Creation time (UTC)
        ///</summary>
        public DateTime CreationTimeUtc { get { return getTime(false); } }

        ///<summary>
        /// Modification time (local). 
        ///</summary>
        public DateTime LastWriteTime { get { return getTime(true); } }

        ///<summary>
        /// Modification time (UTC)
        ///</summary>
        public DateTime LastWriteTimeUtc { get { return getTime(false); } }

        ///<summary>
        /// Last access time (local). 
        ///</summary>
        public DateTime LastAccessTime { get { return getTime(true); } }

        ///<summary>
        /// Last access time (UTC). 
        ///</summary>
        public DateTime LastAccessTimeUtc { get { return getTime(false); } }


        DateTime getTime(bool local)
        {
            
            DateTime? dt = EntryTimeUtc;
            if (dt == null)
                dt=DateTime.UtcNow;
            if (local)
                return dt.Value.ToLocalTime();
            return dt.Value;
        }
        private DateTime? EntryTimeUtc
        {
            get
            {
                if (_ze==null)
                    return null;
                
                DateTime? t = null;
                switch (_ztime)
                {
                    case ZipTime.FileTime:
                        t = _ze.DateTime.ToUniversalTime();
                        break;
                    case ZipTime.UtcFileTime:
                        t = new DateTime(_ze.DateTime.Year,
                                         _ze.DateTime.Month,
                                         _ze.DateTime.Day,
                                         _ze.DateTime.Hour,
                                         _ze.DateTime.Minute,
                                         _ze.DateTime.Second,
                                         _ze.DateTime.Millisecond, DateTimeKind.Utc);
                        break;
                    default:
                        return null;
                }
                return t;
            }
        }


        /// <summary>
        /// true if this is a directory
        /// </summary>
        public bool IsDirectory
        {
            get { return _ze.IsDirectory; }
        }


        /// <summary>
        /// true if this is a file
        /// </summary>
        public bool IsFile
        {
            get { return _ze.IsFile; }
        }

        /// <summary>
        /// true if this file or directory exists
        /// </summary>
        public bool Exists { get { return _exists; }}

        /// <summary>
        /// Length of file in bytes, -1 for directories
        /// </summary>
        public long Length{get{return _ze.Size;}}


        /// <summary>
        /// File name (w/o path)
        /// </summary>
        public string Name
        {
            get
            {
                return Path.GetFileName(FullName);
            }
        }

        /// <summary>
        /// Complete file name (with path)
        /// </summary>
        public string FullName
        {
            get
            {
                var name = _ze.Name;
                if (name.Length==0 || (name[0]!='/' && name[0]!='\\'))
                    name = "/" + name;
                name = name.Replace('/', '\\');
                return name;
            }
        }

        /// <summary>
        /// File extension, starting with . 
        /// </summary>
        public string Extension { get { return Path.GetExtension(FullName); } }

        /// ZIP entry associated with this entry (may be null)
        public ZipEntry ZipEntry { get { return _ze;  } }

        /// File attributes
        public FileAttributes Attributes 
        {
            get
            {
                FileAttributes attr = (_ze.IsDOSEntry ? (FileAttributes) _ze.ExternalFileAttributes : FileAttributes.Normal);
                if (_ze.IsDirectory)
                    attr |= FileAttributes.Directory;
                return attr;

            }
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object
        public override string ToString()
        {
            return FullName;
        }
    }
}