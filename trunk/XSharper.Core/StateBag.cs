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
using System.Collections.Generic;

namespace XSharper.Core
{
    /// Associates a set of variables with a particular object reference
    public class StateBag
    {
        private readonly Dictionary<object, Vars> _set;

        /// Constructor
        public StateBag()
        {
            _set=new Dictionary<object, Vars>(new RefComparer());
        }

        /// <summary>
        /// Set object state variable
        /// </summary>
        /// <param name="o">Object</param>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        public void Set(object o, string name, object value)
        {
            Vars s;
            if (!_set.TryGetValue(o, out s))
            {
                _set[o] = s=new Vars();
            }
            s[name] = value;
        }

        /// <summary>
        /// Remove variable from the object state
        /// </summary>
        /// <param name="o">Object</param>
        /// <param name="name">Variable name</param>
        public void Remove(object o, string name)
        {
            Vars s;
            if (!_set.TryGetValue(o, out s))
                return;
            s.Remove(name);
            if (s.Count == 0)
                _set.Remove(o);
        }

        /// <summary>
        /// Get variable from the object state
        /// </summary>
        /// <param name="o">Object</param>
        /// <param name="name">Variable name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Variable value, or defaultValue if variable is not set</returns>
        public object Get(object o, string name, object defaultValue)
        {
            Vars s;
            if (!_set.TryGetValue(o, out s))
                return defaultValue;
            return s.GetOrDefault(name, defaultValue);
        }
        /// <summary>
        /// Clear all state information
        /// </summary>
        public void Clear()
        {
            _set.Clear();
        }

        class RefComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }


    }
}