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
using System.ComponentModel;
using System.IO;
using System;

namespace XSharper.Core
{
    /// <summary>
    /// Execute block if ANY or ALL of the conditions are true (logical OR or AND depending on All property value)
    /// </summary>
    public abstract class Conditional : Block
    {
        /// true, if all conditions must be checked (AND)
        [Description("true, if all conditions must be checked (AND)")]
        public bool All { get; set; }

        /// true, if variable with the specified name is set
        [Description("true, if variable with the specified name is set")]
        public string IsSet { get; set; }

        /// true, if variable with the specified name is not set
        [Description("true, if variable with the specified name is not set")]
        public string IsNotSet { get; set; }

        /// true, if the expression is null
        [Description("true, if the expression is null")]
        public string IsNull { get; set; }

        /// true, if the expression is not null
        [Description("true, if the expression is not null")]
        public string IsNotNull { get; set; }

        /// true, if the expression is equal to integer 0
        [Description("true, if the expression is equal to integer 0")]
        public string IsZero { get; set; }

        /// true, if the expression is not equal to integer 0
        [Description("true, if the expression is not equal to integer 0")]
        public string IsNotZero { get; set; }


        /// true, if the expression is equal to integer 0
        [Description("true, if the expression is equal to integer 0")]
        public string Is0 { get; set; }

        /// true, if the expression is not equal to integer 0
        [Description("true, if the expression is not equal to integer 0")]
        public string IsNot0 { get; set; }

        /// true, if the expression is boolean 'true'
        [Description("true, if the expression is boolean 'true'")]
        public string IsTrue { get; set; }

        /// true, if the expression is boolean 'true'
        [Description("true, if the expression is boolean 'true'")]
        public string Condition { get; set; }

        /// true, if the expression is boolean 'false'
        [Description("true, if the expression is boolean 'false'")]
        public string IsFalse { get; set; }

        /// true, if the expression is boolean 'false'
        [Description("true, if the expression is boolean 'false'")]
        public string IsNotTrue { get; set; }

        /// true, if the expression is boolean 'false'
        [Description("true, if the expression is boolean 'false'")]
        public string IsNotCondition { get; set; }

        
        /// true, if the string is an empty string
        [Description("true, if the string is an empty string")]
        public string IsEmpty { get; set; }

        /// true, if the string is not an empty string
        [Description("true, if the string is not an empty string")]
        public string IsNotEmpty { get; set; }

        /// true, if the file or directory exists
        [Description("true, if the file or directory exists")]
        public string Exists { get; set; }

        /// true, if the file or directory does not exist
        [Description("true, if the file or directory does not exist")]
        public string NotExists { get; set; }

        /// true, if the file or directory does not exist
        [Description("true, if the file or directory does not exist")]
        public string DoesNotExist { get; set; }


        /// true, if the file exists
        [Description("true, if the file exists")]
        public string IsFile { get; set; }

        /// true, if the file does not exist
        [Description("true, if the file does not exist")]
        public string IsNotFile { get; set; }

        /// true, if the file exists
        [Description("true, if the file exists")]
        public string FileExists { get; set; }

        /// true, if the file does not exist
        [Description("true, if the file does not exist")]
        public string FileNotExists { get; set; }

        /// true, if the file does not exist
        [Description("true, if the file does not exist")]
        public string FileDoesNotExist { get; set; }

        /// true, if the directory exists
        [Description("true, if the directory exists")]
        public string IsDirectory { get; set; }

        /// true, if the directory does not exist
        [Description("true, if the directory does not exist")]
        public string IsNotDirectory { get; set; }
        
        /// true, if the directory exists
        [Description("true, if the directory exists")]
        public string DirectoryExists { get; set; }

        /// true, if the directory does not exist
        [Description("true, if the directory does not exist")]
        public string DirectoryNotExists { get; set; }

        /// true, if the directory does not exist
        [Description("true, if the directory does not exist")]
        public string DirectoryDoesNotExist { get; set; }


        /// Default constructor
        protected Conditional()
        {
        }
        /// Block constructor
        protected Conditional(params IScriptAction[] data)
            : base(data)
        {
        }


