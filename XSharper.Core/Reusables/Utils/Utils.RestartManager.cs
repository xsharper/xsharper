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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


namespace XSharper.Core
{
    static partial class NativeMethods
    {
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        internal static extern int RmStartSession(
            out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        internal static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        internal static extern int RmRegisterResources(uint pSessionHandle,
            UInt32 nFiles, string[] rgsFilenames,
            UInt32 nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications,
            UInt32 nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        internal static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded, ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        internal const int RmRebootReasonNone = 0;
        internal const int CCH_RM_MAX_APP_NAME = 255;
        internal const int CCH_RM_MAX_SVC_NAME = 63;

        [StructLayout(LayoutKind.Sequential)]
        internal struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RmApplicationType ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }
    }

    public enum RmApplicationType
    {
        UnknownApp = 0,
        MainWindow = 1,
        OtherWindow = 2,
        Service = 3,
        Explorer = 4,
        Console = 5,
        Critical = 1000
    }

    public class RmProcessInfo
    {
        public int ProcessId;
        public DateTime StartTime;
        public string AppName;
        public string ServiceShortName;
        public RmApplicationType ApplicationType;
        public uint AppStatus;
        public uint TsSessionId;
        public bool IsRestartable;

        public RmProcessInfo()
        {
            
        }
        internal RmProcessInfo(ref NativeMethods.RM_PROCESS_INFO pi)
        {
            ProcessId = pi.Process.dwProcessId;
            StartTime=DateTime.FromFileTimeUtc((long)pi.Process.ProcessStartTime.dwHighDateTime<<32 | (uint)pi.Process.ProcessStartTime.dwHighDateTime);
            ApplicationType = pi.ApplicationType;
            AppName = pi.strAppName;
            IsRestartable = pi.bRestartable;
            ServiceShortName = pi.strServiceShortName;
            TsSessionId = pi.TSSessionId;
            AppStatus = pi.AppStatus;
        }
    }
    public partial class Utils
    {
        /// Get the list of process using files
        /// Courtesy of https://learn.microsoft.com/en-us/archive/msdn-magazine/2007/april/net-matters-restart-manager-and-generic-method-compilation
        public static List<RmProcessInfo> GetProcessesUsingFiles(string[] filePaths)
        {
            uint sessionHandle;
            List<RmProcessInfo> processes = new List<RmProcessInfo>();

            // Create a restart manager session
            int rv = NativeMethods.RmStartSession(out sessionHandle, 0, Guid.NewGuid().ToString("N"));
            if (rv != 0) throw new Win32Exception(rv);
            try
            {
                // Let the restart manager know what files we’re interested in
                rv = NativeMethods.RmRegisterResources(sessionHandle, (uint)filePaths.Length, filePaths, 0, null, 0, null);
                if (rv != 0) throw new Win32Exception(rv);

                // Ask the restart manager what other applications 
                // are using those files
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0, pnProcInfo = 0, lpdwRebootReasons = NativeMethods.RmRebootReasonNone;
                NativeMethods.RM_PROCESS_INFO[] processInfo = null;
                for (;;)
                {
                    rv = NativeMethods.RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (rv == ERROR_MORE_DATA)
                        processInfo = new NativeMethods.RM_PROCESS_INFO[pnProcInfo=pnProcInfoNeeded];
                    else if (rv == 0)
                    {
                        for (int i=0;i<pnProcInfo;++i)
                            processes.Add(new RmProcessInfo(ref processInfo[i]));
                        break;
                    }
                    else throw new Win32Exception(rv);
                }
                
            }
            // Close the resource manager
            finally { NativeMethods.RmEndSession(sessionHandle); }

            return processes;
        }
    }
}