using System;
using System.Collections.Generic;
using System.Linq;

namespace SME.VHDL
{
    public static class VHDLHelper
    {
        public static readonly System.Text.RegularExpressions.Regex VHDLRE = new System.Text.RegularExpressions.Regex(@"(?<basetype>UNSIGNED|SIGNED|STD_LOGIC_VECTOR)\s*\((?<upper>\d+)\s+(to|downto)\s+(?<lower>\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// Computes the width of a given type
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
            else if (typeof(int).IsAssignableFrom(t) || typeof(uint).IsAssignableFrom(t))
                return 32;
            else if (typeof(long).IsAssignableFrom(t) || typeof(ulong).IsAssignableFrom(t))
                return 64;

            throw new Exception($"The type {t.FullName} cannot be decoded for size, please use either a built-in type with a known bitwidth, e.g. {typeof(UInt3).FullName} or supply the {typeof(VHDLTypeAttribute).FullName} on the type you are using");
        }

        /// <summary>
        /// Examines a type and returns the signedness of the type
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
        /// Splits a string over lines, and adds a number of indentation spaces on each line
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
        /// Converts the entire input array into a bitstring
        /// </summary>
        /// <returns>The data bit string.</returns>
        /// <param name="data">The data to convert.</param>
        /// <param name="paddedsize">The number of bits in the result string</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static string GetDataBitString<TData>(TData[] data, int paddedsize = 0)
        {
            data = data ?? new TData[0];
            var elsize = GetBitWidthFromType(typeof(TData));
            var sb = new System.Text.StringBuilder();

            foreach (var t in data)
            {
                var td = Convert.ToString((long)ulong.Parse(t.ToString()), 2).PadLeft(64, '0');
                td = td.Substring(td.Length - elsize);
                sb.Append(td);
            }

            sb.Append(new string('0', Math.Max(paddedsize - sb.Length, 0)));
            return sb.ToString();
        }

        /// <summary>
        /// Splits a string of data bits into a memory initialization string fitted for Xilinx BRAM initialization macros
        /// </summary>
        /// <returns>The data bit string to mem init.</returns>
        /// <param name="data">The bit string to use.</param>
        /// <param name="datasize">The size of each element.</param>
        /// <param name="integralsize">The internal block ram storage size.</param>
        /// <param name="paritybits">The number of parity bits to emit for each value</param>
        /// <param name="linesize">The size of each output line</param>
        public static Tuple<IEnumerable<string>, IEnumerable<string>> SplitDataBitStringToMemInit(string data, int datasize, int paritybits, int integralsize = 8, int linesize = 256)
        {
            var datalines = new List<string>();
            var paritylines = new List<string>();

            var databuffer = new System.Text.StringBuilder();
            var paritybuffer = new System.Text.StringBuilder();

            var hexchars = ((datasize - paritybits) + 3) / 4;

            var ix = 0;
            while (ix < data.Length)
            {
                var bits = data.Substring(ix, datasize);
                ix += datasize;

                var pb = paritybits;
                while (bits.Length > 0)
                {
                    var real = Math.Min(bits.Length, integralsize);
                    databuffer.Append(bits.Substring(bits.Length - real, real));
                    bits = bits.Substring(0, bits.Length - real);
                    if (pb > 0)
                    {
                        paritybuffer.Append(bits.Substring(bits.Length - 1, 1));
                        pb--;
                        bits = bits.Substring(0, bits.Length - 1);
                    }

                    while (databuffer.Length >= linesize)
                    {
                        var bitline = databuffer.ToString().Substring(0, linesize);
                        databuffer = new System.Text.StringBuilder(databuffer.ToString().Substring(linesize));
                        datalines.Add(ReverseBitsToHexLine(bitline, datasize - paritybits));
                    }
                    while (paritybuffer.Length >= linesize)
                    {
                        var bitline = paritybuffer.ToString().Substring(0, linesize);
                        paritybuffer = new System.Text.StringBuilder(paritybuffer.ToString().Substring(linesize));
                        paritylines.Add(ReverseBitsToHexLine(bitline, paritybits));
                    }
                }
            }

            return new Tuple<IEnumerable<string>, IEnumerable<string>>(datalines, paritylines);
        }

