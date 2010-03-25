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
    /// Output text to output stream
    /// </summary>
    [XsType("print", ScriptActionBase.XSharperNamespace)]
    [Description("Output text to output stream")]
    public class Print : ValueBase
    {
        /// Append new line to the output. Default is true.
        [Description("Append new line to the output. ")]
        [XsAttribute("newLine")]
        [XsAttribute("nl")]
        public bool NewLine { get; set; }

        /// Indent the text by this number of space characters. Default is 0.
        [Description("Indent the text by this number of space characters. ")]
        public int Indent { get; set; }

        /// A semicolon or | separated list of outputs. Default '^out'.
        [Description("A semicolon or | separated list of outputs.")]
        public string OutTo { get; set; }
        
        /// Default constructor
        public Print()
        {
            NewLine = true;
            OutTo = "^out";
        }

        /// Constructor with text
        public Print(string text) : this()
        {
            Value = text;
        }

        /// Execute action
        public override object Execute()
        {
            string text = GetTransformedValueStr();
            if (Indent!=0)
                text = Utils.PrefixEachLine(new string(' ', Indent), text);
            if (NewLine)
                Context.OutTo(Context.TransformStr(OutTo, Transform), text+Environment.NewLine);   
            else
                Context.OutTo(Context.TransformStr(OutTo, Transform), text);   
            return null;
        }
    }
}