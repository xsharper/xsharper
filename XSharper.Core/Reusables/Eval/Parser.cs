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
using System.IO;
using System.Reflection;
using System.Text;

namespace XSharper.Core
{
    
    /// C#-like expression tree builder
    public class Parser
    {
        [AttributeUsage(AttributeTargets.Field)]
        class OpAttribute : Attribute
        {
            public OpAttribute(int prec) { Precedence = prec; }
            public OpAttribute(int prec, string name) { Precedence = prec; Name = name; }
            public string Name { get; set; }
            public int Precedence { get; set; }
        }

        // These are internal operators
        enum Operators
        {
            [Op(0)]     None,
            [Op(0)]     Throw,
            [Op(0)]     Conditional,
            [Op(0)]     Coalesce,
            [Op(1)]     Dump,
            [Op(1)]     Comma,
            [Op(2)]     Is,
            [Op(2)]     As,
            [Op(3, "OR")]   Or,
            [Op(4, "AND")]  And,
            [Op(5, "BOR")]  BinaryOr,
            [Op(6, "BXOR")]  BinaryXor,
            [Op(7, "BAND")] BinaryAnd,
            [Op(8, "EQ")]   Equal,
            [Op(8, "NEQ")]  NotEqual,
            [Op(8, "EQI")]   EqualIgnoreCase,
            [Op(8, "NEQI")]  NotEqualIgnoreCase,
            [Op(9, "LT")]   Less,
            [Op(9, "LE")]   LessOrEqual,
            [Op(9, "GT")]   Greater,
            [Op(9, "GE")]   GreaterOrEqual,

            [Op(12)]    Plus,
            [Op(12)]    Minus,

            [Op(13)]    Divide,
            [Op(13, "MOD")] Modulo,
            [Op(13)]    Multiply,

            [Op(15)]    TypeCast,

            [Op(20)]    UnaryMinus,
            [Op(20)]    UnaryPlus,
            [Op(20, "NOT")] Not,
            [Op(20, "NEG")] BinaryNot,
            [Op(101)]   New,
        }

        // Token type
        enum QType
        {
            Id,
            Value,
            Subexpr,
            Operator,
            ParenthesisOpen,
            ParenthesisClose,
            SquareOpen,
            SquareClose,
            BlockOpen,
            BlockClose,
            Colon,
            Typeof
        }

        // Token. This is done as a simple class instead of the whole class hierarchy
        class QToken
        {
            public QType TokenType;         // Token type
            public Operators Operator;      // Operator (if TokenType==Operator)
            public object Param;            // Any token argument (type name for Is/As/New, value , subexpression etc)
            public int ExtraInt;            // Additional integer parameter

            public QToken(Operators op) : this(QType.Operator) { Operator = op; }
            public QToken(QType t)  {   TokenType = t;}
            public QToken(QType t, object p) { TokenType = t; Param = p; }

            public bool IsOpenBrace { get { return TokenType == QType.SquareOpen || TokenType == QType.BlockOpen || TokenType == QType.ParenthesisOpen; } }
            public bool IsCloseBrace { get { return TokenType == QType.SquareClose || TokenType == QType.BlockClose || TokenType == QType.ParenthesisClose; } }

            public override string ToString()
            {
                using (var sw = new StringWriter())
                {
                    sw.Write(TokenType + ": ");
                    sw.Write(Operator==Operators.None ? string.Empty:"op="+Operator+" ");
                    if (ExtraInt != 0)
                        sw.Write("(extraInt=" + ExtraInt + ")");
                    if (Param != null)
                        sw.Write("(Param=" + Param + ")");
                    return sw.ToString();
                }
            }
            internal bool IsOperator(Operators op)  {   return (TokenType == QType.Operator) && Operator == op;  }
        }


        static Parser()
        {
            s_precedence = new Dictionary<Operators, int>();
            s_names = new Dictionary<string, Operators>();
            s_operatorTrans = new Dictionary<Operators, Operations.OperatorType>();
            foreach (FieldInfo e in typeof(Operators).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField))
            {
                var o = (OpAttribute)Attribute.GetCustomAttribute(e, typeof(OpAttribute), true);
                Operators op = (Operators)e.GetValue(null);
                s_precedence[op] = o.Precedence;
                if (o.Name != null)
                    s_names[o.Name] = op;

                var f = typeof(Operations.OperatorType).GetField(op.ToString(), BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField);
                if (f != null)
                    s_operatorTrans[op] = (Operations.OperatorType)f.GetValue(null);
            }
        }


