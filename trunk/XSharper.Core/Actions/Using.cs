using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace XSharper.Core.Actions
{
    /// <summary>
    /// Dispose of the object after block
    /// </summary>
    [XsType("using", ScriptActionBase.XSharperNamespace)]
    [Description("Dispose of the object after executing a block")]
    public class Using : Block
    {
        /// Variable name
        public string Name { get; set; }

        /// Value to set
        [XsAttribute("value")]
        public object Value
        {
            get;
            set;
        }

        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (!base.ProcessAttribute(context, attribute, value, previouslyProcessed))
            {
                if (Name != null)
                    throw new ParsingException("Only a single variable may be set by using action.");
                Name = attribute;
                Value = value;
                if (previouslyProcessed != null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute, true);
            }
            return true;
        }

        /// Execute action
        public override object Execute()
        {
            string name = Context.TransformStr(Name, Transform);
            object v = null;
            try
            {
                v = Context.Transform(Value, Transform);
                if (name != null)
                    Context[name] = v;
                return base.Execute();
            }
            finally
            {
                if (v != null && v is IDisposable)
                    ((IDisposable)v).Dispose();
                Context.Remove(name);
            }
        }
    }
}
