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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Threading;

namespace XSharper.Core
{
    /// ExecutableScriptBase 
    public abstract class ExecutableScriptBase : MarshalByRefObject
    {
        /// Script context
        protected ScriptContext   c { get { return ScriptContextScope.Current; } }
        
        /// Script context
        protected ScriptContext   Context { get { return ScriptContextScope.Current; }}

        /// Execute 
        public abstract object Execute();

        /// Get script source code, or null if no source code is available
        public virtual string GetSourceCode()
        {
            return null;
        }
    }

    
    /// C# code snippet
    [XsType("code", ScriptActionBase.XSharperNamespace)]
    [Description("C# code snippet (also can be inserted as <?_ ... ?>)")]
    public class Code : StaticValueFromFileBase
    {
        /// Constructor
        public Code()
        {
            Methods=new Sequence();
        }

        /// Constructor
        public Code(string text) : this()
        {
            Value = text;
        }

        // This is needed for XML Schema generation purposes only.
        // <code> element is special as it may have child elements, but these are
        // added to <methods> automatically, after appending a line to the code
        [XsElement("", SkipIfEmpty = true, CollectionItemType = typeof(IScriptAction))]
        internal List<IScriptAction> xml__items { get; set; }
        
        /// Collection of methods
        [XsElement("methods", SkipIfEmpty = true)]
        public Sequence Methods { get; set; }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func, bool isFind)
        {
            if (base.ForAllChildren(func,isFind))
                return true;
            
            // Methods are not visible outside of Code
            if (!isFind)
                if (func(Methods))
                    return true;

            return false;
        }

        private string _cachedClassName
        {
            get
            {
                return (string)Context.StateBag.Get(this, "cachedClassName", null);
            }
            set
            {
                Context.StateBag.Set(this, "cachedClassName", value);
            }
        }
        private Dictionary<string, bool> _methodNames
        {
            get
            {
                var v=(Dictionary<string, bool>)Context.StateBag.Get(this, "methodNames", null);
                if (v==null)
                {
                    v=new Dictionary<string, bool>();
                    Context.StateBag.Set(this,"methodNames",v);
                }
                return v;
            }
        }

        /// Get class name of the current snippet. Class name is calculated as SHA1 hash of the contents, to prevent unnecessary compilations
        public string GetClassName()
        {
            if (_cachedClassName == null || Dynamic)
            {
                using (SHA1 sha = new SHA1Managed())
                {
                    string text = GetTransformedValueStr();
                    _cachedClassName="XSharperSnippet_" + BitConverter.ToString(
                                                    sha.ComputeHash(
                                                        System.Text.Encoding.Unicode.GetBytes(text ?? string.Empty))
                                                    ).Replace("-", "");
                }
            }
            return _cachedClassName;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            _cachedClassName = null;
            Context.Initialize(Methods);
        }

        
        /// Add action to the code
        public void Add(IScriptAction action)
        {
            string methodId;
            for (int n = 0; ; ++n)
            {
                if (string.IsNullOrEmpty(action.Id))
                    methodId = "_" + CustomAttributeHelper.First<XsTypeAttribute>(action.GetType()).Name;
                else
                    methodId = action.Id;
                if (n != 0)
                    methodId += n;
                if (!_methodNames.ContainsKey(methodId))
                {
                    _methodNames[methodId] = true;
                    break;
                }
            }
            if (action is Script || methodId==action.Id || action is Sub)
            {
                Methods.Add(action);
            }
            else
            {
                Sequence b = new Sequence { Id = methodId };
                b.Add(action);
                Methods.Add(b);
                Value += Environment.NewLine + "{ object __ret=" + methodId + "_inline(); if (__ret!=null) return __ret;}";            
            }
            
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, XmlReader reader, PropertyInfo setToProperty)
        {
            Type t;
            if (setToProperty != null)
                t = setToProperty.PropertyType;
            else
                t = (context==null)?null:context.ResolveType(reader);
            if (t==null)
                throw new XsException(reader, string.Format("Unknown xml element '{0}'", reader.Name));

            object newObject = Utils.CreateInstance(t);
            IXsElement xse = newObject as IXsElement;
            if (xse != null)
                xse.ReadXml(context, reader);
            else
                reader.Skip();

            if (setToProperty != null)
                SetChildObject(reader, newObject, setToProperty, null);
            else
                Add((IScriptAction) newObject);

        }

        /// <summary>
        /// Set property of the current object to newObject
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <param name="newObject">New property value</param>
        /// <param name="setToProperty">If not null, <paramref name="newObject"/> must be assigned to this property</param>
        /// <param name="collProperty">If not null, <paramref name="newObject"/> must be added to this IList-derived collection property</param>
        protected override void SetChildObject(XmlReader reader, object newObject, PropertyInfo setToProperty, PropertyInfo collProperty)
        {
            if (setToProperty == GetType().GetProperty("Methods") && Methods != null)
            {
                foreach (var o in ((Sequence)newObject).Items)
                    Methods.Add(o);
                return;
            }
            base.SetChildObject(reader, newObject, setToProperty, collProperty);
        }