        private static readonly Dictionary<Operators, int> s_precedence;
        private static readonly Dictionary<string, Operators> s_names;
        private static readonly Dictionary<Operators, Operations.OperatorType> s_operatorTrans;
        private readonly string _tokenSpecialCharacters = "@_";

        /// Constructor
        public Parser()
        {
        }

        /// Constructor with a specific list of token characters ( by default token may consist of letters, digits and @ and _ symbols)
        public Parser(string tokenSpecialChars)
        {
            _tokenSpecialCharacters = tokenSpecialChars;

        }



        /// <summary>
        /// Parse stream and return the expression tree
        /// </summary>
        /// <param name="r">Stream</param>
        /// <returns>Parsed expression or null if no expression is found on stream</returns>
        public IOperation Parse(ParsingReader r)
        {
            List<IOperation> data = new List<IOperation>();
            while (!r.IsEOF)
            {
                TokenQueue tokenQueue = new TokenQueue(this, r);
                var ex = parseSingleStatement(tokenQueue,-1);
                if (ex == null)
                    break;
                if (data.Count > 0)
                    data.Add(new Operations.OperationPop());
                data.Add(ex);
                r.SkipWhiteSpaceAndComments();
                if (r.Peek() != ';')
                    break;
                do
                {
                    r.Read();
                    r.SkipWhiteSpaceAndComments();
                } while (r.Peek() == ';');
            }
            if (data.Count == 0)
                return null;
            if (data.Count == 1)
                return data[0];
            return new Operations.OperationExpression(data.ToArray());
        }
        
