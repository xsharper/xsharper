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

namespace XSharper.Core.Operations
{
    ///<summary>Operators</summary>
    public enum OperatorType
    {
#pragma warning disable 1591
        Unknown,
        Plus,
        Minus,
        Divide,
        Multiply,
        UnaryMinus,
        UnaryPlus,
        Throw,
        Modulo,
        BinaryNot,
        BinaryOr,
        BinaryAnd,
        BinaryXor,
        Not,
        Or,
        And,
        Equal,
        NotEqual,
        EqualIgnoreCase,
        NotEqualIgnoreCase,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        Dump,
#pragma warning restore 1591
    }

    ///<summary>Calculate an operator on stack</summary>
    [Serializable]
    public class OperationOperator : IOperation
    {
        readonly OperatorType _operator;

        /// Constructor
        public OperationOperator(OperatorType op)
        {
            _operator = op;
        }
        
        /// Return a string representation of the expression
        public override string ToString()
        {
            return "operator(" + _operator + ")";
        }

        /// Returns number of entries added to stack by the operation. 
        public int StackBalance
        {
            get { return 1+(IsUnary(_operator)?-1:-2); }
        }

        
        ///<summary>Determine if operator is binary or unary</summary>
        ///<param name="op">Operator to check</param>
        ///<returns>true if operator is unary, and false if operator is binary</returns>
        public static bool IsUnary(OperatorType op)
        {
            switch (op)
            {
                case OperatorType.UnaryMinus:
                case OperatorType.UnaryPlus:
                case OperatorType.Throw:
                case OperatorType.Dump:
                case OperatorType.BinaryNot:
                case OperatorType.Not:
                    return true;
                default:
                    return false;
            }
        }

        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            var o2 = stack.Pop();
            var o1 = o2;
            if (!IsUnary(_operator))
                o1 = stack.Pop();
            switch (_operator)
            { 
                default:
                    if (_operator == OperatorType.NotEqualIgnoreCase || _operator == OperatorType.EqualIgnoreCase)
                    {
                        if (o1 != null && !(o1 is string)) o1 = o1.ToString();
                        if (o2 != null && !(o2 is string)) o2 = o2.ToString();
                    }
                    
                    var o1isStr=(o1 is string || o1 is char || o1 is char?);
                    var o2isStr=(o2 is string || o2 is char || o2 is char?);
                    if (_operator == OperatorType.Plus)
                    {
                        if (o1 == null)
                        {
                            stack.Push(o2);
                            return;
                        }
                        if (o2 == null)
                        {
                            stack.Push(o1);
                            return;
                        }
                    }
                    if (o1 != null && (!(o1.GetType().IsPrimitive || o1 is decimal) || (o1isStr && o2isStr)))
                    {
                        if (_operator == OperatorType.Plus && o1isStr)
                        {
                            stack.Push(string.Concat(o1 , o2));
                            return;
                        }
                        if (o1isStr && o2isStr)
                        {
                            stack.Push(stringMath(o1.ToString(), (o2 ?? string.Empty).ToString()));
                            return;
                        }
    

                        if (_operator==OperatorType.Equal)
                        {
                            stack.Push(o1.Equals(o2));
                            return;
                        }
                        if (_operator == OperatorType.NotEqual)
                        {
                            stack.Push(!o1.Equals(o2));
                            return;
                        }
                        
                    }
                    if (o1 == null || o2 == null) 
                    {
                        if (_operator == OperatorType.Equal)
                        {
                            stack.Push(o1 == o2);
                            return;
                        }
                        if (_operator == OperatorType.NotEqual)
                        {
                            stack.Push(o1 != o2);
                            return;
                        }
                        if (_operator == OperatorType.Not)
                        {
                            stack.Push(true);
                            return;
                        }
                        throw new NullReferenceException();
                    }

                    
                    if (o1 is decimal || o2 is decimal)
                        stack.Push(decimalMath(Utils.To<decimal>(o1), Utils.To<decimal>(o2)));
                    else  if (o1 is double || o2 is double || o1 is float || o2 is float)
                        stack.Push(doubleMath(Utils.To<double>(o1), Utils.To<double>(o2)));
                    else
                    {
                        var v = longMath(Utils.To<long>(o1), Utils.To<long>(o2));
                        if (o1 is int && o2 is int)
                            v = Utils.To<int>(v);
                        stack.Push(v);
                    }
                    return;

                case OperatorType.Dump:
                    stack.Push(Dump.ToDump(o1));
                    break;
                case OperatorType.Throw:
                    if (o1 is Exception)
                        throw (Exception)o1;
                    throw new ApplicationException((o1??string.Empty).ToString());
                
            }
        }

