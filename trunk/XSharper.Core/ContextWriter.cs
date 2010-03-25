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
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// TextWriter subclass, that forwards all output to ScriptContext
    /// </summary>
    public class ContextWriter : TextWriter
    {
        private readonly ScriptContext _context;
        private readonly OutputType _outputType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Script context, where to forward all output</param>
        /// <param name="outputType">Classify all output as this output type</param>
        public ContextWriter(ScriptContext context, OutputType outputType)
        {
            _context = context;
            _outputType = outputType;
        }

        /// <summary>
        /// returns the <see cref="T:System.Text.Encoding"/> in which the output is written.
        /// </summary>
        /// <returns>
        /// The Encoding in which the output is written.
        /// </returns>
        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        /// 
        public override void Write(object value)
        {
            _context.Write(_outputType, (value??string.Empty).ToString());
            
        }

        /// 
        public override void WriteLine(object obj)
        {
            _context.WriteLine(_outputType, (obj??string.Empty).ToString());

        }

        /// 
        public override void Write(string value)
        {
            _context.Write(_outputType, (value??string.Empty));
        }

        /// 
        public override void WriteLine(string value)
        {
            _context.WriteLine(_outputType, value);
        }

        /// 
        public override void Write(char c)
        {
            _context.Write(_outputType, c.ToString());
        }

        /// Convert object to string, expand all variables and output
        public void Print(object obj)
        {
            _context.WriteLine(_outputType, _context.Expand(obj));
        }

        /// Convert object to string, expand all variables and output
        public void Print(object obj, TransformRules rules)
        {
            _context.WriteLine(_outputType, _context.Transform(obj,rules));
        }

        /// 
        public void Dump(object objectToDump)
        {
            Dump(objectToDump, string.Empty);
        }

        /// 
        public void Dump(object objectToDump, string name)
        {
            Dump(objectToDump, (objectToDump??new object()).GetType(), name);
        }

        /// 
        public void Dump(object objectToDump, Type type)
        {
            Dump(objectToDump, type,string.Empty);
        }

        /// 
        public void Dump(object objectToDump, Type type, string name)
        {
            WriteLine(Core.Dump.ToDump(objectToDump, type, name));
        }

    }
}   