        private static IOperation parseSingleStatement(TokenQueue tokenQueue, int precedenceAtLeast)
        {
            Queue<IOperation> queue = new Queue<IOperation>();
            Stack<QToken> stack = new Stack<QToken>();
            QToken token = null;
            bool argumentExpected = false;
            bool dotIsProperty = false;
            bool exitLoop = false;
            while (!exitLoop)
            {
                token = tokenQueue.Pop();
                if (token == null)
                    break;

                switch (token.TokenType)
                {
                    case QType.ParenthesisOpen:
                        // C# has typecasts, such as ((IInterface)x) so when seeing an opening (, we must try to read type. 
                        // This however does not work if there is an identifier before the (, for example func(Enum.Value,(Enum.Value)).
                        if (stack.Count == 0 || (stack.Peek().TokenType != QType.Id && !stack.Peek().IsOperator(Operators.New)))
                        {
                            string sType = tokenQueue.GetTypeNameFollowedByClosingParenthesis();
                            if (sType != null)
                            {
                                var q = tokenQueue.Peek();
                                if (q==null || (q.TokenType!=QType.Id && q.TokenType!=QType.Value && q.TokenType!=QType.Subexpr && !q.IsOpenBrace))
                                {
                                    token = new QToken(QType.Id, sType);
                                    goto case QType.Id;
                                }
                                stack.Push(new QToken(Operators.TypeCast) {Param = sType});
                                argumentExpected = dotIsProperty = false;
                                break;
                            }
                        }
                        goto case QType.SquareOpen;
                    case QType.SquareOpen:
                    case QType.BlockOpen:
                        token.ExtraInt = -(queue.Count + stack.Count + 1);
                        stack.Push(token);
                        argumentExpected = false;
                        dotIsProperty = false;
                        break;

                    case QType.Value:
                        queue.Enqueue(operationFromToken(token));
                        argumentExpected = false;
                        dotIsProperty = true;
                        break;
                    case QType.Subexpr:
                        queue.Enqueue(operationFromToken(token));
                        argumentExpected = false;
                        dotIsProperty = true;
                        break;
                    case QType.Typeof:
                        if (tokenQueue.Peek() == null || tokenQueue.Peek().TokenType != QType.ParenthesisOpen)
                            tokenQueue.ThrowParsingException("Expected (");
                        tokenQueue.Pop();
                        token.Param = tokenQueue.ReadTypeName();
                        if (tokenQueue.Peek() == null || tokenQueue.Peek().TokenType != QType.ParenthesisClose)
                            tokenQueue.ThrowParsingException("Expected )");
                        tokenQueue.Pop();
                        queue.Enqueue(operationFromToken(token));
                        argumentExpected = false;
                        dotIsProperty = true;
                        break;

                    case QType.Id:
                        string curId = (string)token.Param;
                        bool startsWithDot = curId.StartsWith(".", StringComparison.Ordinal);
                        if (dotIsProperty)
                        {
                            if (!startsWithDot)
                                tokenQueue.ThrowParsingException("Expected .");
                            token.Param = curId.Substring(1);
                            token.ExtraInt = 1;
                        }
                        else if (token.ExtraInt == 0)
                        {
                            var n = tokenQueue.Peek();
                            if (n != null && (n.TokenType != QType.ParenthesisOpen) && (n.TokenType != QType.SquareOpen))
                                goto case QType.Value;
                        }
                        stack.Push(token);
                        argumentExpected = false;
                        dotIsProperty = true;
                        break;
                    case QType.Colon:
                        tokenQueue.Push(token);
                        exitLoop = true;
                        break;
                    case QType.Operator:
                        Operators op = token.Operator;
                        if (argumentExpected || !dotIsProperty)
                        {
                            if (op == Operators.Minus) token = new QToken(op = Operators.UnaryMinus);
                            if (op == Operators.Plus) token = new QToken(op = Operators.UnaryPlus);
                        }

                        cleanStack(queue, stack, op);

                        if (precedenceAtLeast >= 0 && s_precedence[op] < precedenceAtLeast && stack.Count == 0)
                        {
                            tokenQueue.Push(token);
                            exitLoop = true;
                            argumentExpected = false;
                            break;
                        }
                        
                        switch (op)
                        {
                            case Operators.Or:
                            case Operators.And:
                                {
                                    // Or and AND short-circuit, so a || b || c => a?true:(b?true:c)
                                    // and a && b &&c => a?(b?c:false):false
                                    Stack<IOperation> terms = new Stack<IOperation>();
                                    bool isOr = (op == Operators.Or);
                                    do
                                    {
                                        var e1 = parseSingleStatement(tokenQueue, s_precedence[op] + 1);
                                        if (e1 == null)
                                            tokenQueue.ThrowParsingException("Expression expected after " + (isOr ? "||" : "&&"));
                                        terms.Push(e1);
                                        var n = tokenQueue.Peek();
                                        if (n == null || !n.IsOperator(op))
                                            break;
                                        tokenQueue.Pop();
                                    } while (true);

                                    IOperation o = null;
                                    while (terms.Count > 0)
                                    {
                                        if (o == null)
                                            o = terms.Pop();
                                        else
                                            o = new Operations.OperationExpression(terms.Pop(),
                                                                                    (isOr) ? new Operations.OperationConditional(createConstant(true), o) :
                                                                                          new Operations.OperationConditional(o, createConstant(false)));
                                    }
                                    o = (isOr) ? new Operations.OperationConditional(createConstant(true), o) :
                                                new Operations.OperationConditional(o, createConstant(false));

                                    queue.Enqueue(o);
                                    argumentExpected = dotIsProperty = false;
                                    break;
                                }
                            case Operators.Conditional:
                                {
                                    var e1 = parseSingleStatement(tokenQueue, -1);
                                    if (e1 == null)
                                        tokenQueue.ThrowParsingException("Failed to parse the first part of the conditional expression");
                                    var n = tokenQueue.Pop();
                                    if (n == null || n.TokenType != QType.Colon)
                                        tokenQueue.ThrowParsingException("Expected :");

                                    var e2 = parseSingleStatement(tokenQueue, -1);
                                    if (e2 == null)
                                        tokenQueue.ThrowParsingException("Failed to parse the second part of the conditional expression");
                                    queue.Enqueue(new Operations.OperationConditional(e1, e2));
                                    argumentExpected = dotIsProperty = false;
                                }
                                break;
                            case Operators.Coalesce:
                                {
                                    var e2 = parseSingleStatement(tokenQueue, -1);
                                    if (e2 == null)
                                        tokenQueue.ThrowParsingException("Expression expected after ??");

                                    queue.Enqueue(new Operations.OperationCoalesce(e2));
                                    argumentExpected = dotIsProperty = false;
                                }
                                break;

                            case Operators.Comma:
                                while (stack.Count > 0 && !stack.Peek().IsOpenBrace)
                                    queue.Enqueue(operationFromToken(stack.Pop()));
                                if (stack.Count > 0 && stack.Peek().IsOpenBrace)
                                {
                                    var p = stack.Pop();
                                    if (p.ExtraInt <= 0)
                                        p.ExtraInt = 1;
                                    p.ExtraInt++; // Count the number of comma-separated entities in braces
                                    stack.Push(p);
                                }
                                argumentExpected = dotIsProperty = false;
                                break;
                            case Operators.New:
                            case Operators.Is:
                            case Operators.As:
                                // After this there is a typename, which in case of typeof() MUST be in parentheses (unlike C++)
                                token.Param = tokenQueue.ReadTypeName();
                                if (token.Param==null && op!=Operators.New)
                                    tokenQueue.ThrowParsingException("Expected type");
                                stack.Push(token);
                                argumentExpected = false;
                                dotIsProperty = true;
                                break;

                            default:
                                argumentExpected = true;
                                dotIsProperty = false;
                                stack.Push(token);
                                break;
                        }
                        break;

                    case QType.ParenthesisClose:
                    case QType.BlockClose:
                    case QType.SquareClose:
                        if (argumentExpected)
                            tokenQueue.ThrowParsingException("Argument expected");
                        QType match = QType.ParenthesisOpen;
                        if (token.TokenType == QType.SquareClose) match = QType.SquareOpen;
                        if (token.TokenType == QType.BlockClose) match = QType.BlockOpen;

                        while (stack.Count > 0 && stack.Peek().TokenType != match)
                        {
                            var sp = stack.Pop();
                            if (sp.IsOpenBrace)
                                tokenQueue.ThrowParsingException("Parentheses or square brackets do not match");
                            queue.Enqueue(operationFromToken(sp));

                        }

                        if (stack.Count == 0)
                        {
                            tokenQueue.Push(token);
                            exitLoop = true;
                            break;
                        }

                        var args = stack.Pop().ExtraInt;
                        if (args < 0)
                            args = (args == -(queue.Count + stack.Count + 1)) ? 0 : 1;

                        var nextToken = tokenQueue.Peek();

                        if (stack.Count > 0 && stack.Peek().IsOperator(Operators.New))
                        {
                            var newOp = stack.Pop();
                            switch (token.TokenType)
                            {
                                case QType.SquareClose:
                                    if (nextToken == null || nextToken.TokenType != QType.BlockOpen)
                                        queue.Enqueue(new Operations.OperationNewObject((string)newOp.Param, args, false));
                                    else
                                    {
                                        newOp.ExtraInt = args;
                                        stack.Push(newOp);
                                    }
                                    break;
                                case QType.BlockClose:
                                    queue.Enqueue(new Operations.OperationCreateBlock(args));
                                    queue.Enqueue(new Operations.OperationNewObject((string)newOp.Param, newOp.ExtraInt + 1, (token.TokenType == QType.BlockClose)));
                                    break;
                                default:
                                    queue.Enqueue(new Operations.OperationNewObject((string)newOp.Param, args, (token.TokenType == QType.BlockClose)));
                                    break;
                            }
                        }
                        else if (token.TokenType == QType.ParenthesisClose || token.TokenType == QType.SquareClose)
                        {
                            if (stack.Count > 0 && (stack.Peek().TokenType == QType.Id))
                            {
                                var pop = stack.Pop();
                                string id = (string)pop.Param;
                                bool isProperty = (token.TokenType == QType.SquareClose);
                                queue.Enqueue(new Operations.OperationCall(id, pop.ExtraInt != 0, isProperty, args));
                            }
                            else if (token.TokenType == QType.SquareClose)
                                queue.Enqueue(new Operations.OperationCall(string.Empty, true, true, args));
                        }
                        else // if (token.Type == QType.BlockClose)
                        {
                            queue.Enqueue(new Operations.OperationCreateBlock(args));
                        }
                        argumentExpected = false;
                        dotIsProperty = true;
                        break;

                }

            }

            if (argumentExpected)
                tokenQueue.ThrowParsingException("Argument expected");

            while (stack.Count > 0)
                queue.Enqueue(operationFromToken(stack.Pop()));

            IOperation ex;
            if (queue.Count == 0)
                return null;
            if (queue.Count == 1)
                ex = queue.Peek();
            else
                ex = new Operations.OperationExpression(queue.ToArray());
            if (ex.StackBalance != 1)
                tokenQueue.ThrowParsingException("Invalid expression syntax");
            return ex;
        }

