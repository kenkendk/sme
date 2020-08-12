using System;
using Newtonsoft.Json;

namespace SME.Tracer.JsonTypes
{
    /// <summary>
    /// Core properties for a process.
    /// </summary>
    public class Process
    {
        /// <summary>
        /// The process ID.
        /// </summary>
        [JsonProperty("id")]
        public long ID;
        /// <summary>
        /// The name of the process.
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        /// <summary>
        /// The process source class.
        /// </summary>
        [JsonProperty("sourceclass")]
        public string SourceClass;
        /// <summary>
        /// A value indicating if the process is clocked.
        /// </summary>
        [JsonProperty("isclocked")]
        public bool IsClocked;
        /// <summary>
        /// The input busses in this process.
        /// </summary>
        [JsonProperty("inbusses")]
        public long[] InputBusses;
        /// <summary>
        /// The output busses in this process.
        /// </summary>
        [JsonProperty("outbusses")]
        public long[] OutputBusses;
        /// <summary>
        /// The internal busses in this process.
        /// </summary>
        [JsonProperty("internalbusses")]
        public long[] InternalBusses;
        /// <summary>
        /// The variables in this process.
        /// </summary>
        [JsonProperty("variables")]
        public Variable[] Variables;
    }

    /// <summary>
    /// Element for representing a variable.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// The signal ID.
        /// </summary>
        [JsonProperty("id")]
        public long ID;
        /// <summary>
        /// A value indicating if the variable is treated as a signal.
        /// </summary>
        [JsonProperty("issignal")]
        public bool IsSignal;
        /// <summary>
        /// The signal name.
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        /// <summary>
        /// The signal type.
        /// </summary>
        [JsonProperty("type")]
        public string Type;
    }

    /// <summary>
    /// Element for representing process dependencies.
    /// </summary>
    public class ProcessTree
    {
        /// <summary>
        /// The name of this process.
        /// </summary>
        [JsonProperty("self")]
        public long Self;

        /// <summary>
        /// The parent dependencies for this process.
        /// </summary>
        [JsonProperty("parents")]
        public long[] Parents;

        /// <summary>
        /// The processes that depends on this process.
        /// </summary>
        [JsonProperty("children")]
        public long[] Children;
    }

    /// <summary>
    /// Element for representing a signal.
    /// </summary>
    public class Signal
    {
        /// <summary>
        /// The signal ID.
        /// </summary>
        [JsonProperty("id")]
        public long ID;
        /// <summary>
        /// The signal name.
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        /// <summary>
        /// The signal type.
        /// </summary>
        [JsonProperty("type")]
        public string Type;
        /// <summary>
        /// Flag indicating if the signal is a driver signal.
        /// </summary>
        [JsonProperty("isdriver")]
        public bool IsDriver;
        /// <summary>
        /// Flag indicating if the signal is internal.
        /// </summary>
        [JsonProperty("isinternal")]
        public bool IsInternal;
    }

    /// <summary>
    /// Element for representing a bus.
    /// </summary>
    public class Bus
    {
        /// <summary>
        /// The bus ID.
        /// </summary>
        [JsonProperty("id")]
        public long ID;
        /// <summary>
        /// The name of the bus.
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        /// <summary>
        /// The bus source class.
        /// </summary>
        [JsonProperty("sourceclass")]
        public string SourceClass;
        /// <summary>
        /// The signals in this bus.
        /// </summary>
        [JsonProperty("signals")]
        public Signal[] Signals;
        /// <summary>
        /// Flag indicating if the bus is clocked.
        /// </summary>
        [JsonProperty("isclocked")]
        public bool IsClocked;
        /// <summary>
        /// Flag indicating if the bus is internal.
        /// </summary>
        [JsonProperty("isinternal")]
        public bool IsInternal;
    }

    /// <summary>
    /// Element for representing the entire network.
    /// </summary>
    public class Network
    {
        /// <summary>
        /// The processes in the network.
        /// </summary>
        [JsonProperty("processes")]
        public Process[] Processes;

        /// <summary>
        /// The process dependencies.
        /// </summary>
        [JsonProperty("tree")]
        public ProcessTree[] Tree;

        /// <summary>
        /// The busses in the network.
        /// </summary>
        [JsonProperty("busses")]
        public Bus[] Busses;

        /// <summary>
        /// An array with ID's for the fields present in each of the entries in the values area.
        /// </summary>
        [JsonProperty("valuemap")]
        public long[] ValueMap;

        /// <summary>
        /// A value indicating if the trace file contains process variables.
        /// </summary>
        [JsonProperty("hasvariables")]
        public bool IncludesVariables;

        /// <summary>
        /// A value indicating if the trace file contains process variable arrays.
        /// </summary>
        [JsonProperty("hasvariablearrays")]
        public bool IncludesVariableArrays;
    }
}
