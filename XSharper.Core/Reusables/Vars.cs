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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// Set of variables, which can be accessed by name or by index
    /// </summary>
    public partial class Vars : MarshalByRefObject, ICollection<Var>, IDictionary<string,object>
    {
        #region -- private classes --
        class VarCollection : KeyedCollection<string, Var>
        {
            public VarCollection()
                : base(StringComparer.OrdinalIgnoreCase)
            {

            }

            protected override string GetKeyForItem(Var item)
            {
                return item.Name;
            }
        }
        class RemotableEnumerator : MarshalByRefObject, IEnumerator<Var>
        {
            public Var Current
            {
                get
                {
                    return _sv._variables[_curindex];
                }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            object IEnumerator.Current
            {
                get
                {
                    return _sv._variables[_curindex];
                }
            }

            private int _curindex = -1;
            private Vars _sv;

            public RemotableEnumerator(Vars sv)
            {
                _sv = sv;
            }
            public bool MoveNext()
            {
                if (_curindex >= _sv.Count - 1)
                    return false;
                _curindex++;
                return true;
            }

            public void Reset()
            {
                _curindex = -1;
            }
        }
        private readonly VarCollection _variables = new VarCollection();
        #endregion
        

        /// <summary>
        /// Constructor
        /// </summary>
        public Vars()
        {
        }

        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public Vars(Vars setOfVariables) : this(setOfVariables as IEnumerable<Var>)
        {
            
        }
        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public Vars(IEnumerable<Var> setOfVariables)
        {
            if (setOfVariables != null)
            {
                foreach (Var i in setOfVariables)
                    _variables.Add(new Var(i.Name, i.Value));
            }
        }
        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public Vars(IEnumerable<KeyValuePair<string, object>> setOfVariables)
        {
            if (setOfVariables != null)
            {
                foreach (var i in setOfVariables)
                    _variables.Add(new Var(i.Key, i.Value));
            }
        }
        
        #region IEnumerable<Variable> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            foreach (var v in _variables)
                yield return new KeyValuePair<string, object>(v.Name,v.Value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Var> GetEnumerator()
        {
            return new RemotableEnumerator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// Returns null to ensure that remote object does not timeout in remoting scenarios
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }
        
        /// Return variable or default value, if the variable is not set
        public object GetOrDefault(string key, object def)
        {
            object ret;
            if (!TryGetValue(key, out ret))
                return def;
            return ret;
        }


        /// Returns true if variable is set
        public virtual bool IsSet(string key)
        {
            if (_variables.Contains(key))
                return true;
            return false;
        }

        /// <summary>
        /// Try to get variable value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="value">Where to store found variable value</param>
        /// <returns>true if variable is set</returns>
        public virtual bool TryGetValue(string key, out object value)
        {
            if (_variables.Contains(key))
            {
                value = _variables[key].Value;
                return true;
            }
            value = null;
            return false;
        }

        /// Gets or sets the variable
        public object this[string key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Set(key, value);
            }
        }


        /// Get all variable names
        public ICollection<string> Keys
        {
            get
            {
                List<string> s=new List<string>();
                foreach (var variable in _variables)
                    s.Add(variable.Name);
                return s;
            }
        }

        /// Get all variable values
        public ICollection<object> Values
        {
            get
            {
                List<object> s = new List<object>();
                foreach (var variable in _variables)
                    s.Add(variable.Value);
                return s;
            }
        }

        /// Return variable value or throw KeyNotFoundException if the variable is not set
        public object Get(string variableName)
        {
            object ret;
            if (TryGetValue(variableName, out ret))
                return ret;
            
            throw new KeyNotFoundException(String.Format("Variable '{0}' is not set", variableName));
        }

      

        /// Remove variable 
        public bool Remove(Var item)
        {
            return _variables.Remove(item.Name);
        }

        /// Remove variable 
        public bool Remove(KeyValuePair<string, object> item)
        {
            return _variables.Remove(item.Key);
        }

        /// Number of contained variables
        public int Count
        {
            get { return _variables.Count; }
        }


        /// Gets a value indicating whether the set is read-only.
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// Add a variable
        public void Add(Var item)
        {
            _variables.Remove(item.Name);
            _variables.Add(new Var(item.Name, item.Value));
        }

        /// Add a variable
        public void Add(KeyValuePair<string, object> item)
        {
            Add(new Var(item.Key,item.Value));
        }

        /// Removes all variables
        public virtual void Clear()
        {
            _variables.Clear();
        }

        /// Determines whether the set contains a specific variable
        public bool Contains(KeyValuePair<string, object> item)
        {
            return Contains(new Var(item.Key, item.Value));
        }

        /// Copy to array
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            foreach (var v in _variables)
                array[arrayIndex++]=new KeyValuePair<string, object>(v.Name,v.Value);
        }

        /// Determines whether the set contains a specific variable
        public bool Contains(Var item)
        {
            return _variables.Contains(item);
        }

        /// Copy to array
        public void CopyTo(Var[] array, int arrayIndex)
        {
            _variables.CopyTo(array, arrayIndex);
        }

        /// Set variable
        public void Set(string key, object value)
        {
            Set(key, value, false);
        }

        /// Add multiple variables
        public void Merge(IDictionary<string,object> dictionary)
        {
            if (dictionary != null)
                foreach (var pair in dictionary)
                    Set(pair.Key, pair.Value);
        }

        /// Set variable
        public virtual void Set(string key, object value, bool append)
        {
            key = key ?? string.Empty;
            if (append && _variables.Contains(key))
            {
                _variables[key].Append(value);
            }
            else
            {
                _variables.Remove(key);
                _variables.Add(new Var(key, value));    
            }
        }

        /// Determines whether the set contains a specific variable
        public bool ContainsKey(string key)
        {
            return IsSet(key);
        }

        /// Add variable
        public void Add(string key, object value)
        {
            Add(new Var(key,value));
        }

        /// Add variable
        public void AddRange(IEnumerable<Var> range)
        {
            if (range!=null)
                foreach (var variable in range)
                    _variables.Add(variable);
        }

        /// Remove variable
        public virtual bool Remove(string key)
        {
            return _variables.Remove(key);
        }

        /// Dump all variables to a string for debug purposes
        public string ToDumpAll()
        {
            return ToDumpAll((IStringFilter)null);
        }

        /// Dump all variables to a string for debug purposes
        public string ToDumpAll(string filter)
        {
            return ToDumpAll(new StringFilter(filter));
        }

        /// Dump variables matching the filter to a string for debug purposes
        public string ToDumpAll(IStringFilter filter)
        {
            var np = new List<Var>(this);
            np.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            var sb = new StringBuilder();
            int maxlen = 0;
            foreach (var pair in np)
                maxlen = Math.Max(pair.Name.Length, maxlen);
            foreach (var pair in np)
            {
                if (filter!=null && !filter.IsMatch(pair.Name))
                    continue;
                Dump od = new Dump(pair.Value, ((pair.Value == null) ? typeof (object) : pair.Value.GetType()), pair.Name.PadRight(maxlen + 1, ' '), 7);
                od.MaxItems=10;
                od.MaxDepth = 7;
                sb.AppendLine(od.ToString().Trim());
            }
            return sb.ToString().TrimEnd();
        }
    }
}