        private static bool isRightAssociative(Operators o)
        {
            return o == Operators.UnaryMinus || o == Operators.UnaryPlus || o==Operators.New;
        }
        private static void cleanStack(Queue<IOperation> queue, Stack<QToken> stack, Operators o1)
        {
            while (stack.Count > 0)
            {
                var pk = stack.Peek();
                bool en = false;

                if (pk.TokenType == QType.Operator)
                {
                    var o2 = pk.Operator;
                    if (isRightAssociative(o1))
                        en = (s_precedence[o1] < s_precedence[o2]);
                    else
                        en = (s_precedence[o1] <= s_precedence[o2]);
                }
                else
                    en = (pk.TokenType == QType.Id && pk.ExtraInt != 0);

                if (en)
                    queue.Enqueue(operationFromToken(stack.Pop()));
                else
                    break;
            }
        }

        private static IOperation createConstant(object obj)
        {
            var trueOp = new Operations.OperationVariableAccess();
            trueOp.AddValue(obj);
            return trueOp;

        }
        private static IOperation operationFromToken(QToken token)
        {
            switch (token.TokenType)
            {
                case QType.Id:
                    return new Operations.OperationCall((string)token.Param, token.ExtraInt != 0, true, 0);
                case QType.Value:
                    return new Operations.OperationPush(token.Param);
                case QType.Typeof:
                    return new Operations.OperationTypeOf((string)token.Param);
                case QType.Subexpr:
                    return (IOperation)token.Param;
                case QType.Operator:
                    Operations.OperatorType op;
                    var o = token.Operator;
                    if (s_operatorTrans.TryGetValue(o, out op))
                        return new Operations.OperationOperator(op);
                    switch (o)
                    {
                        case Operators.TypeCast: return new Operations.OperationTypecast((string)token.Param);
                        case Operators.Is: return new Operations.OperationIs((string)token.Param);
                        case Operators.As: return new Operations.OperationAs((string)token.Param);
                        default: throw new ArgumentOutOfRangeException("Unexpected operator " + token.Operator);
                    }
            }
            if (token.IsOpenBrace)
                throw new ParsingException("Unmatched " + token.TokenType);
            throw new ArgumentOutOfRangeException("Unexpected token of type " + token.TokenType);
        }

