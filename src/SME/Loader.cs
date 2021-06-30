using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace SME
{
    /// <summary>
    /// The class that initializes and starts all defined processes as well as loads all instances of buses.
    /// </summary>
    public static class Loader
    {
        /// <summary>
        /// Helper variable to toggle assignment debug output.
        /// </summary>
        public static bool DebugBusAssignments = false;

        /// <summary>
        /// Gets all the bus fields in the specified type.
        /// </summary>
        /// <returns>The bus fields.</returns>
        /// <param name="t">The type to examine.</param>
        public static IEnumerable<FieldInfo> GetBusFields(Type t)
        {
            return t
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(n =>
                {
                    if (typeof(IBus).IsAssignableFrom(n.FieldType))
                        return true;

                    if (n.FieldType.IsArray && typeof(IBus).IsAssignableFrom(n.FieldType.GetElementType()))
                        return true;

                    return false;
                });
        }

        /// <summary>
        /// Gets all <see cref="T:SME.Bus"/> instances from the specified field.
        /// </summary>
        /// <returns>The <see cref="T:SME.Bus"/> instances.</returns>
        /// <param name="source">The object to extract the instances from.</param>
        /// <param name="field">The field to extract the instances from</param>
        public static IEnumerable<IBus> GetBusInstances(object source, FieldInfo field)
        {
            var v = field.GetValue(source);
            if (v == null)
                yield break;

            if (v.GetType().IsArray)
            {
                var a = (Array)v;
                for (var i = 0; i < a.GetLength(0); i++)
                    yield return (IBus)a.GetValue(i);
            }
            else
                yield return (IBus)field.GetValue(source);
        }

        /// <summary>
        /// Loads all <see cref="T:SME.Bus"/> interface fields for the given object.
        /// </summary>
        /// <returns>The object with the bus loaded.</returns>
        /// <param name="o">The object instance to load bus for.</param>
        /// <param name="forceautoload">Forces automatic bus loading, even if the <see cref="SME.AutoloadBusAttribute"/> is not set.</param>
        public static object AutoloadBusses(object o, bool forceautoload = false)
        {
            if (DebugBusAssignments)
                Console.WriteLine("Autoloading busses for {0}", o.GetType().FullName);

            // Autoload if the process is marked as Autoload
            forceautoload |= (o.GetType().GetCustomAttributes(typeof(AutoloadBusAttribute)).FirstOrDefault() as AutoloadBusAttribute != null);

            foreach (var f in GetBusFields(o.GetType()))
                if (f.GetValue(o) == null)
                {
                    var internalBus = (f.GetCustomAttributes(typeof(InternalBusAttribute)).FirstOrDefault() as InternalBusAttribute != null);
                    var componentBus = (f.GetCustomAttributes(typeof(ComponentBusAttribute)).FirstOrDefault() as ComponentBusAttribute != null);
                    var autoloadbus = forceautoload || (f.GetCustomAttributes(typeof(AutoloadBusAttribute)).FirstOrDefault() as AutoloadBusAttribute != null);

                    if (autoloadbus || typeof(ISingletonBus).IsAssignableFrom(f.FieldType))
                    {
                        var bus = Scope.CreateOrLoadBus(f.FieldType, null, internalBus, componentBus);
                        if (DebugBusAssignments)
                            Console.WriteLine("Setting field {0}.{1} = {2}:{3}:{4} -> {5}", f.DeclaringType.Name, f.Name, null, bus, f.FieldType.FullName, bus.GetHashCode());

                        f.SetValue(o, Scope.CreateOrLoadBus(f.FieldType, null, internalBus));
                    }
                }

            return o;
        }

        /// <summary>
        /// Creates a single bus for each class that implements <see cref="SME.IProcess"/> and runs the simulation on that.
        /// </summary>
        /// <param name="asm">The assembly to load.</param>
        /// <param name="autoloadbusses">Forces automatic bus loading on the processes, even if the <see cref="SME.AutoloadBusAttribute"/> is not set</param>
        public static IProcess[] StartProcesses(Assembly asm, bool autoloadbusses)
        {
            var procs = asm
                .GetTypes()
                .Where(x => typeof(IProcess).IsAssignableFrom(x) && x.GetConstructor(new Type[0]) != null)
                .Select(x => (IProcess)Activator.CreateInstance(x))
                .ToArray();

            foreach (var p in procs)
                AutoloadBusses(p, autoloadbusses);

            return procs;
        }
    }
}
