using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SME.Tracer
{
    /// <summary>
    /// Class for tracing the network into a .csv file.
    /// </summary>
    public class CSVTracer : Tracer
    {
        /// <summary>
        /// The filename the trace will be written to.
        /// </summary>
        private string m_filename;
        /// <summary>
        /// Flag indicating whether this the current entry written is the first.
        /// </summary>
        private bool m_firstEntry = true;

        /// <summary>
        /// Constructs a new instance of the CSV tracer class, which will write the trace into the file at the given filename in the given folder.
        /// </summary>
        /// <param name="filename">The given filename.</param>
        /// <param name="targetfolder">The given folder.</param>
        public CSVTracer(string filename = null, string targetfolder = null)
        {
            m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv"));

            while (filename == null && File.Exists(m_filename))
            {
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
                m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv"));
            }

            using (File.Create(m_filename)) { }
        }

        private string SignalName(SignalEntry signal)
        {
            if (signal.Property.PropertyType.IsGenericType && signal.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
            {
                var attr = signal.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();
                var propname = BusSignalToName(signal);
                var expanded = Enumerable
                                    .Range(0, attr.Length)
                                    .Select(x => $"{propname}({x})");
                return string.Join(',', expanded);
            }
            else
                return BusSignalToName(signal);
        }

        /// <summary>
        /// Method used to emit the signal names as part of the very first cycle.
        /// </summary>
        /// <param name="signals">The signals to emit the names for.</param>
        protected override void OutputSignalNames(SignalEntry[] signals)
        {
            using (var af = File.AppendText(m_filename))
            {
                var names = signals.Select(x => SignalName(x));
                af.WriteLine(string.Join(',', names));
            }
        }

        /// <summary>
        /// Method used to output the value for each of the signals.
        /// </summary>
        /// <param name="values">The signal and value pairs.</param>
        /// <param name="last">If set to <c>true</c> the signals are the last set in the current cycle.</param>
        protected override void OutputSignalData(IEnumerable<Tuple<SignalEntry, object>> values, bool last)
        {
            using (var af = File.AppendText(m_filename))
            {
                foreach (var signal in values)
                {
                    if (m_firstEntry)
                        m_firstEntry = false;
                    else
                        af.Write(",");

                    var p = signal.Item1;
                    var value = signal.Item2;

                    if (p.Property.PropertyType.IsGenericType && p.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
                    {
                        var attr = p.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();

                        try
                        {
                            var eltype = p.Property.PropertyType.GetGenericArguments().First();
                            var m = value.GetType().GetProperties().FirstOrDefault(x => x.GetIndexParameters().Length == 1);
                            var fixedInteraction = (IFixedArrayInteraction)value;
                            foreach (var n in Enumerable.Range(0, attr.Length))
                            {
                                try
                                {
                                    if (!fixedInteraction.CanRead(n))
                                        af.Write("U");
                                    else
                                        af.Write(ConvertToString(m.GetValue(value, new object[] { n }), eltype));
                                }
                                catch (Exception ex)
                                {
                                    if (ex is TargetInvocationException)
                                        ex = ((TargetInvocationException)ex).InnerException;

                                    if (ex is ReadViolationException)
                                        af.Write("U");
                                    else
                                        af.Write("X");

                                }
                                if (n != attr.Length - 1)
                                    af.Write(",");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is TargetInvocationException)
                                ex = ((TargetInvocationException)ex).InnerException;

                            string c = "X";
                            if (ex is ReadViolationException)
                                c = "U";

                            foreach (var n in Enumerable.Range(0, attr.Length))
                            {
                                af.Write(c);
                                if (n != attr.Length - 1)
                                    af.Write(",");
                            }
                        }
                    }
                    else
                    {
                        af.Write(ConvertToString(value, p.Property.PropertyType));
                    }
                }

                if (last)
                {
                    af.WriteLine();
                    m_firstEntry = true;
                }
            }
        }

        /// <summary>
        /// Converts the given object into a string representation, based on the given type.
        /// <summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="itemtype">The type of the object.</param>
        protected virtual string ConvertToString(object value, Type itemtype)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is ReadViolationException)
                return "U";
            else if (value is Exception)
                return "X";
            else if (typeof(ITracerSerializable).IsAssignableFrom(itemtype))
                return ((ITracerSerializable)value).Serialize(this);
            else if (itemtype == typeof(bool))
                return ((bool)value) ? "1" : "0";
            else if (itemtype.IsEnum)
            {
                var v = value.ToString();
                if (!Enum.GetNames(value.GetType()).Any(x => x == v))
                    v = Enum.GetNames(value.GetType()).First();

                return ConvertToValidName(value.GetType().FullName + "." + v).ToLower();
            }
            else if (itemtype == typeof(byte))
                return Convert.ToString((byte)value, 2).PadLeft(8, '0');
            else if (itemtype == typeof(ushort))
                return Convert.ToString((ushort)value, 2).PadLeft(16, '0');
            else if (itemtype == typeof(uint))
                return Convert.ToString((uint)value, 2).PadLeft(32, '0');
            else if (itemtype == typeof(sbyte))
                return Convert.ToString((sbyte)value, 2).PadLeft(8, '0');
            else if (itemtype == typeof(short))
                return Convert.ToString((short)value, 2).PadLeft(16, '0');
            else if (itemtype == typeof(int))
                return Convert.ToString((int)value, 2).PadLeft(32, '0');
            else if (itemtype == typeof(long))
                return
                    Convert.ToString((int)(((long)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
                    Convert.ToString((int)((long)value & 0xffffffff), 2).PadLeft(32, '0')
                ;
            else if (itemtype == typeof(ulong))
                return
                    Convert.ToString((int)(((ulong)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
                    Convert.ToString((int)((ulong)value & 0xffffffff), 2).PadLeft(32, '0')
                ;
            else if (itemtype == typeof(float))
                return Convert.ToString(BitConverter.ToUInt32(BitConverter.GetBytes((float)value), 0), 2).PadLeft(32, '0');
            else if (itemtype == typeof(double))
            {
                long l = BitConverter.DoubleToInt64Bits((double)value);
                var lh = (int)((l >> 32) & 0xffffffff);
                var lr = (int)(l & 0xffffffff);
                return
                    Convert.ToString(lh, 2).PadLeft(32, '0') +
                    Convert.ToString(lr, 2).PadLeft(32, '0');
            }
            else
                return (value ?? string.Empty).ToString();
        }

        /// <summary>
        /// Converts the given string to a valid name.
        /// </summary>
        /// <param name="name">The string to convert.</param>
        public virtual string ConvertToValidName(string name)
        {
            return name;
        }

        /// <summary>
        /// Returns a string representation of the given signal.
        /// </summary>
        /// <param name="p">The given signal.</param>
        public virtual string BusSignalToName(SignalEntry p)
        {
            return p.SortKey.Replace(",", "_");
        }
    }
}
