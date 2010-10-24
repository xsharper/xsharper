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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Security.Cryptography;


namespace XSharper.Core
{    
    static partial class NativeMethods
    {
        internal delegate int CopyProgressRoutine(
             long totalFileSize, long TotalBytesTransferred, long streamSize,
             long streamBytesTransferred, int streamNumber, int callbackReason,
             IntPtr sourceFile, IntPtr destinationFile, IntPtr data);

        [Flags]
        internal enum CopyFileExFlags
        {
            None = 0x0,
            COPY_FILE_FAIL_IF_EXISTS = 0x1,
            COPY_FILE_RESTARTABLE = 0x2,
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x8,
        }

        [Flags]
        internal enum MoveFileExFlags
        {
            MOVEFILE_COPY_ALLOWED = 2,
            MOVEFILE_CREATE_HARDLINK = 16,
            MOVEFILE_DELAY_UNTIL_REBOOT = 4,
            MOVEFILE_FAIL_IF_NOT_TRACKABLE =32,
            MOVEFILE_REPLACE_EXISTING=1,
            MOVEFILE_WRITE_THROUGH=8
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool MoveFileWithProgress(
            string lpExistingFileName, string lpNewFileName,
            CopyProgressRoutine lpProgressRoutine,
            IntPtr lpData, 
            int dwFlags);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool CopyFileEx(
            string lpExistingFileName, string lpNewFileName,
            CopyProgressRoutine lpProgressRoutine,
            IntPtr lpData, ref bool pbCancel, int dwCopyFlags);

        
    }

    public partial class Utils
    {

        /// <summary>
        /// Callback to be executed during copy/move operations
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="destination">destination file</param>
        /// <param name="state">state parameter</param>
        /// <param name="totalFileSize">total file size</param>
        /// <param name="totalBytesTransferred">total bytes transferred</param>
        public delegate void CopyFileCallback(string source, string destination, object state, long totalFileSize, long totalBytesTransferred);

        /// <summary>
        /// Copy file from source to destination with progress callback
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="destination">destination file</param>
        /// <param name="overwrite">true if destination must be overwritten if exists</param>
        /// <param name="callback">progress callback method to be called</param>
        public static void CopyFile(string source, string destination, bool overwrite, CopyFileCallback callback)
        {
            CopyOrMoveFile(source, destination, overwrite, false, callback);
        }


        /// <summary>
        /// Move file from source to destination with progress callback
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="destination">destination file</param>
        /// <param name="overwrite">true if destination must be overwritten if exists</param>
        /// <param name="callback">progress callback method to be called</param>
        public static void MoveFile(string source, string destination, bool overwrite, CopyFileCallback callback)
        {
            CopyOrMoveFile(source, destination, overwrite, true, callback);
        }

        /// <summary>
        /// Copy or move file from source to destination with progress callback
        /// </summary>
        /// <param name="source">source file</param>
        /// <param name="destination">destination file</param>
        /// <param name="overwrite">true if destination must be overwritten if exists</param>
        /// <param name="move">true if move, false=copy</param>
        /// <param name="callback">progress callback method to be called</param>
        public static void CopyOrMoveFile(string source, string destination, bool overwrite, bool move, CopyFileCallback callback)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");

            new FileIOPermission(FileIOPermissionAccess.Read, source).Demand();
            new FileIOPermission(FileIOPermissionAccess.Write, destination).Demand();

            CopyProgressData progData = null;
            NativeMethods.CopyProgressRoutine cpr = null;
            if (callback != null)
            {
                progData = new CopyProgressData(source, destination, callback);
                cpr = progData.CallbackHandler;
            }

            bool cancel = false;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    bool ret = true;
                    if (move)
                    {
                        NativeMethods.MoveFileExFlags fm = NativeMethods.MoveFileExFlags.MOVEFILE_COPY_ALLOWED;
                        if (overwrite)
                            fm |= NativeMethods.MoveFileExFlags.MOVEFILE_REPLACE_EXISTING;

                        ret = NativeMethods.MoveFileWithProgress(source, destination, cpr, IntPtr.Zero, (int)fm);
                    }
                    else
                    {
                        NativeMethods.CopyFileExFlags f = NativeMethods.CopyFileExFlags.COPY_FILE_ALLOW_DECRYPTED_DESTINATION;
                        if (!overwrite)
                            f |= NativeMethods.CopyFileExFlags.COPY_FILE_FAIL_IF_EXISTS;

                        ret = NativeMethods.CopyFileEx(source, destination, cpr, IntPtr.Zero, ref cancel, (int)f);
                    }
                    if (progData != null && progData.Exception != null)
                        Utils.Rethrow(progData.Exception);

                    if (!ret)
                    {
                        var w = new Win32Exception();
                        if (w.NativeErrorCode == 2)
                            throw new FileNotFoundException(w.Message, source);
                        throw new IOException(w.Message, w);
                    }
                    return;
                }
                catch (MissingMethodException) { }
                catch (SecurityException) { }
            }
            
            // The old fashioned way
            if (source != destination)
            {
                if (overwrite)
                {
                    var fi = new FileInfo(destination);
                    if (fi.Exists && (fi.Attributes & (FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System))!=0)
                        fi.Attributes&=~(FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                    fi.Delete();
                }
                if (move)
                    File.Move(source, destination);
                else
                    File.Copy(source, destination, overwrite);
                return;
            }
        }

        private class CopyProgressData
        {
            private string _source = null;
            private string _destination = null;
            private CopyFileCallback _callback = null;

            private Exception _exception;

            public CopyProgressData(string source, string destination,
                CopyFileCallback callback)
            {
                _source = source;
                _destination = destination;
                _callback = callback;
            }

            public Exception Exception
            {
                get
                {
                    return _exception;
                }
            }

            public int CallbackHandler(
                long totalFileSize, long totalBytesTransferred,
                long streamSize, long streamBytesTransferred,
                int streamNumber, int callbackReason,
                IntPtr sourceFile, IntPtr destinationFile, IntPtr data)
            {
                try
                {
                    _callback(_source, _destination, null, totalFileSize, totalBytesTransferred);
                    return 0;
                }
                catch (Exception e)
                {
                    _exception = e;
                    return 1; // Cancel
                }

            }
        }
    }
}