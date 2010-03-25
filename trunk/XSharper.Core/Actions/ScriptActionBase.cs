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
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace XSharper.Core
{
    ///<summary>
    /// Attribute to mark an XML attribute which value must be transformed
    ///</summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class XsNotTransformed : Attribute
    {
    }

    /// <summary>
    /// Wrapper for return values from IScriptAction.execute() method.
    /// 
    /// Because null has a special meaning as "no return value", to return
    /// null from execute() method ReturnValue.Null should be returned instead.
    /// 
    /// Also it can be used to break loop execution, as ReturnValue.Break
    /// </summary>
    public class ReturnValue
    {
        private readonly object _value;
        private readonly bool _exception;

        /// Null return value
        public static readonly ReturnValue Null=new ReturnValue(null);

        /// exit the innermost loop return value
        public static readonly ReturnValue Break= new ReturnValue(null);

        /// <summary>
        /// Create return value
        /// </summary>
        /// <param name="value">value to return</param>
        public ReturnValue(object value)
        {
            _value = value;
        }

        private ReturnValue(object value, bool exception)
        {
            _value = value;
            _exception = exception;
        }

        /// Create return value
        public static ReturnValue Create(object value)
        {
            return new ReturnValue(value);
        }
        /// Create exception return value
        public static ReturnValue CreateException(Exception value)
        {
            return new ReturnValue(value, true);
        }

        /// <summary>
        /// Get stored value
        /// </summary>
        public object Value { get { return _value;} }

        /// <summary>
        /// Returns true if the specified object is a command to break out of the innermost loop
        /// </summary>
        /// <param name="obj">Object to test</param>
        /// <returns>true if the specified object is a command to break out of the innermost loop</returns>
        public static bool IsBreak(object obj)
        {
            return obj != null && ReferenceEquals(obj, Break); 
        }

        /// <summary>
        /// Throws the exception if return value is exception
        /// </summary>
        /// <param name="obj">Object to test</param>
        public static void RethrowIfException(object obj)
        {
            if (obj != null && obj is ReturnValue)
            {
                var rv = (ReturnValue) obj;
                if (rv._exception)
                    Utils.Rethrow((Exception) rv._value);
            }
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object.
        public override string ToString()
        {
            if (Value==null)
                return null;
            return Value.ToString();
        }

        /// <summary>
        /// Extract the contained value out of the provided object, if it is of type ReturnValue
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Unwrap(object obj)
        {
            if (obj is ReturnValue)
                return ((ReturnValue)obj).Value;
            return obj;
        }
    }

    /// <summary>
    /// Every script action implements this interface
    /// </summary>
    public interface IScriptAction 
    {
        /// <summary>
        /// A unique ID of the script action or null.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Do the necessary initialization. This is executed after the script is loaded, but before it is compiled to an executable.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Execute action. This method is invoked after <see cref="Initialize"/>
        /// </summary>
        object Execute();

        /// <summary>
        /// Execute the specified action for every child action
        /// </summary>
        /// <param name="func">Action to be executed</param>
        /// <param name="isFind">true if this is called to find an action by its <see cref="Id"/>.</param>
        bool ForAllChildren(Predicate<IScriptAction> func,bool isFind);

        /// <summary>
        /// Get property value. An exception is thrown if property with this name is not found
        /// </summary>
        /// <param name="name">Property name</param>
        /// <returns>Property value</returns>
        /// <exception cref="KeyNotFoundException">Thrown if property with the specified name is not found</exception>
        object GetAttr(string name);

        /// <summary>
        /// Set property value. An exception is thrown if property with this name is not found.
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">Property value</param>
        /// <exception cref="KeyNotFoundException">Thrown if property with the specified name is not found</exception>
        void SetAttr(string name, object value);
    }


    /// <summary>
    /// <see cref="XsElement"/> with <see cref="Transform"/> field, specifying how to transform its string properties before use
    /// </summary>
    public abstract class XsTransformableElement : XsElement
    {
        /// Flags specifying how to transform string properties of the action before use
        [Description("Flags specifying how to transform string properties of the action before use")]
        [XsAttribute("tr")]
        [XsAttribute("transform")]
        public TransformRules Transform { get; set; }

        /// Constructor
        protected XsTransformableElement()
        {
            Transform = TransformRules.Default;
        }
    }

    /// <summary>
    /// Default implementation of all methods of IScriptAction interface, except execute()
    /// </summary>
    public abstract class ScriptActionBase : XsTransformableElement, IScriptAction
    {
        private string _elementName;

        /// Default XSharper namespace
        public const string XSharperNamespace = "http://www.xsharper.com/schemas/1.0";

        /// A unique ID of the script action or null.
        [Description("A unique ID of the script action")]
        [XsNotTransformed]
        public string Id
        {
            get; set;
        }

        /// <summary>
        /// Convenience property to return ScriptContext.Current
        /// </summary>
        protected ScriptContext Context
        {
            get { return ScriptContextScope.Current; }
        }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public virtual bool ForAllChildren(Predicate<IScriptAction> func, bool isFind)
        {
            return false;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public virtual void Initialize() 
        {
            
            foreach (PropertyInfo c in GetType().GetProperties())
            {
                bool valSet = false;
                object val=null;
                foreach (XsElementAttribute e in CustomAttributeHelper.All<XsElementAttribute>(c))
                {
                    if (!valSet)
                    {
                        val= c.GetValue(this, null);
                        valSet = true;
                    }
                    ScriptActionBase bl = val as ScriptActionBase;
                    if (bl != null && bl._elementName == null)
                        bl._elementName = e.Name;
                    continue;
                }

                if (CustomAttributeHelper.Has<XsRequiredAttribute>(c))
                {
                    if (!valSet)
                    {
                        if (c.GetIndexParameters().Length > 0)
                            continue;

                        val = c.GetValue(this, null);
                    }
                    if (val == null)
                        throw new ParsingException(string.Format("Required attribute '{0}' is not set", XsAttributeAttribute.GetNames(c, false)[0]));
                }
                if ((c.PropertyType == typeof(object) || c.PropertyType == typeof(string)) && 
                    !CustomAttributeHelper.Has<XsNotTransformed>(c) && !CustomAttributeHelper.Has<XmlIgnoreAttribute>(c) &&
                    c.GetSetMethod()!=null)
                {
                    if (!valSet)
                    {
                        if (c.GetIndexParameters().Length > 0)
                            continue;

                        val = c.GetValue(this, null);
                    }
                    if (val!=null)
                        if (val is string)
                            Context.AssertGoodTransform((string)val, Transform);
                        else
                            Context.AssertGoodTransform(val.ToString(), Transform);
                }
            }
        }

        /// <summary>
        /// Abstract method to execute action
        /// </summary>
        /// <returns>return value or null, if execution must be continued</returns>
        public virtual object Execute()
        {
            return null;
        }



        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            XsTypeAttribute v = CustomAttributeHelper.First<XsTypeAttribute>(GetType());
            if (_elementName != null)
                sb.Append(_elementName);
            else
                sb.Append((v == null) ? GetType().FullName : v.Name);
            sb.Append("(");

            bool first = true;
            if (!string.IsNullOrEmpty(Id))
            {
                sb.AppendFormat("id=\"{0}\"", Id);
                first = false;
            }
            object def = null;
            foreach (PropertyInfo pi in GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.SetProperty))
            {
                var n = XsAttributeAttribute.GetNames(pi,false);
                if (n==null || n[0]=="id" || n[0]=="password")
                    continue;
                
                object o = pi.GetValue(this, null);
                if (def == null)
                    def = Utils.CreateInstance(GetType());
                if (o==null || o.Equals(pi.GetValue(def,null)) || (n[0]==string.Empty && n.Length==1))
                    continue;


                if (!first)
                    sb.Append(", ");

                first = false;
                sb.Append(n[0]);
                sb.Append("=");
                sb.Append("\"");
                string s = o.ToString().Trim();
                s = s.Replace("\r", "\\r");
                s = s.Replace("\n", "\\n");
                s = s.Replace("\t", "\\t");

                int maxW = (n[0] == "from" || n[0] == "location") ? 50 : 30;
                var fw = (n[0] == "from" || n[0] == "location") ? FitWidthOption.EllipsisStart : FitWidthOption.EllipsisEnd;

                sb.Append(Utils.FitWidth(s, maxW, fw));
                sb.Append("\"");
            
            }
            
            sb.Append(")");
            return sb.ToString();
        }




        

        /// <summary>
        /// Get property value. An exception is thrown if property with this name is not found
        /// </summary>
        /// <param name="name">Property name</param>
        /// <returns>Property value</returns>
        /// <exception cref="KeyNotFoundException">Thrown if property with the specified name is not found</exception>
        public object GetAttr(string name)
        {
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            foreach (PropertyInfo c in props)
            {
                var n = XsAttributeAttribute.GetNames(c,true);
                if (n!=null)
                {
                    foreach (var s in n)
                        if (string.Compare(s, name, StringComparison.OrdinalIgnoreCase) == 0)
                            return c.GetValue(this, null);
                }
            }
            
            throw new KeyNotFoundException("Property '" + name + "' not found");
        }
        /// <summary>
        /// Set property value. An exception is thrown if property with this name is not found.
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">Property value</param>
        /// <exception cref="KeyNotFoundException">Thrown if property with the specified name is not found</exception>
        public void SetAttr(string name, object value)
        {
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            foreach (PropertyInfo c in props)
            {
                var n = XsAttributeAttribute.GetNames(c,true);
                if (n != null)
                {
                    foreach (var s in n)
                        if (string.Compare(s, name, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            c.SetValue(this, Utils.To(c.PropertyType, value), null);
                            return;
                        }
                }
            }

            throw new KeyNotFoundException("Property '" + name + "' not found");
        }
        
        

        /// <summary>
        /// Output verbose message prefixed by the type name and >
        /// </summary>
        /// <param name="text">message text</param>
        protected void VerboseMessage(string text)
        {
            Context.WriteVerbose(Utils.PrefixEachLine(GetType().Name + "> ", text));
        }

        /// <summary>
        /// Format and output verbose message prefixed by the type name and >
        /// </summary>
        /// <param name="text">message text</param>
        /// <param name="p">optional parameters</param>
        protected void VerboseMessage(string text, params object[] p)
        {
            Context.WriteVerbose(Utils.PrefixEachLine(GetType().Name + "> ", string.Format(text, p)));
        }

        /// <summary>
        /// Process inner node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">Reader</param>
        protected override void ProcessInnerNode(IXsContext context, XmlReader reader)
        {
            PropertyInfo collProperty = null;

            switch (reader.NodeType)
            {
                case XmlNodeType.Comment:
                    collProperty = FindCollectionForNode(reader);
                    if (collProperty != null)
                        SetChildObject(reader, new Rem(reader.Value), null, collProperty);
                    reader.Read();
                    break;
                case XmlNodeType.ProcessingInstruction:
                    IScriptAction a = null;

                    if (string.Compare(reader.LocalName, "_", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(reader.LocalName, "code", StringComparison.OrdinalIgnoreCase) == 0)
                        a = new Code(reader.Value);
                    else if (string.Compare(reader.LocalName, "_d", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(reader.LocalName, "codeD", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(reader.LocalName, "codeDynamic", StringComparison.OrdinalIgnoreCase) == 0)
                        a = new Code(reader.Value) {Dynamic = true};
                    else if (string.Compare(reader.LocalName, "header", StringComparison.OrdinalIgnoreCase) == 0 ||
                             string.Compare(reader.LocalName, "h", StringComparison.OrdinalIgnoreCase) == 0)
                        a = new Header(reader.Value);
                    else if (string.Compare(reader.LocalName, "headerWithTypes", StringComparison.OrdinalIgnoreCase) == 0 ||
                             string.Compare(reader.LocalName, "ht", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var h = new Header(reader.Value) {WithTypes = true};
                        h.RunWithTypes(context);
                        a = h;
                    }
                    else if (string.Compare(reader.LocalName, "rem", StringComparison.OrdinalIgnoreCase) == 0)
                        a = new Rem { Text = reader.Value };
                    else if (string.Compare(reader.LocalName, "xsharper-args", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        reader.Read();
                        break;
                    }

                    collProperty = FindCollectionForNode(reader);

                    if (a == null)
                        throw new XsException(reader, string.Format("Unexpected processing instruction {0}", reader.Name));
                    

                    IList coll = null;
                    if (collProperty!=null)
                        coll=collProperty.GetValue(this, null) as IList;
                    if (this is Code)
                    {
                        if (a is Code)
                            ProcessAttribute(context, string.Empty, reader.Value, null);
                        else
                            ((Code) this).Add(a);
                    }
                    else if (coll == null)
                    {
                        if (!(a is Rem))
                            throw new XsException(reader, string.Format("Unexpected position of processing instruction {0}", reader.Name));
                    }
                    else
                        coll.Add(a);
                    
                    reader.Read();
                    break;
                default:
                    base.ProcessInnerNode(context, reader);
                    break;
            }
        }
        
    }
}