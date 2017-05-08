using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SME.Tracer
{
	public class JsonTracer : Tracer
	{
		private string m_filename;
		private JsonWriter m_writer;
		private StreamWriter m_stream;

		public JsonTracer(string filename = null, string targetfolder = null)
		{
			m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".json"));

			while (filename == null && File.Exists(m_filename))
			{
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
				m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".json"));
			}

			m_stream = new StreamWriter(m_filename, false, System.Text.Encoding.UTF8);
			m_writer = new JsonTextWriter(m_stream);

			m_writer.Formatting = Newtonsoft.Json.Formatting.Indented;

			m_writer.WriteStartObject();
		}

		protected override void OutputSignalNames(SignalEntry[] signals)
		{
			m_writer.WritePropertyName("signals");
			m_writer.WriteStartArray();

			foreach (var p in signals)
			{
				m_writer.WriteStartObject();

				m_writer.WritePropertyName("name");

				if (p.IsInternal)
					m_writer.WriteValue(p.Property.DeclaringType.DeclaringType.Name + "." + p.Property.DeclaringType.Name + "." + p.Property.Name);
				else
					m_writer.WriteValue(p.Property.DeclaringType.Name + "." + p.Property.Name);

				m_writer.WritePropertyName("driver");
				m_writer.WriteValue(p.IsDriver);

				m_writer.WriteEndObject();
			}
	
			m_writer.WriteEndArray();

			m_writer.WritePropertyName("values");
			m_writer.WriteStartArray();
		}

		protected override void OuputSignalData(IEnumerable<Tuple<SignalEntry, object>> values)
		{
			m_writer.WriteStartArray();

			foreach (var v in values)
				if (v.Item2 is SME.ReadViolationException)
					m_writer.WriteValue('U');
				else if (v.Item2 != null && v.Item2.GetType().IsConstructedGenericType)
				{
					m_writer.WriteValue("todo!");
				}
				else
					m_writer.WriteValue(v.Item2);

			m_writer.WriteEndArray();
		}


		protected override void Dispose(bool disposing)
		{
			if (m_writer != null)
			{
				try { m_writer.WriteEndArray(); }
				catch { }

				try { m_writer.WriteEndObject(); }
				catch { }

				m_writer.Flush();
				m_writer.Close();
				m_writer = null;
			}

			if (m_stream != null)
			{
				m_stream.Dispose();
				m_stream = null;
			}
		}
	}}
