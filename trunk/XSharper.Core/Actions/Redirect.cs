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

namespace XSharper.Core
{
    /// <summary>
    /// Redirect script output streams into variables or other streams
    /// </summary>
    [XsType("redirect", ScriptActionBase.XSharperNamespace)]
    [Description("Redirect script output streams into variables or other streams")]
    public class Redirect : Block
    {
        /// Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^out should be redirected. 
        [Description("Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^out should be redirected. ")]
        public string OutTo { get; set; }

        /// Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^bold should be redirected. 
        [Description("Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^bold should be redirected. ")]
        public string BoldTo { get; set; }

        /// Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^error should be redirected. 
        [Description("Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^error should be redirected. ")]
        public string ErrorTo { get; set; }

        /// Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^info should be redirected. 
        [Description("Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^info should be redirected. ")]
        public string InfoTo { get; set; }

        /// Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^debug should be redirected. 
        [Description("Variable, Stream (prefixed with ^) or filename (prefixed with ^#) where all output to ^debug should be redirected. ")]
        public string DebugTo { get; set; }

        /// Execute action
        public override object Execute()
        {
            object re = Context.SaveRedirect();

            try
            {
                Context.AddRedirect(OutputType.Out, Context.TransformStr(OutTo, Transform));
                Context.AddRedirect(OutputType.Bold, Context.TransformStr(BoldTo, Transform));
                Context.AddRedirect(OutputType.Error, Context.TransformStr(ErrorTo, Transform));
                Context.AddRedirect(OutputType.Debug, Context.TransformStr(DebugTo, Transform));
                Context.AddRedirect(OutputType.Info, Context.TransformStr(InfoTo, Transform));
                
                return base.Execute();
            }
            finally
            {
                Context.RestoreRedirect(re);
            }
        }

        
    }
}