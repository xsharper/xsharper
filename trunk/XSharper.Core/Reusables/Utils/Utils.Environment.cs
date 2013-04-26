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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


namespace XSharper.Core
{
    public partial class Utils
    {
        private static int s_realConsole = (Environment.OSVersion.Platform==PlatformID.Win32NT)? -1:0;

        public static bool IsWindows
        {
            get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
        }
        /// True if Console is attached and real (Console app) and false if console is fake (Console.Title or Console.BufferWidth calls will throw an exception)
        public static bool HasRealConsole
        {
            get
            {
                if (s_realConsole == -1)
                {
                    try
                    {
                        string s = Console.Title;
                        int n = Console.BufferWidth;
                        s_realConsole = 1;
                    }
                    catch (Exception)
                    {
                        s_realConsole = 0;
                    }
                }
                return s_realConsole == 1;
            }
        }

        /// Get .NET core directory
        public static string GetCORSystemDirectory()
        {
            return System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        }

        /// Get directory of a given .NET framework version
        public static DirectoryInfo FindNETFrameworkDirectory(Version v)
        {
            string core = new DirectoryInfo(GetCORSystemDirectory()).Parent.FullName;

            List<Version> ret = new List<Version>();
            foreach (DirectoryInfo d in new DirectoryInfo(core).GetDirectories("v*.*"))
            {
                string dirname = d.Name;
                if (dirname.Length > 2 && dirname.StartsWith("v", StringComparison.OrdinalIgnoreCase) && char.IsDigit(dirname[1]))
                {
                    Version verFound = new Version(d.Name.Substring(1));
                    if (v == verFound)
                        return d;
                }
            }
            return null;
        }

        /// Return list of installed .NET versions
        public static Version[] GetInstalledNETVersions()
        {
            string core = new DirectoryInfo(GetCORSystemDirectory()).Parent.FullName;
            List<Version> ret = new List<Version>();
            foreach (DirectoryInfo d in new DirectoryInfo(core).GetDirectories("v*.*"))
            {
                string dirname = d.Name;
                if (dirname.Length > 2 && dirname.StartsWith("v", StringComparison.InvariantCultureIgnoreCase) && char.IsDigit(dirname[1]))
                {
                    Version v = new Version(d.Name.Substring(1));
                    ret.Add(v);
                }
            }
            ret.Sort((x, y) => ((x.Major * 10000 + x.Minor) - (y.Major * 10000 + y.Minor)));
            return ret.ToArray();
        }
    }
}