        /// Parse multi-expression, like ${a|b|=2+3}
        public IOperation ParseMulti(ParsingReader reader)
        {
            Operations.OperationVariableAccess va = new Operations.OperationVariableAccess();
            StringBuilder sb = new StringBuilder();
            bool orMet = true;
            int n;
            while ((n=reader.Peek())!=-1)
            {
                var q = (char)n;
                if (q == '}' || q == ')' || q == ']')
                    break;
                switch (q)
                {
                    case '|':
                        reader.Read();
                        va.AddName(sb.ToString());
                        sb.Length = 0;
                        orMet = true;
                        break;
                    case '\'':
                    case '\"':
                    case '`':
                        orMet = false;
                        if (sb.Length != 0)
                            reader.ThrowParsingException("Quote must be a first character");
                        va.AddValue(reader.ReadQuote());
                        reader.SkipWhiteSpaceAndComments();
                        if (reader.Peek() != '}' && reader.Peek() != '|')
                            reader.ThrowParsingException("Unexpected character '" + (char)reader.Peek() + "'");
                        sb.Length = 0;
                        break;
                    case '=':
                        orMet = false;
                        reader.Read();
                        var v = Parse(reader);
                        if (v != null)
                            va.AddExpression(v);
                        else
                            va.AddValue(null);
                        break;

                    default:
                        orMet = false;
                        sb.Append(q);
                        reader.Read();
                        break;
                }
            }
            if (sb.Length > 0 || orMet)
                va.AddName(sb.ToString());
            return va;
        }

        class TokenQueue
        {
            private ParsingReader _reader;
            private Stack<QToken> _front = new Stack<QToken>();
            private Parser _parser;
            