        /// Execute member function
        [DebuggerHidden]
        public object ExecuteMember(CallIsolation isolation, string name, object[] par)
        {
            IScriptAction f=Methods.Items.Find(delegate(IScriptAction x) { return x.Id == name; });
            if (f == null)
                throw new ParsingException("Cannot find method " + name);
            
            List<CallParam> cp=new List<CallParam>();
            if (par!=null)
                foreach (var o in par)
                    cp.Add(new CallParam(null, o,TransformRules.None));
            return Context.ExecuteAction(f, cp, isolation);
        
        }
        #region Block Members

        /// Generate source code of the snippet
        public string GenerateSourceCode(ScriptContext context,bool embedSourceCode, bool isPublic)
        {
            StringBuilder sb=new StringBuilder();
            sb.Append(
@"        {public}class {class} : {base}
        { 
            public override object Execute() 
                ");
            sb  .Replace("{class}", GetClassName())
                .Replace("{public}", isPublic?"public ":"")
                .Replace("{ctxclass}", context.Compiler.GetTypeName(context.GetType()))
                .Replace("{base}", context.Compiler.GetTypeName(typeof(ExecutableScriptBase)));

            sb.AppendLine();

            string text = GetTransformedValueStr() ?? string.Empty;
            
            sb.AppendLine("\t\t\t// ---- User code ----- ");
            var t = text.Trim();
            if (!t.StartsWith("{", StringComparison.Ordinal))
                sb.AppendLine("{");
            sb.Append(t);
            sb.AppendLine();
            sb.AppendLine("\t\t\t// ---- End of user code ----- ");
            if (!t.StartsWith("{", StringComparison.Ordinal))
                sb.AppendLine("\t\t\t;return null;}");

            if (Methods!=null)
                foreach (IScriptAction c in Methods.Items)
                {
                    sb.AppendLine("");
                    string par = "",par2="null";
                    if (c is Sub)
                    {
                        par = "params object[] par";
                        par2 = "par";
                    }
                    sb.AppendLine("\tprivate object  " + c.Id + "(" + par + ") { return XS.ReturnValue.Unwrap(((" + GetType().FullName + ")c.CallStack.Peek().ScriptAction).ExecuteMember(XS.CallIsolation.Default,\"" + c.Id + "\", " + par2 + "));}");
                    sb.AppendLine("\tprivate object  " + c.Id + "_inline(" + par + ") { return ((" + GetType().FullName + ")c.CallStack.Peek().ScriptAction).ExecuteMember(XS.CallIsolation.None,\"" + c.Id + "\", " + par2 + ");}");
                    sb.AppendLine("\tprivate object  " + c.Id + "<T>(" + par + ") { return XS.Utils.To<T>(XS.ReturnValue.Unwrap(((" + GetType().FullName + ")c.CallStack.Peek().ScriptAction).ExecuteMember(XS.CallIsolation.None,\"" + c.Id + "\", " + par2 + ")));}");
                }
            if (embedSourceCode)
            {
                sb.AppendLine("\t\t\tpublic override string GetSourceCode() {");
                sb.Append("\t\t\t\treturn ");

                sb.Append("@\"");
                StringReader reader = new StringReader(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)));
                char[] buf = new char[80];
                int n;
                while ((n = reader.ReadBlock(buf, 0, buf.Length)) != 0)
                {
                    sb.Append(buf, 0, n);
                    if (n == buf.Length)
                    {
                        sb.AppendLine();
                        sb.Append("\t\t\t\t");
                    }
                }
                sb.Append("\"");
                sb.AppendLine("\t\t\t;}");
            }
            sb.AppendLine("\t\t}");
            return Utils.TransformStr(sb.ToString(), TransformRules.NewLineToCRLF); 
        }


        /// <summary>
        /// Write element text
        /// </summary>
        /// <param name="writer">XML writer</param>
        protected override void WriteText(XmlWriter writer)
        {
            string text = Utils.To<string>(Value);
            if (text != null)
            {
                text = text.Trim() + Environment.NewLine;
                if (text.Length > 0)
                {
                    if (text.IndexOfAny("><&".ToCharArray()) != -1)
                    {
                        if (!text.Contains("?>"))
                            writer.WriteProcessingInstruction("_", text);
                        else
                            writer.WriteCData(text);
                    }
                    else
                        writer.WriteValue(text);
                }
            }
        }

        /// Execute action
        public override object Execute()
        {
            object o = base.Execute();
            if (o != null)
                return o;

            var type = Context.GetClassInstanceType(this);
            ExecutableScriptBase es = (ExecutableScriptBase)Utils.CreateInstance(type);

            return es.Execute();
        }

        #endregion

        
    }
}