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
using Z = ICSharpCode.SharpZipLib.GZip;

namespace Gzip
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                byte[] buf = new byte[1024];
                int n;
                using (Stream input = Console.OpenStandardInput())
                using (Stream output = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(Console.OpenStandardOutput()))
                    while ((n = input.Read(buf, 0, buf.Length)) != 0)
                        output.Write(buf, 0, n);
                return 0;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("Error: "+e.Message);
                return 1;
            }
        }
    }
}
