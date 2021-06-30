using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SME.VHDL
{
    /// <summary>
    /// Helper class containing static methods for VHDL rendering.
    /// </summary>
    public static class VHDLHelper
    {
        /// <summary>
        /// Regular expression for checking if string is valid VHDL.
        /// </summary>
        public static readonly System.Text.RegularExpressions.Regex VHDLRE = new System.Text.RegularExpressions.Regex(@"(?<basetype>UNSIGNED|SIGNED|STD_LOGIC_VECTOR)\s*\((?<upper>\d+)\s+(to|downto)\s+(?<lower>\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Computes the width of a given type.
        /// </summary>
        /// <returns>The bit width of the type.</returns>
        /// <param name="t">The data type to get the width from.</param>
        public static int GetBitWidthFromType(Type t)
        {
            var customtype = t.GetCustomAttributes(typeof(VHDLTypeAttribute), true).OfType<VHDLTypeAttribute>().FirstOrDefault();
            if (customtype != null)
            {
                var m = VHDLRE.Match(customtype.Type);
                if (!m.Success)
                    m = VHDLRE.Match(customtype.Alias);

                if (m.Success)
                {
                    return
                        Math.Max(int.Parse(m.Groups["upper"].Value), int.Parse(m.Groups["lower"].Value))
                            -
                        Math.Min(int.Parse(m.Groups["upper"].Value), int.Parse(m.Groups["lower"].Value))
                            +
                        1;
                }
            }

            if (typeof(sbyte).IsAssignableFrom(t) || typeof(byte).IsAssignableFrom(t))
                return 8;
            else if (typeof(short).IsAssignableFrom(t) || typeof(ushort).IsAssignableFrom(t))
                return 16;
            else if (typeof(int).IsAssignableFrom(t) || typeof(uint).IsAssignableFrom(t) || typeof(float).IsAssignableFrom(t))
                return 32;
            else if (typeof(long).IsAssignableFrom(t) || typeof(ulong).IsAssignableFrom(t) || typeof(double).IsAssignableFrom(t))
                return 64;

            throw new Exception($"The type {t.FullName} cannot be decoded for size, please use either a built-in type with a known bitwidth, e.g. {typeof(UInt3).FullName} or supply the {typeof(VHDLTypeAttribute).FullName} on the type you are using");
        }

        /// <summary>
        /// Examines a type and returns the signedness of the type.
        /// </summary>
        /// <returns><c>true</c>, if the type is signed, <c>false</c> if it is unsigned and throws an exception if the signedness could not be determined.</returns>
        /// <param name="t">The type to examine.</param>
        public static bool GetIsSignedFromType(Type t)
        {
            var customtype = t.GetCustomAttributes(typeof(VHDLTypeAttribute), true).OfType<VHDLTypeAttribute>().FirstOrDefault();
            if (customtype != null)
            {
                var m = VHDLRE.Match(customtype.Type);
                if (!m.Success)
                    m = VHDLRE.Match(customtype.Alias);

                if (m.Success)
                {
                    if (string.Equals(m.Groups["basetype"].Value, "SIGNED", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (string.Equals(m.Groups["basetype"].Value, "UNSIGNED", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (typeof(sbyte).IsAssignableFrom(t) || typeof(short).IsAssignableFrom(t) || typeof(int).IsAssignableFrom(t) || typeof(long).IsAssignableFrom(t))
                return true;
            if (typeof(byte).IsAssignableFrom(t) || typeof(ushort).IsAssignableFrom(t) || typeof(uint).IsAssignableFrom(t) || typeof(ulong).IsAssignableFrom(t))
                return false;

            throw new Exception($"The type {t.FullName} cannot be decoded for signess, please use either a built-in type with a known type, e.g. {typeof(UInt3).FullName} or supply the {typeof(VHDLTypeAttribute).FullName} on the type you are using and specify the SIGNED or UNSIGNED type");
        }

        /// <summary>
        /// Splits a string over lines, and adds a number of indentation spaces on each line.
        /// </summary>
        /// <returns>The indented template string.</returns>
        /// <param name="str">The template string to indent.</param>
        /// <param name="indentation">The number of indentation spaces to add.</param>
        public static string ReIndentTemplate(string str, int indentation)
        {
            var ind = new string(' ', indentation);
            return
                ind
                + string.Join(
                    Environment.NewLine + ind,

                    (str ?? string.Empty)
                    .Trim()
                    .Replace("\t", new string(' ', 4))
                    .Replace("\r", "")
                    .Split(new string[] { "\n" }, StringSplitOptions.None)
                )
                + Environment.NewLine;
        }

        /// <summary>
        /// Converts the entire input array into a bitstring.
        /// </summary>
        /// <returns>The data bit string.</returns>
        /// <param name="data">The data to convert.</param>
        /// <param name="start">The offset into the array.</param>
        /// <param name="length">The number of elements to extract.</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static IEnumerable<string> GetDataBitStrings(Array data, int start = 0, int length = -1)
        {
            var elsize = GetBitWidthFromType(data.GetType().GetElementType());
            var sb = new System.Text.StringBuilder();
            if (length < 0)
                length = data.Length;

            for (var i = start; i < length + start; i++)
            {
                var el = data.GetValue(i).ToString();
                string td;
                if (el[0] == '-')
                    td = Convert.ToString(long.Parse(data.GetValue(i).ToString()), 2).PadLeft(64, '0');
                else
                    td = Convert.ToString((long)ulong.Parse(data.GetValue(i).ToString()), 2).PadLeft(64, '0');
                td = td.Substring(td.Length - elsize);
                yield return td;
            }

        }

        /// <summary>
        /// Converts the entire input array into a bitstring.
        /// </summary>
        /// <returns>The data bit string.</returns>
        /// <param name="data">The data to convert.</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static IEnumerable<string> GetDataBitStrings<TData>(TData[] data)
        {
            data = data ?? new TData[0];
            var elsize = GetBitWidthFromType(typeof(TData));
            var sb = new System.Text.StringBuilder();

            foreach (var t in data)
            {
                var td = Convert.ToString((long)ulong.Parse(t.ToString()), 2).PadLeft(64, '0');
                td = td.Substring(td.Length - elsize);
                yield return td;
            }

        }

        /// <summary>
        /// Converts the entire input array into a bitstring.
        /// </summary>
        /// <returns>The data bit string.</returns>
        /// <param name="datatype">The data type.</param>
        /// <param name="data">The data to convert.</param>
        /// <param name="paddedsize">The number of bits in the result string.</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static string GetDataBitString(Type datatype, object data, int paddedsize = 0)
        {
            var elsize = GetBitWidthFromType(datatype);
            var sb = new System.Text.StringBuilder();

            var td = Convert.ToString((long)ulong.Parse(data.ToString()), 2).PadLeft(64, '0');
            td = td.Substring(td.Length - elsize);
            sb.Append(td);

            sb.Append(new string('0', Math.Max(paddedsize - sb.Length, 0)));
            return sb.ToString();
        }

        /// <summary>
        /// Converts the entire input array into a bitstring.
        /// </summary>
        /// <returns>The data bit string.</returns>
        /// <param name="data">The data to convert.</param>
        /// <param name="paddedsize">The number of bits in the result string.</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static string GetDataBitString<TData>(TData data, int paddedsize = 0)
        {
            return GetDataBitString(typeof(TData), data, paddedsize);
        }

        /// <summary>
        /// Converts the given object into either a bit or hex string, depending
        /// on the bit width of the datatype.
        /// </summary>
        /// <returns>The bit or hex string.</returns>
        /// <param name="datatype">The data type of the object.</param>
        /// <param name="data">The object to convert.</param>
        public static string GetDataBitOrHexString(Type datatype, object data)
        {
            var elsize = GetBitWidthFromType(datatype);
            if (elsize % 4 != 0)
                return GetDataBitString(datatype, data);
            var hexwidth = (elsize / 4);
            var width_string = $"0:x{hexwidth}";
            var format_string = $"x\"{{{width_string}}}\"";
            switch (data)
            {
                case double d: return string.Format(format_string, BitConverter.ToInt64(BitConverter.GetBytes(d)));
                case float f: return string.Format(format_string, BitConverter.ToInt32(BitConverter.GetBytes(f)));
                default: return string.Format(format_string, data);
            }
        }

        /// <summary>
        /// Splits a sequence of data bits into a memory initialization string fitted for Xilinx BRAM initialization macros.
        /// </summary>
        /// <returns>The data bit string to mem init.</returns>
        /// <param name="data">The bit string to use.</param>
        /// <param name="datasize">The size of each element.</param>
        /// <param name="integralsize">The internal block ram storage size.</param>
        /// <param name="paritybits">The number of parity bits to emit for each value.</param>
        /// <param name="linesize">The size of each output line.</param>
        public static Tuple<IEnumerable<string>, IEnumerable<string>> SplitDataBitStringToMemInit(IEnumerable<string> data, int datasize, int paritybits, int integralsize = 8, int linesize = 256)
        {
            var items = new List<string>();
            var databuf = new List<string>();
            var paritybuf = new List<string>();
            var elements = 0;

            // Split input data into data / parity
            foreach(var de in data)
            {
                elements++;
                var element = de;
                var sb = new List<string>();
                var pbuf = new List<string>();

                var pb = paritybits;

                while (element.Length > 0)
                {
                    var take = Math.Min(8, element.Length);
                    sb.Insert(0, element.Substring(element.Length - take, take));
                    element = element.Substring(0, element.Length - take);

                    if (pb > 0)
                    {
                        pbuf.Insert(0, element.Substring(element.Length - 1, 1));
                        element = element.Substring(0, element.Length - 1);
                        pb--;
                    }
                    else
                    {
                        pbuf.Insert(0, "0");
                    }
                }

                databuf.Add(string.Join(string.Empty, sb.Select(x => Convert.ToByte(x, 2).ToString("X2"))));
                paritybuf.Add(string.Join(string.Empty, pbuf));
            }

            var line = new List<string>();
            var lines = new List<string>();
            var chars = 0;

            foreach (var de in databuf)
            {
                line.Insert(0, de);
                chars += de.Length;

                if (chars * 4 >= linesize)
                {
                    lines.Add(string.Join(string.Empty, line));
                    line.Clear();
                    chars = 0;
                }
            }

            var pbits = string.Join(string.Empty, paritybuf);
            var pbsize = pbits.Length / elements;
            var plines = new List<string>();
            var pcount = 0;

            for (var i = 0; i < pbits.Length - 1; i += pbsize)
            {
                var pt = pbits.Substring(i, pbsize);

                line.Insert(0, pbits.Substring(i, pbsize));
                pcount += pbsize;
                if (pcount >= linesize || i == pbits.Length - pbsize)
                {
                    var fullpline = string.Join(string.Empty, line);
                    var pb = new StringBuilder();
                    for (var j = 0; j < fullpline.Length; j += 4)
                        pb.Append(Convert.ToByte(fullpline.Substring(j, 4), 2).ToString("X1"));

                    plines.Add(pb.ToString());
                    line.Clear();
                    pcount = 0;
                }
            }

            return new Tuple<IEnumerable<string>, IEnumerable<string>>(lines, plines);

        }

        /// <summary>
        /// Creates a specific type from an <see cref="ulong"/> value.
        /// </summary>
        /// <returns>The intger type.</returns>
        /// <param name="v">The value to initialize with.</param>
        /// <typeparam name="T">The return data type parameter.</typeparam>
        public static T CreateIntType<T>(ulong v)
        {
            var width = GetBitWidthFromType(typeof(T));
            var issigned = GetIsSignedFromType(typeof(T));
            object integral;
            if (width > 32)
                integral = issigned ? (object)(long)v : v;
            else if (width > 16)
                integral = issigned ? (object)(int)v : (uint)v;
            else if (width > 8)
                integral = issigned ? (object)(short)v : (ushort)v;
            else
                integral = issigned ? (object)(sbyte)v : (byte)v;


            if (new Type[] { typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(byte), typeof(sbyte) }.Contains(typeof(T)))
                return (T)integral;

            return (T)Activator.CreateInstance(typeof(T), integral);
        }

        /// <summary>
        /// Creates the default include region for a component.
        /// </summary>
        /// <returns>The component include region.</returns>
        /// <param name="config">The render configuration.</param>
        /// <param name="indentation">The template indentation level.</param>
        public static string CreateComponentInclude(RenderConfig config, int indentation)
        {
            string template;
            if (config.DEVICE_VENDOR == FPGAVendor.Xilinx)
            {
                template = $@"
library UNISIM;
use UNISIM.vcomponents.all;
library UNIMACRO;
use unimacro.Vcomponents.all;
";
            }
            else
            {
                template = string.Empty;
            }

            return ReIndentTemplate(template, indentation);
        }

        /// <summary>
        /// Returns all elements from a non-empty array as a VHDL initialization list.
        /// </summary>
        /// <returns>The array as assignment list.</returns>
        /// <param name="data">The array with initialization elements.</param>
        /// <param name="typecasttemplate">A template to use for casting the string representation to the desired type.</param>
        /// <param name="inverse">A flag used to inverse the element order.</param>
        public static string GetArrayAsAssignmentList(Array data, string typecasttemplate = "std_logic_vector(to_unsigned({0}, {1}))", bool inverse = false)
        {
            if (inverse)
            {
                var tmp = Array.CreateInstance(data.GetType().GetElementType(), data.Length);
                for (var i = 0; i < data.Length; i++)
                    tmp.SetValue(data.GetValue(i), data.Length - i - 1);
                data = tmp;
            }

            var last = data.GetValue(data.Length - 1);
            var datawidth = GetBitWidthFromType(data.GetType().GetElementType());

            int first_trailing_element;
            for (first_trailing_element = data.Length - 2; first_trailing_element >= 0; first_trailing_element--)
                if (!data.GetValue(first_trailing_element).Equals(last))
                    break;

            first_trailing_element++;
            var sb = new StringBuilder();
            for (var i = 0; i < first_trailing_element; i++)
            {
                if (sb.Length != 0)
                    sb.Append(", \n");
                if (datawidth >= 32)
                    sb.Append(GetDataBitOrHexString(data.GetType().GetElementType(), data.GetValue(i)));
                else
                    sb.Append(string.Format(typecasttemplate, data.GetValue(i), datawidth));
            }

            if (first_trailing_element != data.Length)
            {
                if (sb.Length != 0)
                    sb.Append(", ");
                sb.Append("others => ");
                if (datawidth >= 32)
                    sb.Append(GetDataBitOrHexString(last.GetType(), last));
                else
                    sb.Append(string.Format(typecasttemplate, last, datawidth));
            }

            return "(" + sb + ")";
        }
    }
}
