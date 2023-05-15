using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Base template for VHDL files.
    /// </summary>
    public abstract class BaseTemplate
    {
        /// <summary>
        /// Writes the template to the VHDL file.
        /// </summary>
        public virtual string TransformText()
        {
            GenerationEnvironment = null;
            Write("Template not implemented");
            return GenerationEnvironment.ToString();
        }

        /// <summary>
        /// Initializes the template.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// The string builder used for writing the files.
        /// </summary>
        private StringBuilder builder;
        /// <summary>
        /// The current session.
        /// </summary>
        private IDictionary<string, object> session;
        /// <summary>
        /// The collection of errors.
        /// </summary>
        private CompilerErrorCollection errors;
        /// <summary>
        /// The current indentation.
        /// </summary>
        private string currentIndent = string.Empty;
        /// <summary>
        /// Stack of indentations.
        /// </summary>
        private Stack<int> indents;
        /// <summary>
        /// Helper class with static methods.
        /// </summary>
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();

        /// <summary>
        /// Gets or sets the current session.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current builder.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current collection of errors.
        /// </summary>
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

        /// <summary>
        /// Gets the current indentation.
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return currentIndent;
            }
        }

        /// <summary>
        /// Gets the current stack of indentations.
        /// </summary>
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

        /// <summary>
        /// Gets the string helper class.
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return _toStringHelper;
            }
        }

        /// <summary>
        /// Adds the given error to the collection of errors.
        /// </summary>
        /// <param name="message">The given error.</param>
        public void Error(string message)
        {
            Errors.Add(new CompilerError(null, -1, -1, null, message));
        }

        /// <summary>
        /// Adds the given warning to the collection of warnings.
        /// </summary>
        /// <param name="message">The given warning.</param>
        public void Warning(string message)
        {
            CompilerError val = new CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            Errors.Add(val);
        }

        /// <summary>
        /// Pops an indentation from the stack of indentations.
        /// </summary>
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

        /// <summary>
        /// Pushes the given indentation to the stack of indentations.
        /// </summary>
        /// <param name="indent">The given indentation.</param>
        public void PushIndent(string indent)
        {
            Indents.Push(indent.Length);
            currentIndent = (currentIndent + indent);
        }

        /// <summary>
        /// Clears the current indentation.
        /// </summary>
        public void ClearIndent()
        {
            currentIndent = string.Empty;
            Indents.Clear();
        }

        /// <summary>
        /// Writes the given text to the builder.
        /// </summary>
        /// <param name="textToAppend">The text to write.</param>
        public void Write(string textToAppend)
        {
            GenerationEnvironment.Append(textToAppend);
        }

        /// <summary>
        /// Applies the given objects to the format string and writes it to the builder.
        /// </summary>
        /// <param name="format">The given format string.</param>
        /// <param name="args">The given objects to apply to the format string.</param>
        public void Write(string format, params object[] args)
        {
            GenerationEnvironment.AppendFormat(format, args);
        }

        /// <summary>
        /// Writes the given text to the builder along with a newline.
        /// </summary>
        /// <param name="textToAppend">The text to write.</param>
        public void WriteLine(string textToAppend)
        {
            GenerationEnvironment.Append(currentIndent);
            GenerationEnvironment.AppendLine(textToAppend);
        }

        /// <summary>
        /// Applies the given objects to the format string and writes it to the builder along with a newline.
        /// </summary>
        /// <param name="format">The given format string.</param>
        /// <param name="args">The given objects to apply to the format string.</param>
        public void WriteLine(string format, params object[] args)
        {
            GenerationEnvironment.Append(currentIndent);
            GenerationEnvironment.AppendFormat(format, args);
            GenerationEnvironment.AppendLine();
        }

        /// <summary>
        /// Helper class containing static methods for manipulating strings.
        /// </summary>
        public class ToStringInstanceHelper
        {
            /// <summary>
            /// The format provider for formatting strings.
            /// </summary>
            private System.IFormatProvider formatProvider = System.Globalization.CultureInfo.InvariantCulture;

            /// <summary>
            /// Gets or sets the format provider.
            /// </summary>
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

            /// <summary>
            /// Returns a collection of indices of the given (potentially array of) bus(ses).
            /// </summary>
            /// <param name="bus">The given (potentially array of) bus(ses).</param>
            public IEnumerable<string> ToEnumeratedIndices(RenderState RS, AST.Bus bus)
            {
                if ((bus == null))
                {
                    throw new ArgumentNullException("bus");
                }

                if ((bus.SourceInstances.Length == 1))
                {
                    return new string[] {
                            string.Empty};
                }

                // Extract the offset
                var offset = int.Parse(Regex.Match(RS.TestBenchSignalName(bus.Signals.First()), "#([0-9]+)").Groups[1].Value);

                // Return the range TODO order by their string name, but return array index!
                return Enumerable.Range(0, bus.SourceInstances.Length).OrderBy(x => (x + offset).ToString()).Select(x => x.ToString());
            }

            /// <summary>
            /// Converts the given collection of busses to a collection of strings. If a bus is an array of busses, the strings are incremented with the index of the first bus. E.g. an array of buses `somebus#3` with two instances will return `["somebus#3", "somebus#4"]`.
            /// </summary>
            /// <param name="busses">The given collection of busses.</param>
            public IEnumerable<string> ToEnumeratedString(RenderState RS, IEnumerable<AST.Bus> busses)
            {
                if ((busses == null))
                {
                    throw new ArgumentNullException("busses");
                }

                List<string> result = new List<string>();

                foreach (var bus in busses)
                {
                    if (bus.SourceInstances.Length > 1)
                    {
                        var offset = int.Parse(Regex.Match(RS.TestBenchSignalName(bus.Signals.First()), "#([0-9]+)").Groups[1].Value);
                        var formats = bus.Signals
                            .OrderBy(x => x.Name)
                            .SelectMany(x => RS.SplitArray(x))
                            .Select(x => Regex.Replace( RS.TestBenchSignalName(x), "#[0-9]+", "#{0}" ));
                        var idxs = Enumerable
                            .Range(0, bus.SourceInstances.Length)
                            .OrderBy(x => (x + offset).ToString());
                        var signalnames = idxs
                            .SelectMany(x =>
                                formats.Select(y =>
                                    ToStringWithCulture(string.Format(y, x))));

                        result.AddRange(signalnames);
                    } else {
                        result.AddRange(bus.Signals
                            .OrderBy(x => x.Name)
                            .SelectMany(x => RS.SplitArray(x))
                            .Select(x => RS.TestBenchSignalName(x)));
                    }

                }

                return result;
            }

            /// <summary>
            /// Converts the given object to a string, along with culture given by the format provider.
            /// </summary>
            /// <param name="objectToConvert">The object to convert.</param>
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