        /// <summary>
        /// Reverts a line with bits into little-endian hex string, preserving big-endianness for the individual characters
        /// </summary>
        /// <returns>The bits to hex line.</returns>
        /// <param name="bits">Bits.</param>
        /// <param name="datawidth">Datawidth.</param>
        private static string ReverseBitsToHexLine(string bits, int datawidth)
        {
            var hexchars = (datawidth + 3) / 4;
            var w = new List<string>();

            var off = 0;
            while (off < bits.Length)
            {
                var h = new List<string>();
                for (var i = 0; i < hexchars; i++)
                    h.Add(Convert.ToByte(bits.Substring((i * 4) + off, 4), 2).ToString("X1"));
                w.Insert(0, string.Join(string.Empty, h));
                off += (hexchars * 4);
            }

            return string.Join(string.Empty, w);
        }

        /// <summary>
        /// Creates multiple data lines from the given input data
        /// </summary>
        /// <returns>The memory as a list of strings.</returns>
        /// <param name="data">The data to render, or null.</param>
        /// <param name="paddedsize">The size of the data, which will be right-padded with zeroes until it reaches this limit</param>
        /// <param name="bitlength">The number of bits in each emitted string, must be evenly divisible by radix.</param>
        /// <param name="radix">The output radix to use, can be 2, 8 or 16</param>
        /// <typeparam name="TData">The data type parameter.</typeparam>
        public static IEnumerable<string> GetMemoryAsString<TData>(TData[] data, long paddedsize = 0, int bitlength = 256, int radix = 16)
        {
            data = data ?? new TData[0];
            var elsize = GetBitWidthFromType(typeof(TData));
            var totalbits = Math.Max(paddedsize, elsize * data.Length);

            string radixchar;
            int radixbits;
            switch (radix)
            {
                case 16:
                    radixchar = "X";
                    radixbits = 4;
                    break;
                case 8:
                    radixchar = "O";
                    radixbits = 2;
                    break;
                case 2:
                    radixchar = "B";
                    radixbits = 1;
                    break;
                default:
                    throw new Exception($"Only {nameof(radix)} 16, 8 and 2 (hex, octal, binary) are supported");
            }

            if ((bitlength % radixbits) != 0)
                throw new Exception($"The given {nameof(bitlength)} ({bitlength}) is not evenly divisble by the {nameof(radix)} bit size ({radixbits})");
                

            var charsprline = bitlength / radixbits;

            var dataix = 0;
            var sb = new System.Text.StringBuilder();
            var bits = new System.Text.StringBuilder();

            while (totalbits > 0 && dataix < data.Length)
            {
                var td = Convert.ToString((long)Convert.ToUInt64(data[dataix]), 2);
                td = td.Substring(td.Length - elsize);
                bits.Append(td);
                dataix++;

                while (bits.Length >= radixbits)
                {
                    var tmp = bits.ToString();
                    sb.Append(Convert.ToString(Convert.ToByte(tmp.Substring(0, radixbits - 1), 2), radix));
                    bits.Clear();
                    bits.Append(tmp.Substring(radixbits));
                }

                while (sb.Length >= charsprline)
                {
                    yield return EmitLine(sb, radixchar, charsprline);
                    totalbits -= bitlength;
                }
            }

            if (bits.Length != 0)
            {
                bits.Append(new string('0', radixbits - bits.Length));
                sb.Append(Convert.ToString(Convert.ToByte(bits.ToString(), 2), radix));
                bits.Clear();
            }

            while (totalbits > 0)
            {
                sb.Append(new string('0', charsprline - sb.Length));

                while (sb.Length >= charsprline)
                    yield return EmitLine(sb, radixchar, charsprline);                
            }

            sb.Append("\"");

            yield return sb.ToString();

        }
        /// <summary>
        /// Helper method to emit a single data line from <see cref="GetMemoryAsString"/>
        /// </summary>
        /// <returns>The line to emit.</returns>
        /// <param name="sb">The string builder to use and update.</param>
        /// <param name="radixchar">The radix charater to emit.</param>
        /// <param name="charsprline">The number of characters to emit for each line.</param>
        private static string EmitLine(System.Text.StringBuilder sb, string radixchar, int charsprline)
        {
            var tmp = sb.ToString();
            var res = $"{radixchar}\"{tmp.Substring(0, charsprline - 1)}\"";

            sb.Clear();
            sb.Append(tmp.Substring(charsprline));

            return res;
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
        /// Creates the default include region for a component
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
    }
}
