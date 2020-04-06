using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SME.VHDL.Templates
{
    public abstract class BaseTemplate
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
                return session;
            }
            set
            {
                session = value;
            }
        }

        public StringBuilder GenerationEnvironment
        {
            get
            {
                if ((builder == null))
                {
                    builder = new StringBuilder();
                }
                return builder;
            }
            set
            {
                builder = value;
            }
        }

        protected CompilerErrorCollection Errors
        {
            get
            {
                if ((errors == null))
                {
                    errors = new CompilerErrorCollection();
                }
                return errors;
            }
        }

        public string CurrentIndent
        {
            get
            {
                return currentIndent;
            }
        }

        private Stack<int> Indents
        {
            get
            {
                if ((indents == null))
                {
                    indents = new Stack<int>();
                }
                return indents;
            }
        }

        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return _toStringHelper;
            }
        }

        public void Error(string message)
        {
            Errors.Add(new CompilerError(null, -1, -1, null, message));
        }

        public void Warning(string message)
        {
            CompilerError val = new CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            Errors.Add(val);
        }

        public string PopIndent()
        {
            if ((Indents.Count == 0))
            {
                return string.Empty;
            }
            int lastPos = (currentIndent.Length - Indents.Pop());
            string last = currentIndent.Substring(lastPos);
            currentIndent = currentIndent.Substring(0, lastPos);
            return last;
        }

        public void PushIndent(string indent)
        {
            Indents.Push(indent.Length);
            currentIndent = (currentIndent + indent);
        }

        public void ClearIndent()
        {
            currentIndent = string.Empty;
            Indents.Clear();
        }

        public void Write(string textToAppend)
        {
            GenerationEnvironment.Append(textToAppend);
        }

        public void Write(string format, params object[] args)
        {
            GenerationEnvironment.AppendFormat(format, args);
        }

        public void WriteLine(string textToAppend)
        {
            GenerationEnvironment.Append(currentIndent);
            GenerationEnvironment.AppendLine(textToAppend);
        }

        public void WriteLine(string format, params object[] args)
        {
            GenerationEnvironment.Append(currentIndent);
            GenerationEnvironment.AppendFormat(format, args);
            GenerationEnvironment.AppendLine();
        }

        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProvider = System.Globalization.CultureInfo.InvariantCulture;

            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return formatProvider;
                }
                set
                {
                    if ((value != null))
                    {
                        formatProvider = value;
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
                    return ((IConvertible)(objectToConvert)).ToString(formatProvider);
                }
                System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new Type[] {
                            iConvertibleType});
                if ((methInfo != null))
                {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}