        private object stringMath(string d1, string d2)
        {
            int r ;
            if (_operator==OperatorType.EqualIgnoreCase || _operator==OperatorType.NotEqualIgnoreCase)
                r = string.Compare(d1,d2,StringComparison.InvariantCultureIgnoreCase);
            else
                r=string.Compare(d1, d2, StringComparison.Ordinal);

            switch (_operator)
            {
                case OperatorType.EqualIgnoreCase: return r == 0;
                case OperatorType.NotEqualIgnoreCase: return r != 0;
                case OperatorType.Equal: return r==0;
                case OperatorType.NotEqual: return r != 0;
                case OperatorType.Less: return r < 0;
                case OperatorType.LessOrEqual: return r <= 0;
                case OperatorType.Greater: return r > 0;
                case OperatorType.GreaterOrEqual: return r >= 0;
                default:
                    if (_operator == OperatorType.Not)
                        return string.IsNullOrEmpty(d1) || !Utils.To<bool>(d1);

                    var v1=ParsingReader.TryParseNumber(d1);
                    var v2 = ParsingReader.TryParseNumber(d2);
                    if (v1!=null && v2!=null)
                    {
                        if (v1 is double || v2 is double || v1 is float || v2 is float || v1 is decimal || v2 is decimal)
                            return doubleMath(Utils.To<double>(v1), Utils.To<double>(v2));
                        var x= longMath(Utils.To<long>(v1), Utils.To<long>(v2));
                        if (x is long && (long)x >= int.MinValue && (long)x <= int.MaxValue)
                            return Utils.To<int>(x);
                        return x;
                    }
                    
                    throw new InvalidOperationException("Cannot process operator " + _operator);
            }
        }

        private object doubleMath(double d1, double d2)
        {
            switch (_operator)
            {
                case OperatorType.UnaryMinus: return -d1;
                case OperatorType.UnaryPlus: return d1; 
                case OperatorType.Plus: return d1 + d2; 
                case OperatorType.Minus: return d1 - d2; 
                case OperatorType.Multiply: return d1 * d2;
                case OperatorType.Divide: return d1 / d2; 
                case OperatorType.Modulo: return d1 % d2;
                case OperatorType.Equal: return (d1 == d2);
                case OperatorType.NotEqual: return !(d1 == d2);
                case OperatorType.Less: return (d1 < d2);
                case OperatorType.LessOrEqual: return (d1 <= d2);
                case OperatorType.Greater: return (d1 > d2);
                case OperatorType.GreaterOrEqual: return (d1 >= d2);
                default:
                    return longMath((long) d1, (long) d2);
            }
            
        }
        private object decimalMath(decimal d1, decimal d2)
        {
            switch (_operator)
            {
                case OperatorType.UnaryMinus: return -d1;
                case OperatorType.UnaryPlus: return d1;
                case OperatorType.Plus: return d1 + d2;
                case OperatorType.Minus: return d1 - d2;
                case OperatorType.Multiply: return d1 * d2;
                case OperatorType.Divide: return d1 / d2;
                case OperatorType.Modulo: return d1 % d2;
                case OperatorType.Equal: return (d1 == d2);
                case OperatorType.NotEqual: return !(d1 == d2);
                case OperatorType.Less: return (d1 < d2);
                case OperatorType.LessOrEqual: return (d1 <= d2);
                case OperatorType.Greater: return (d1 > d2);
                case OperatorType.GreaterOrEqual: return (d1 >= d2);
                default:
                    return longMath((long)d1, (long)d2);
            }

        }
        private object longMath(long d1, long d2)
        {
            switch (_operator)
            {
                case OperatorType.UnaryMinus: return -d1;
                case OperatorType.UnaryPlus: return d1; 
                case OperatorType.Plus: return d1 + d2;
                case OperatorType.Minus: return d1 - d2;
                case OperatorType.Multiply: return d1 * d2;
                case OperatorType.Divide: return d1 / d2;
                case OperatorType.Modulo: return d1 % d2;
                case OperatorType.BinaryNot: return ~d1;
                case OperatorType.BinaryOr: return d1|d2;
                case OperatorType.BinaryXor: return d1 ^ d2;
                case OperatorType.BinaryAnd: return d1&d2;
                case OperatorType.Not:return (d1==0);
                case OperatorType.Or:return (d1!=0 || d2!=0);
                case OperatorType.And:return (d1!=0 && d2!=0);
                case OperatorType.Equal:return (d1==d2);
                case OperatorType.NotEqual:return !(d1==d2);
                case OperatorType.Less:return (d1<d2);
                case OperatorType.LessOrEqual:return (d1<=d2);
                case OperatorType.Greater:return (d1>d2);
                case OperatorType.GreaterOrEqual:return (d1>=d2);
        
            }
            throw new InvalidOperationException("Cannot process operator " + _operator);
        }
    }
}