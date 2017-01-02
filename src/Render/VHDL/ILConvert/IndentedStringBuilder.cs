using System;
using System.Text;
using System.Collections.Generic;

namespace SME.Render.VHDL.ILConvert
{
	internal class IndentedStringBuilder
	{
		private int m_indent;
		private List<string> m_sb = new List<string>();
		private StringBuilder m_line = new StringBuilder();
		private List<string> m_postline = new List<string>();

		public IndentedStringBuilder(int indent) 
		{
			m_indent = indent;
		}

		public void AppendLine() 
		{
			m_sb.Add(m_line.ToString()); 
			m_line.Length = 0;
			m_sb.AddRange(m_postline);
			m_postline.Clear();
		}

		public void Flush()
		{
			if (m_line.Length > 0)
			{
				m_sb.Add(m_line.ToString());
				m_line.Length = 0;
			}
			m_sb.AddRange(m_postline);
			m_postline.Clear();
		}

		private void Indent() { m_line.Append(' ', Math.Max(0, Indentation) + m_indent); }
		public void Append(string txt) { Indent(); m_line.Append(txt); }
		public void AppendLine(string txt) { Append(txt); AppendLine(); }
		public void AppendFormat(string format, object arg1) { Indent(); m_line.AppendFormat(format, arg1); }
		public void AppendFormat(string format, object arg1, object arg2) { Indent(); m_line.AppendFormat(format, arg1, arg2); }
		public void AppendFormat(string format, object arg1, object arg2, object arg3) { Indent(); m_line.AppendFormat(format, arg1, arg2, arg3); }
		public void AppendFormat(string format, object arg1, object arg2, object arg3, object arg4) { Indent(); m_line.AppendFormat(format, arg1, arg2, arg3, arg4); }
		public void AppendFormat(string format, params object[] args) { Indent(); m_line.AppendFormat(format, args); }

		public void PrependLine(string format, params object[] args) 
		{
			m_sb.Add(new string(' ', Math.Max(0, Indentation) + m_indent) + string.Format(format, args));
		}

		public void PostpendLine(string format, params object[] args)
		{
			m_postline.Add(new string(' ', Math.Max(0, Indentation) + m_indent) + string.Format(format, args));
		}

		public int Indentation { get; set; }

		public override string ToString()
		{
			Flush();
			return string.Join(Environment.NewLine, m_sb);
		}

		public IEnumerable<string> Lines
		{
			get
			{
				Flush();
				return m_sb;
			}
		}
	}
}

