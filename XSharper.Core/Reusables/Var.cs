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
using System.Text;

namespace XSharper.Core
{
    
    /// <summary>
    /// Variable, as a name-value pair
    /// </summary>
    [Serializable]
    public class Var
    {
        private object _value;
        private string _name;
        private StringBuilder _stringBuilder;

        /// Constructor 
        public Var()
        {
        }

        /// Constructor 
        public Var(string name, object value)
        {
            _name = name??string.Empty;
            _value = value;
        }

        /// <summary>
        /// Variable name
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Variable value
        /// </summary>
        public object Value
        {
            get
            {
                if (_stringBuilder != null)
                    return _stringBuilder.ToString();
                return _value;
            }
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object.
        public override string ToString()
        {
            return string.Format("var {0}='{1}'", Name, Value);
        }

        /// <summary>
        /// Convert current value to string, and append the provided value to it
        /// </summary>
        /// <param name="value">value to append</param>
        public void Append(object value)
        {
            if (_stringBuilder == null)
                _stringBuilder=new StringBuilder(Utils.To<string>(_value));

            _stringBuilder.Append(Utils.To<string>(value));
        }
    }
}