using System;
using System.Collections.Generic;
using System.Linq;

namespace SME
{
    /// <summary>
    /// Description of a process with captured metadata and associated methods.
    /// </summary>
    public class ProcessMetadata
    {
        /// <summary>
        /// The definition for the fields to read.
        /// </summary>
        private const System.Reflection.BindingFlags FIELD_FLAGS = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// The instance that is recorded.
        /// </summary>
        public readonly IProcess Instance;

        /// <summary>
        /// Gets or sets the initialization capture values.
        /// </summary>
        public Dictionary<string, object> Initialization { get; } = new Dictionary<string, object>();


        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.ProcessMetadata"/> class.
        /// </summary>
        /// <param name="process">The process to wrap.</param>
        public ProcessMetadata(IProcess process)
        {
            Instance = process;
        }

        /// <summary>
        /// Captures all initial values and records them as reset values.
        /// </summary>
        public void RegisterInitializationData()
        {
            foreach (var fi in Instance.GetType().GetFields(FIELD_FLAGS))
            {
                var source = fi.GetValue(Instance);
                if (source == null)
                {
                    Initialization[fi.Name] = null;
                    return;
                }

                var t = fi.FieldType;

                if (t.IsArray)
                {
                    var arraysource = (Array)source;
                    int[] lengths = new int[arraysource.Rank];
                    for (int i = 0; i < arraysource.Rank; i++)
                        lengths[i] = arraysource.GetLength(i);
                    var target = Array.CreateInstance(t.GetElementType(), lengths);
                    Array.Copy(arraysource, target, arraysource.Length);
                    Initialization[fi.Name] = target;
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IFixedArray<>))
                {
                    var attr = fi.GetCustomAttributes(typeof(FixedArrayLengthAttribute), true).OfType<FixedArrayLengthAttribute>().FirstOrDefault();
                    if (attr == null)
                        throw new InvalidProgramException($"The field {fi.Name} in {fi.DeclaringType.FullName} is missing the {nameof(FixedArrayLengthAttribute)} attribute");

                    var target = Array.CreateInstance(t.GenericTypeArguments[0], attr.Length);

                    // TODO: Implement the copy here if we support initialized
                    // arrays at some point. This is only if a process has
                    // IFixedArray as a field.
                    //Array.Copy(arraysource, target, arraysource.Length);
                    Initialization[fi.Name] = target;
                }
                else
                {
                    Initialization[fi.Name] = source;
                }
            }
        }

        /// <summary>
        /// Writes all value from the recorded initialization data back to the instance.
        /// </summary>
        public void ResetInstance()
        {
            foreach (var n in Initialization.Keys)
            {
                var fi = Instance.GetType().GetField(n, FIELD_FLAGS);
                if (fi != null)
                    fi.SetValue(Instance, Initialization[n]);
            }
        }
    }
}
