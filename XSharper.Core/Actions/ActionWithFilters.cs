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
using System.IO;

namespace XSharper.Core
{
    /// <summary>
    /// This is a special block which is executed in callback operations
    /// </summary>
    public abstract class CallbackBlock : Block
    {
        /// Variable prefix
        [Description("Variable prefix")]
        public string Name { get; set; }

        /// Execute base.execute() method
        protected object blockExecute() 
        {
            return base.Execute();
        }

        /// Gets a value indicating whether catch block may exist without try.
        protected override bool AllowMissingTry { get { return true; } }

        /// Delegate that is called before the main process
        protected delegate object PrepareFunc();

        /// Process delegate that does an action-specific thing
        protected delegate object ProcessFunc(bool skip);
        
        /// Prepare to run
        protected object ProcessPrepare(IFileSystemInfo from, IFileSystemInfo to, PrepareFunc func)
        {

            Context.CheckAbort();
            string pref = Context.TransformStr(Name, Transform);
            Vars sv = new Vars();
            sv[""] = sv["from"] = from; 
            if (to != null)
                sv["to"] = to;

            return Context.ExecuteWithVars(delegate()
                {
                    object ret = func();
                    return ret;
                },sv,pref);
        }

        /// Complete execution
        protected object ProcessComplete(IFileSystemInfo from, IFileSystemInfo to, bool skip, ProcessFunc func)
        {
            string pref = Context.TransformStr(Name, Transform);
            Vars sv=new Vars();
            sv[""]=sv["from"] = from;
            if (to != null)
                sv["to"] = to;
            sv["skip"] = skip;
        
            return Context.ExecuteWithVars(delegate()
                                               {
                                                   Context.CheckAbort();
                                                   object ret = null;
                                                   try
                                                   {
                                                       ret = SequenceExecute();
                                                   }
                                                   catch (Exception ex)
                                                   {
                                                       return ReturnValue.CreateException(ex);
                                                   }
                                                   try
                                                   {
                                                       if (ret == null)
                                                            ret = Context.Execute(Try);
                                                       skip=Context.GetBool(pref+"skip", false);
                                                       if (ret == null)
                                                           ret = func(skip);
                                                   }
                                                   catch (Exception ex)
                                                   {
                                                       ret = OnError(ex);
                                                   }
                                                   finally
                                                   {
                                                       // Execute final portion
                                                       Context.Execute(Finally);
                                                   }
                                                   return ret;
                                               }, sv, pref);
            
        }
    }

    /// <summary>
    /// Base abstract class for actions that deal with files, and have separate filters for directories and files
    /// </summary>
    public abstract class ActionWithFilters : CallbackBlock
    {
        /// Directory filter. Filter format is specified in <see cref="Syntax"/>
        [Description("Directory filter")]
        public string DirectoryFilter { get; set; }

        /// File filter. Filter format is specified in <see cref="Syntax"/>
        [Description("File filter")]
        public string Filter { get; set; }

        /// Syntax format of the filters
        [Description("Syntax format of the filters")]
        public FilterSyntax Syntax { get; set; }

        /// Include hidden directories and files
        [Description("Include hidden directories and files")]
        public bool Hidden { get; set; }
        

        /// Constructor
        protected ActionWithFilters()
        {
            Syntax = FilterSyntax.Auto;
        }

        /// Check if the provided object matches the current filter for hidden files
        protected bool CheckHidden(FileSystemInfo fsi)
        {
            if (Hidden)
                return true;
            
            return (fsi.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0;
        }
    }

    
}