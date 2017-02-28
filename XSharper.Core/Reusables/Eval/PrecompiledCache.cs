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
    public interface IPrecompiledCache
    {
        int Capacity { get; }
        void Clear();
        IOperation this[string expression] { get; set; }
    }

    /// Cache for precompiled expressions
    public class PrecompiledCache : IPrecompiledCache
    {
        private readonly string[] _expressions;
        private readonly Dictionary<string, IOperation> _map;
        private int _nextptr = 0;
        private object _lock = null;
        
        /// Create cache with the specified number of entries
        public PrecompiledCache(int cacheSize) : this(cacheSize, false)
        {
        }

        /// Create cache with the specified number of entries
        public PrecompiledCache(int cacheSize, bool useLocking)
        {
            _expressions = new string[cacheSize];
            _map = new Dictionary<string, IOperation>(cacheSize);
            _lock = useLocking?new object():null;
        }

        public int Capacity
        {
            get { return _expressions.Length; }
        }

        /// Clear cache
        public void Clear()
        {
            _expressions.Initialize();
            if (_lock!=null)
            {
                lock (_lock)
                    _map.Clear();
            }
            else
                _map.Clear();
        }

        /// Access cache
        public IOperation this[string expression]
        {
            // if (_lock != null) logic is a bit over the top, but there isn't much of it
            get
            {
                IOperation v;
                if (_lock != null)
                {
                    lock (_lock)
                        return _map.TryGetValue(expression, out v) ? v : null;
                }
                else
                    return _map.TryGetValue(expression, out v) ? v : null;
            }
            set
            {
                if (_lock!=null)
                {
                    lock (_lock)
                    {
                        if (_expressions[_nextptr] != null)
                            _map.Remove(_expressions[_nextptr]);
                        _expressions[_nextptr++] = expression;
                        _map[expression] = value;
                    }
                }
                else
                {
                    if (_expressions[_nextptr] != null)
                        _map.Remove(_expressions[_nextptr]);
                    _expressions[_nextptr++] = expression;
                    _map[expression] = value;
                }
            }
        }

    }
}