            public TokenQueue(Parser p, ParsingReader r)
            {
                _reader = r;
                _parser = p;
            }
            public QToken Pop()
            {
                if (_front.Count > 0)
                    return _front.Pop();
                return parseToken();
            }

            
            public void ThrowParsingException(string s)
            {
                _reader.ThrowParsingException(s);
            }

            public QToken Peek()
            {
                if (_front.Count > 0)
                    return _front.Peek();
                var q = parseToken();
                if (q != null)
                    _front.Push(q);
                return q;
            }

            public void Push(QToken token)
            {
                if (_front.Count == 0 && _reader.CanPoke)
                {
                    switch (token.TokenType)
                    {
                        case QType.SquareClose: _reader.Poke(']'); return;
                        case QType.ParenthesisClose: _reader.Poke(')'); return;
                        case QType.BlockClose: _reader.Poke('}'); return;
                        case QType.Colon: _reader.Poke(':'); return;
                    }
                }
                _front.Push(token);
            }

            private string readTypeName(Stack<QToken> tokensRead)
            {
                StringBuilder sb = new StringBuilder();
                QToken q;
                while ((q = Peek()) != null)
                {
                    
                    if (q.TokenType == QType.Id)
                    {
                        if (sb.Length == 0 || (sb[sb.Length - 1] == '.' || ((string) q.Param).StartsWith(".", StringComparison.Ordinal)))
                            sb.Append(q.Param);
                        else
                            return null;
                        tokensRead.Push(Pop());
                        continue;
                    }
                    if (q.IsOperator(Operators.Conditional))
                    {
                        tokensRead.Push(Pop());

                        if (Peek() == null || Peek().IsCloseBrace)
                            sb.Append("?");
                        else
                            return null;
                        
                        continue;
                    }
                    if (q.TokenType==QType.SquareOpen)
                    {
                        sb.Append("[]");
                        tokensRead.Push(Pop());
                        var n = Peek();
                        if (n == null || n.TokenType != QType.SquareClose)
                            return null;
                        tokensRead.Push(Pop());
                    }
                    
                    break;
                }

                return sb.Length==0?null:sb.ToString();
            }

            public string ReadTypeName()
            {
                Stack<QToken> tok = new Stack<QToken>();
                var s=readTypeName(tok);
                if (s == null)
                {
                    foreach (var qToken in tok)
                        Push(qToken);
                }
                return s;
            }

            public string GetTypeNameFollowedByClosingParenthesis()
            {
                Stack<QToken> tok = new Stack<QToken>();
                string s = readTypeName(tok);
                QToken q;
                if (s!=null && (q=Pop())!=null)
                {
                    tok.Push(q);
                    if (q.TokenType!=QType.ParenthesisClose)
                        s = null;
                }
                if (s == null)
                {
                    foreach (var qToken in tok)
                        Push(qToken);
                }
                return s;
            }

            private bool check(char ch)
            {
                if (_reader.Peek() == ch)
                {
                    _reader.Read();
                    return true;
                }
                return false;
            }

