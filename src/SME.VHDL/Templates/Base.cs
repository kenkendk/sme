using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SME.VHDL.Templates
{
    public class BaseTemplate
    {
        public virtual string TransformText()
        {
            GenerationEnvironment = null;
            Write("Template not implemented");
            return GenerationEnvironment.ToString();
        }

        public virtual void Initialize()
        {
        }

        private StringBuilder builder;
        private IDictionary<string, object> session;
        private CompilerErrorCollection errors;
        private string currentIndent = string.Empty;
        private Stack<int> indents;
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();

        public virtual IDictionary<string, object> Session
        {
            get
            {
                return this.session;
            }
            set
            {
                this.session = value;
            }
        }

        public StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.builder == null))
                {
                    this.builder = new StringBuilder();
                }
                return this.builder;
            }
            set
            {
                this.builder = value;
            }
        }

        protected CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errors == null))
                {
                    this.errors = new CompilerErrorCollection();
                }
                return this.errors;
            }
        }

        public string CurrentIndent
        {
            get
            {
                return this.currentIndent;
            }
        }

        private Stack<int> Indents
        {
            get
            {
                if ((this.indents == null))
                {
                    this.indents = new Stack<int>();
                }
                return this.indents;
            }
        }

        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this._toStringHelper;
            }
        }

        public void Error(string message)
        {
            this.Errors.Add(new CompilerError(null, -1, -1, null, message));
        }

        public void Warning(string message)
        {
            CompilerError val = new CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }

        public string PopIndent()
        {
            if ((this.Indents.Count == 0))
            {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }

        public void PushIndent(string indent)
        {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }

        public void ClearIndent()
        {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }

        public void Write(string textToAppend)
        {
            this.GenerationEnvironment.Append(textToAppend);
        }

        public void Write(string format, params object[] args)
        {
            this.GenerationEnvironment.AppendFormat(format, args);
        }

        public void WriteLine(string textToAppend)
        {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }

        public void WriteLine(string format, params object[] args)
        {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }

        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProvider = System.Globalization.CultureInfo.InvariantCulture;

            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProvider;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProvider = value;
                    }
                }
            }

            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new ArgumentNullException("objectToConvert");
                }
                Type type = objectToConvert.GetType();
                Type iConvertibleType = typeof(IConvertible);
                if (iConvertibleType.IsAssignableFrom(type))
                {
                    return ((IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new Type[] {
                            iConvertibleType});
                if ((methInfo != null))
                {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}