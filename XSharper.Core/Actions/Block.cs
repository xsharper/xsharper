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
using System.ComponentModel;
using System.Threading;

namespace XSharper.Core
{
    /// <summary>
    /// Block of code followed by try/catch/finally blocks
    /// </summary>
    [XsType("block", ScriptActionBase.XSharperNamespace)]
    [Description("Block of code followed by try/catch/finally blocks")]
    public class Block : Sequence
    {
        /// Try block
        [XsElement("try", SkipIfEmpty = true, Ordering = 1, AtLeastOneChildForNotEmpty = false)]
        [Description("Try block")]
        public Block Try { get; set; }

        /// Catch block
        [XsElement("catch", SkipIfEmpty = true, Ordering = 2, AtLeastOneChildForNotEmpty = false)]
        [Description("Catch block, executed if exception is thrown")]
        public Block Catch { get; set; }

        /// Finally block
        [XsElement("finally", SkipIfEmpty = true, Ordering = 3, AtLeastOneChildForNotEmpty = false)]
        [Description("Finally block, whether or not the exception is thrown")]
        public Block Finally { get; set; }

        
        /// Default constructor
        public Block()
        {
        }

        /// Constructor that adds elements to the block (before try)
        public Block(params IScriptAction[] data) : base(data)
        {
        }

        /// Handle exception
        protected object OnError(Exception ex)
        {
            return OnError(ex, Catch);
        }

        /// Handle exception
        protected object OnError(Exception ex, Block catchBlock)
        {
            VerboseMessage("Exception: {0}.", Utils.TransformStr(ex.Message, TransformRules.TrimInternal));
            if (catchBlock == null)
                Utils.Rethrow(ex);
            Exception excSave = Context.CurrentException;
            try
            {
                
                ScriptExceptionWithStackTrace cs = ex as ScriptExceptionWithStackTrace;
                Context.CurrentException = (cs == null) ? ex : cs.InnerException;
                if (catchBlock != null && catchBlock.Items.Count == 0)
                    VerboseMessage("Supressing exception");
                else
                {
                    
                    return Context.Execute(catchBlock);
                }
            }
            finally
            {
                Context.CurrentException = excSave;
                if (ex is ScriptTerminateException)
                    Utils.Rethrow(ex);
            }
            return null;
        }

        /// Execute the list of actions before try
        protected object SequenceExecute()
        {
            return base.Execute();
        }

        /// Execute action
        public override object Execute()
        {
            object ret=SequenceExecute();
            if (ret != null)
                return ret;
            try
            {
                ret=Context.Execute(Try);
            }
            catch (Exception ex)
            {
                ret = OnError(ex);
            }
            finally
            {
                try
                {
                    Context.CheckAbort();
                }
                finally
                {
                    // Execute final portion
                    Context.Execute(Finally);
                }
            }
            return ret;
        }

        
        /// True if catch may appear w/o preceding try (internal thing)
        protected virtual bool AllowMissingTry { get { return false; } }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            if (Try == null && (Catch != null || Finally != null) && !AllowMissingTry )
                throw new ParsingException("try block is missing");
            if (Try != null && (Catch == null && Finally == null))
                throw new ParsingException("catch or finally blocks must be present");
            Context.Initialize(Try);
            Context.Initialize(Catch);
            Context.Initialize(Finally);
        }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            return base.ForAllChildren(func,isFind) || func(Try) || func(Catch) || func(Finally);
        }
        
        /// <summary>
        /// Add action to Try block
        /// </summary>
        /// <param name="action">Action to add</param>
        public void AddTry(IScriptAction action)
        {
            if (Try == null)
                Try = new Block();
            Try.Add(action);
        }

        /// <summary>
        /// Add action to Catch block
        /// </summary>
        /// <param name="action">Action to add</param>
        public void AddCatch(IScriptAction action)
        {
            if (Catch== null)
                Catch = new Block();
            Catch.Add(action);
        }

        /// <summary>
        /// Add action to Finally block
        /// </summary>
        /// <param name="action">Action to add</param>
        public void AddFinally(IScriptAction action)
        {
            if (Finally== null)
                Finally = new Block();
            Finally.Add(action);
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, System.Xml.XmlReader reader, System.Reflection.PropertyInfo setToProperty)
        {
            int itemsBefore = Items.Count;
            
            base.ReadChildElement(context, reader, setToProperty);
            
            if (Items.Count!=itemsBefore && (Try != null || Catch != null || Finally != null))
                throw new XsException(reader, "actions after try block are NOT allowed!");
        }
    }
}