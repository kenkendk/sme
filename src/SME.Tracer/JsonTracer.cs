using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SME.Tracer
{
    /// <summary>
    /// A tracer that outputs the traced data in JSON format.
    /// </summary>
    public class JsonTracer : Tracer
    {
        /// <summary>
        /// The name of the file where data is written.
        /// </summary>
        private string m_filename;
        /// <summary>
        /// The writer instance.
        /// </summary>
        private JsonWriter m_writer;
        /// <summary>
        /// The open stream.
        /// </summary>
        private StreamWriter m_stream;
        /// <summary>
        /// Flag that tracks the output state for the signal arrays.
        /// </summary>
        private bool m_startedArray = false;
        /// <summary>
        /// Flag handling the emission of variables.
        /// </summary>
        private bool m_emitVariables = true;
        /// <summary>
        /// Flag handling the emission of variable arrays.
        /// </summary>
        private bool m_emitVariableArrays = false;
        /// <summary>
        /// The dictionary with all variables from the processes.
        /// </summary>
        private Dictionary<IProcess, Dictionary<System.Reflection.FieldInfo, long>> m_variables = new Dictionary<IProcess, Dictionary<System.Reflection.FieldInfo, long>>();


        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Tracer.JsonTracer"/> class.
        /// </summary>
        /// <param name="filename">The name of the file to write to.</param>
        /// <param name="targetfolder">The folder where the file shold be written, if the filename is not an absolute path.</param>
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

        /// <summary>
        /// Method used to output the value for each of the signals.
        /// </summary>
        /// <param name="values">The signal and value pairs.</param>
        /// <param name="last">If set to <c>true</c> the signals are the last set in the current cycle.</param>
        protected override void OutputSignalNames(SignalEntry[] signals)
        {
            var simulation = Simulation.Current;
            var graph = simulation.Graph;

            var sr = JsonSerializer.Create();
            var id = 0L;

            var procmap = new Dictionary<IProcess, long>();
            foreach (var n in graph.ExecutionPlan.Select(x => x.Item))
                procmap[n] = id++;

            foreach (var n in graph.ExecutionPlan.Select(x => x.Item))
            {
                var fmap = new Dictionary<System.Reflection.FieldInfo, long>();
                var fields = n
                    .GetType()
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                    .Where(x => x.FieldType.IsPrimitive || (m_emitVariableArrays && x.FieldType.IsArray && x.FieldType.GetElementType().IsPrimitive));

                foreach (var f in fields)
                    fmap[f] = id++;
                m_variables[n] = fmap;
            }

            var busmap = new Dictionary<IRuntimeBus, long>();
            foreach (var n in graph.AllBusses)
                busmap[n] = id++;

            var signalmap = new Dictionary<SignalEntry, long>();
            foreach (var n in signals)
                signalmap[n] = id++;

            m_writer.WritePropertyName("config");
            sr.Serialize(m_writer, new JsonTypes.Network()
            {
                Processes = procmap.OrderBy(x => x.Value).Select(x => new SME.Tracer.JsonTypes.Process()
                {
                    ID = x.Value,
                    Name = x.Key.Name,
                    IsClocked = x.Key.IsClockedProcess,
                    SourceClass = x.Key.GetType().ToString(),
                    Variables = m_variables[x.Key]
                        .Select(y => new SME.Tracer.JsonTypes.Variable()
                        {
                            ID = y.Value,
                            IsSignal = false,
                            Name = y.Key.Name,
                            Type = y.Key.FieldType.ToString()
                        })
                        .ToArray(),
                    InputBusses = x.Key.InputBusses
                        .SelectMany(y => y)
                        .Union(x.Key.ClockedInputBusses.SelectMany(y => y))
                        .Select(y => busmap[y])
                        .ToArray(),
                    OutputBusses = x.Key.OutputBusses
                        .SelectMany(y => y)
                        .Select(y => busmap[y])
                        .ToArray(),
                    InternalBusses = x.Key.InternalBusses
                        .SelectMany(y => y)
                        .Select(y => busmap[y])
                        .ToArray()
                }).ToArray(),

                Busses = busmap.OrderBy(x => x.Value).Select(x => new SME.Tracer.JsonTypes.Bus()
                {
                    ID = x.Value,
                    Name = x.Key.BusType.ToString(),
                    IsClocked = x.Key.IsClocked,
                    IsInternal = x.Key.IsInternal,
                    SourceClass = x.Key.BusType.ToString(),
                    Signals = signals.Where(y => y.Bus == x.Key).Select(y => new SME.Tracer.JsonTypes.Signal()
                    {
                        ID = signalmap[y],
                        Name = y.Property.Name,
                        Type = y.Property.PropertyType.ToString(),
                        IsDriver = y.IsDriver,
                        IsInternal = y.IsInternal
                    }).ToArray()
                }).ToArray(),

                Tree = graph.ExecutionPlan.Select(x => new SME.Tracer.JsonTypes.ProcessTree()
                {
                    Self = procmap[x.Item],
                    Parents = x.Parents.Select(y => procmap[y.Item]).ToArray(),
                    Children = x.Parents.Select(y => procmap[y.Item]).ToArray()
                }).ToArray(),

                ValueMap = signals
                    .Select(x => signalmap[x])
                    .Concat(
                        !m_emitVariables
                        ? new long[0]
                        : m_variables.SelectMany(p => p.Value.Select(f => f.Value))
                    )
                    .ToArray(),

                IncludesVariables = m_emitVariables,
                IncludesVariableArrays = m_emitVariables && m_emitVariableArrays

            });


            m_writer.WritePropertyName("values");
            m_writer.WriteStartArray();
        }

        /// <summary>
        /// Writes the given collection of signal values to the trace file.
        /// </summary>
        /// <param name="values">The given collection of signal values</param>
        /// <param name="last">Flag indicating whether the given values, was the last so a newline should be written.</param>
        protected override void OutputSignalData(IEnumerable<Tuple<SignalEntry, object>> values, bool last)
        {
            if (!m_startedArray)
            {
                m_writer.WriteStartArray();
                m_startedArray = true;
            }

            foreach (var v in values)
            {
                if (v.Item2 is SME.ReadViolationException)
                    m_writer.WriteValue('U');
                else if (v.Item2 != null && v.Item1.Property.PropertyType.IsArray)
                {
                    var a = v.Item2 as Array;
                    m_writer.WriteStartArray();
                    for (var i = 0; i < a.Length; i++)
                        m_writer.WriteValue(a.GetValue(i));
                    m_writer.WriteEndArray();
                }
                else if (v.Item2 != null && v.Item1.Property.PropertyType.IsGenericType && v.Item1.Property.PropertyType.GetGenericTypeDefinition() == typeof(SME.IFixedArray<>))
                {
                    var attr = v.Item1.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();
                    var eltype = v.Item1.Property.PropertyType.GetGenericArguments().First();
                    var m = v.Item2.GetType().GetProperties().FirstOrDefault(x => x.GetIndexParameters().Length == 1);
                    var fixedInteraction = (IFixedArrayInteraction)v.Item2;
                    try
                    {
                        if (!fixedInteraction.CanRead(0))
                        {
                            m_writer.WriteValue('U');
                            continue;
                        }
                        m.GetValue(v.Item2, new object[] { 0 });
                    }
                    catch (Exception)
                    {
                        m_writer.WriteValue('U');
                        continue;
                    }

                    m_writer.WriteStartArray();
                    for (var i = 0; i < attr.Length; i++)
                    {
                        if (!fixedInteraction.CanRead(0))
                            m_writer.WriteValue('U');
                        else
                            m_writer.WriteValue(m.GetValue(v.Item2, new object[] { i }));
                    }
                    m_writer.WriteEndArray();
                }
                else if (v.Item2 != null && v.Item2.GetType().IsConstructedGenericType)
                {
                    m_writer.WriteValue("todo!");
                }
                else
                {
                    m_writer.WriteValue(v.Item2);
                }
            }

            if (last)
            {
                if (m_emitVariables)
                {
                    if (!m_startedArray)
                    {
                        m_writer.WriteStartArray();
                        m_startedArray = true;
                    }

                    foreach (var p in m_variables)
                        foreach (var f in p.Value)
                        {
                            if (f.Key.FieldType.IsArray)
                            {
                                m_writer.WriteStartArray();
                                foreach (var e in (Array)f.Key.GetValue(p.Key))
                                    m_writer.WriteValue(e);
                                m_writer.WriteEndArray();
                            }
                            else
                            {
                                m_writer.WriteValue(f.Key.GetValue(p.Key));
                            }
                        }
                }

                if (m_startedArray)
                    m_writer.WriteEndArray();
                m_startedArray = false;
            }
        }

        /// <summary>
        /// Dispose the current instance.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> the call originates from the dispose method, otherwise it comes from a finalize method.</param>
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
    }
}