        /// Returns true if condition is satisfied or false otherwise
        protected bool ShouldRun()
        {
            bool? r=null;


            // True
            if (!string.IsNullOrEmpty(IsTrue))
            {
                r = (Utils.To<bool>(Context.Transform(IsTrue, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(Condition))
            {
                r = (Utils.To<bool>(Context.Transform(Condition, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // False
            if (!string.IsNullOrEmpty(IsFalse))
            {
                r = (!Utils.To<bool>(Context.Transform(IsFalse, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotTrue))
            {
                r = (!Utils.To<bool>(Context.Transform(IsNotTrue, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            if (!string.IsNullOrEmpty(IsNotCondition))
            {
                r = (!Utils.To<bool>(Context.Transform(IsNotCondition, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // Set/not set
            if (!string.IsNullOrEmpty(IsSet))
            {
                r= Context.IsSet(Context.TransformStr(IsSet, Transform));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotSet))
            {
                r = !Context.IsSet(Context.TransformStr(IsNotSet, Transform));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // Null / not null
            if (!string.IsNullOrEmpty(IsNull))
            {
                r = (Context.Transform(IsNull, Transform) == null);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotNull))
            {
                r = (Context.Transform(IsNotNull, Transform) != null);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }


            // Empty / not empty
            if (!string.IsNullOrEmpty(IsEmpty))
            {
                r = (string.IsNullOrEmpty(Context.TransformStr(IsEmpty, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotEmpty))
            {
                r = (!string.IsNullOrEmpty(Context.TransformStr(IsNotEmpty, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // Zero / not zero
            if (!string.IsNullOrEmpty(IsZero))
            {
                r = (Utils.To<long>(Context.Transform(IsZero, Transform)) == 0);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            if (!string.IsNullOrEmpty(Is0))
            {
                r = (Utils.To<long>(Context.Transform(Is0, Transform)) == 0);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotZero))
            {
                var res = Utils.To<long>(Context.Transform(IsNotZero, Transform));
                r = (res != 0);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            if (!string.IsNullOrEmpty(IsNot0))
            {
                var res = Utils.To<long>(Context.Transform(IsNot0, Transform));
                r = (res != 0);
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            
            // File or directory existence
            if (!string.IsNullOrEmpty(Exists))
            {
                r = ((File.Exists(Context.TransformStr(Exists, Transform)) || Directory.Exists(Context.TransformStr(Exists, Transform))));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(NotExists))
            {
                r = (!(File.Exists(Context.TransformStr(NotExists, Transform)) || Directory.Exists(Context.TransformStr(NotExists, Transform))));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            if (!string.IsNullOrEmpty(DoesNotExist))
            {
                r = (!(File.Exists(Context.TransformStr(DoesNotExist, Transform)) || Directory.Exists(Context.TransformStr(DoesNotExist, Transform))));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // Directory
            if (!string.IsNullOrEmpty(IsDirectory))
            {
                r = (Directory.Exists(Context.TransformStr(IsDirectory, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotDirectory))
            {
                r = (!Directory.Exists(Context.TransformStr(IsNotDirectory, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(DirectoryExists))
            {
                r = (Directory.Exists(Context.TransformStr(DirectoryExists, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(DirectoryNotExists))
            {
                r = (!Directory.Exists(Context.TransformStr(DirectoryNotExists, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }
            if (!string.IsNullOrEmpty(DirectoryDoesNotExist))
            {
                r = (!Directory.Exists(Context.TransformStr(DirectoryDoesNotExist, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            // File
            if (!string.IsNullOrEmpty(IsFile))
            {
                r = (File.Exists(Context.TransformStr(IsFile, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(IsNotFile))
            {
                r = (!File.Exists(Context.TransformStr(IsNotFile, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(FileExists))
            {
                r = (File.Exists(Context.TransformStr(FileExists, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }

            if (!string.IsNullOrEmpty(FileNotExists))
            {
                r = (!File.Exists(Context.TransformStr(FileNotExists, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }


            if (!string.IsNullOrEmpty(FileDoesNotExist))
            {
                r = (!File.Exists(Context.TransformStr(FileDoesNotExist, Transform)));
                if (!r.Value && All) return false;
                if (r.Value && !All) return true;
            }


            

            if (r==null)
                return true;
            return r.Value;
        }

    }

    
    /// <summary>
    /// Execute block if ANY or ALL of the conditions are true
    /// </summary>
    [XsType("if", ScriptActionBase.XSharperNamespace)]
    [Description("Execute block if ANY or ALL of the conditions are true")]
    public class If : Conditional
    {
        /// Block to execute if condition is false
        [XsElement("else",SkipIfEmpty = true, Ordering = 4)]
        [Description("Block to execute if condition is false")]
        public Block Else { get; set; }

        /// Default constructor
        public If()
        {
        }
        /// Block constructor
        public If(params IScriptAction[] data) : base(data)
        {
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, System.Xml.XmlReader reader, System.Reflection.PropertyInfo setToProperty)
        {
            if (string.Compare(reader.LocalName, "else", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (Else!=null)
                    throw new ParsingException("Only one else block is allowed per if statement!");

                // To allow special <if> <else /> </if> syntax
                if (reader.IsEmptyElement)
                {
                    Else = new Block();
                    reader.Skip();
                    return;
                }
            }

            base.ReadChildElement(context, reader, setToProperty);
        }

        /// <summary>
        /// Set property of the current object to newObject
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <param name="newObject">New property value</param>
        /// <param name="setToProperty">If not null, <paramref name="newObject"/> must be assigned to this property</param>
        /// <param name="collProperty">If not null, <paramref name="newObject"/> must be added to this IList-derived collection property</param>
        protected override void SetChildObject(System.Xml.XmlReader reader, object newObject, System.Reflection.PropertyInfo setToProperty, System.Reflection.PropertyInfo collProperty)
        {
            if (Else != null)
                Else.SetChildObjectAccessor(reader, newObject, setToProperty, collProperty);
            else
                base.SetChildObject(reader, newObject, setToProperty, collProperty);
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Context.Initialize(Else);
        }

        /// Execute action
        public override object Execute()
        {
            if (ShouldRun())
                return base.Execute();
            return Context.Execute(Else);
        }

        /// Add an action to Else block
        public void AddElse(IScriptAction action)
        {
            if (Else == null)
                Else = new Block();
            Else.Add(action);
        }
    }
}