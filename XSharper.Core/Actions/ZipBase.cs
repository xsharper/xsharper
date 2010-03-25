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
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace XSharper.Core
{
    /// <summary>
    /// Base class for ZIP and UNZIP actions
    /// </summary>
    public abstract class ZipBase : ActionWithFilters
    {
        /// How time is stored inside ZIP archive. Default 'FileTime' => ZIP contains timestamps in local timezone
        public ZipTime ZipTime { get; set; }

        /// ZIP password
        public string Password { get; set; }

        /// How often to call progress method
        protected TimeSpan ProgressInterval { get { return TimeSpan.FromMilliseconds(500); } }

        /// Constructor
        protected ZipBase()
        {
            ZipTime = ZipTime.FileTime;
        }
        /// Initialize action
        public override void Initialize()
        {
            ZipConstants.DefaultCodePage = Console.InputEncoding.WindowsCodePage;
            base.Initialize();
        }
    }
}