            private QToken parseToken()
            {
                var r = _reader;
                r.SkipWhiteSpaceAndComments();
                int n = r.Peek();
                if (n == -1)
                    return null;

                char ch = (char)n;
                switch (ch)
                {
                    case '#':
                        r.Read();

                        // #? or ## = dump
                        if (r.Peek() == '?' || r.Peek() == '#')
                        {
                            r.Read();
                            return new QToken(Operators.Dump);
                        }

                        // Could be an operator like #AND#
                        string str = r.ReadUntil('#');
                        r.ReadAndThrowIfNot('#');
                        Operators op;
                        if (s_names.TryGetValue(str.ToUpperInvariant(), out op))
                            return new QToken(op);

                        // Then it's a data like #2009-09-01#
                        return new QToken(QType.Value,DateTime.Parse(str));
                    case '?': r.Read();
                        if (r.Peek() == '?')
                        {
                            r.Read();
                            return new QToken(Operators.Coalesce);
                        }
                        return new QToken(Operators.Conditional);
                    case '&': r.Read(); return new QToken(check('&') ? Operators.And : Operators.BinaryAnd);
                    case '|': r.Read(); return new QToken(check('|') ? Operators.Or : Operators.BinaryOr);
                    case '^': r.Read(); return new QToken(Operators.BinaryXor);
                    case '<': r.Read(); return new QToken(check('=') ? Operators.LessOrEqual : Operators.Less);
                    case '>': r.Read(); return new QToken(check('=') ? Operators.GreaterOrEqual : Operators.Greater);
                    case '!': r.Read();
                        if (r.Peek() == '~') { r.Read(); return new QToken(Operators.NotEqualIgnoreCase); }
                        return new QToken(check('=') ? Operators.NotEqual : Operators.Not);
                    case '=': r.Read(); 
                        if (r.Peek() == '~') { r.Read(); return new QToken(Operators.EqualIgnoreCase); }
                        return new QToken(Operators.Equal);
                    case '~': r.Read(); return new QToken(Operators.BinaryNot);
                    case '(': r.Read(); return new QToken(QType.ParenthesisOpen);
                    case ')': r.Read(); return new QToken(QType.ParenthesisClose);
                    case '[': r.Read(); return new QToken(QType.SquareOpen);
                    case ']': r.Read(); return new QToken(QType.SquareClose);
                    case '{': r.Read(); return new QToken(QType.BlockOpen);
                    case '}': r.Read(); return new QToken(QType.BlockClose);
                    case ':': r.Read(); return new QToken(QType.Colon);
                    case '+': r.Read(); return new QToken(Operators.Plus);
                    case '-': r.Read(); return new QToken(Operators.Minus);
                    case '/': r.Read(); return new QToken(Operators.Divide);
                    case '*': r.Read(); return new QToken(Operators.Multiply);
                    case '%': r.Read(); return new QToken(Operators.Modulo);
                    case ',': r.Read(); return new QToken(Operators.Comma);
                    case '\'':
                    case '`':
                    case '"': return new QToken(QType.Value, r.ReadQuote());
                    case '$': r.Read(); return new QToken(QType.Subexpr, parseVar());

                    default:
                        bool number = char.IsDigit(ch);
                        if (ch == '.')
                        {
                            r.Read();
                            int cc = r.Peek();
                            number = (cc != -1) && Char.IsDigit((char)cc);
                            r.Poke('.');
                        }
                        if (number)
                        {
                            var nn = r.ReadNumber();
                            if (nn == null)
                                ThrowParsingException("Invalid number");
                            return new QToken(QType.Value, nn);
                        }

                        StringBuilder sb = new StringBuilder();
                        
                        while ((n = (char)r.Peek())!=-1)
                        {
                            ch = (char) n;
                            if (char.IsLetterOrDigit(ch) || ch == '.' || _parser._tokenSpecialCharacters.IndexOf(ch) != -1)
                            {
                                sb.Append(ch);
                                r.Read();
                            }
                            else if (char.IsWhiteSpace(ch) && sb.Length > 0 && sb[sb.Length - 1] == '.')
                                r.SkipWhiteSpaceAndComments();
                            else
                                break;
                        }
                        if (sb.Length > 0)
                        {
                            switch (sb.ToString())
                            {
                                case "new": return new QToken(Operators.New);
                                case "throw": return new QToken(Operators.Throw);
                                case "is": return new QToken(Operators.Is);
                                case "as": return new QToken(Operators.As);
                                case "typeof": return new QToken(QType.Typeof, null);
                            }

                            return new QToken(QType.Id, sb.ToString());
                        }
                        return null;
                }
            }


            private IOperation parseVar()
            {
                Operations.OperationVariableAccess va = new Operations.OperationVariableAccess();

                var cc = _reader.Peek();
                bool blockMode = (cc== '{');

                StringBuilder sb = new StringBuilder();
                if (!blockMode)
                {
                    while ((cc=_reader.Peek())!=-1)
                    {
                        var ch = (char) cc;
                        if (!char.IsLetterOrDigit(ch) && _parser._tokenSpecialCharacters.IndexOf(ch) == -1)
                            break;

                        sb.Append(ch);
                        _reader.Read();

                    }
                    va.AddName(sb.ToString());
                    return new Operations.OperationExpression(new IOperation[] { va });
                }
                _reader.Read();  // Read {
                var v = _parser.ParseMulti(_reader);
                _reader.SkipWhiteSpaceAndComments();
                _reader.ReadAndThrowIfNot('}'); // eat }
                return v;
            }

        }
    }
}