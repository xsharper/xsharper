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
    /// Read text file
    [XsType("readtext", ScriptActionBase.XSharperNamespace)]
    [Description("Read text file")]
    public class ReadText : ScriptActionBase
    {
        /// Filename or URI to read
        [XsRequired]
        public string From { get; set; }

        /// Encoding, if known
        public string Encoding { get; set; }

        /// Where to put the read file
        public string OutTo { get; set; }

        /// Default value to use, if file cannot be read
        public string Default { get; set; }

        /// Execute action
        public override object Execute()
        {
            string fromEx = Context.TransformStr(From, Transform);
            string ret=null;
            try
            {
                string encoding = Context.TransformStr(Encoding, Transform);
                ret = Context.ReadText(fromEx, Utils.GetEncoding(encoding));
            }
            catch(Exception e)
            {
                if (Default!=null)
                    ret = Context.TransformStr(Default, Transform);
                else
                    Utils.Rethrow(e);
            }

            Context.OutTo(Context.TransformStr(OutTo, Transform), ret);
            return null;
        }
    }
}