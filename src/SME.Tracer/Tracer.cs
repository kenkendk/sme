using SME;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace SME.Tracer
{
    /// <summary>
    /// Implementation of a tracer that captures data on the signals in the network.
    /// </summary>
    public abstract class Tracer : IDisposable
    {
        /// <summary>
        /// The list of signals to emit.
        /// </summary>
        protected SignalEntry[] m_props;
        /// <summary>
        /// Indicator to capture the very first output and emit the names of the variables here.
        /// </summary>
        private bool m_first = true;
        /// <summary>
        /// The number of signals that are considered driver signals.
        /// </summary>
        private int m_driversignalcount;
        /// <summary>
        /// Variable used to avoid emitting the (un)initialized state.
        /// </summary>
        private bool m_skipInitializationData = false;

        /// <summary>
        /// Finds the used signals and the attached busses.
        /// </summary>
        /// <returns>The list of signals.</returns>
        /// <param name="simulation">The simulaton instance that the signals are read from.</param>
        public static IEnumerable<SignalEntry> BuildPropertyMap(Simulation simulation)
        {
            return
                (from bus in simulation.Graph.AllBusses
                 from prop in bus.BusType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
                 select new SignalEntry()
                 {
                     Bus = bus,
                     Property = prop,
                     IsDriver = simulation.TopLevelInputBusses.Contains(bus),
                     SortKey = simulation.BusNames[bus] + "." + prop.Name
                 })
                .Where(x => !x.Bus.IsInternal)
                .OrderByDescending(x => x.IsDriver)
                .ThenBy(x => x.SortKey)
             ;
        }


        /// <summary>
        /// Method used to emit the signal names as part of the very first cycle.
        /// </summary>
        /// <param name="signals">The signals to emit the names for.</param>
        protected abstract void OutputSignalNames(SignalEntry[] signals);
        /// <summary>
        /// Method used to output the value for each of the signals.
        /// </summary>
        /// <param name="values">The signal and value pairs.</param>
        /// <param name="last">If set to <c>true</c> the signals are the last set in the current cycle.</param>
        protected abstract void OutputSignalData(IEnumerable<Tuple<SignalEntry, object>> values, bool last);

        /// <summary>
        /// Extracts the values from the bus signals.
        /// </summary>
        /// <returns>The The signal and value pairs.</returns>
        protected virtual IEnumerable<Tuple<SignalEntry, object>> GetValues()
        {
            foreach (var p in m_props)
            {
                object value = null;

                try
                {
                    if (!p.Bus.CanRead(p.Property.Name))
                        value = new SME.ReadViolationException("Signal not written");
                    else
                        value = p.Property.GetValue(p.Bus);

                }
                catch (Exception ex)
                {
                    if (ex is System.Reflection.TargetInvocationException)
                        ex = ((System.Reflection.TargetInvocationException)ex).InnerException;

                    if (!(ex is SME.ReadViolationException))
                        Console.WriteLine(string.Format("Failed to read item {0}.{1}, message: {2}", p.Property.DeclaringType.FullName, p.Property.Name, ex));
                    value = ex;
                }

                yield return new Tuple<SignalEntry, object>(p, value);
            }
        }

        /// <summary>
        /// Callback handler invoked before the current cycle has started.
        /// </summary>
        /// <param name="parent">The simulation that controls the cycle.</param>
        public void BeforeRun(Simulation parent)
        {
            if (m_skipInitializationData)
                return;

            // For the very first clock tick we emit the reset state
            // of the model
            if (m_first)
            {
                m_props = BuildPropertyMap(parent).ToArray();
                OutputSignalNames(m_props);
                m_first = false;
                m_driversignalcount = m_props.TakeWhile(x => x.IsDriver).Count();

                // Send out the init cycle
                OutputSignalData(GetValues(), true);
            }

            //OutputSignalData(GetValues().Take(m_driversignalcount), false);
        }

        /// <summary>
        /// Callback handler invoked after the current cycle has finished.
        /// </summary>
        /// <param name="parent">The simulation that controls the cycle.</param>
        public void AfterRun(Simulation parent)
        {
            if (m_skipInitializationData)
            {
                m_skipInitializationData = false;
                return;
            }

            //OutputSignalData(GetValues().Skip(m_driversignalcount), true);
            OutputSignalData(GetValues(), true);
        }

        /// <summary>
        /// Callback handler invoked after the clocked processes are invoked.
        /// </summary>
        /// <param name="parent">The simulation that controls the cycle.</param>
        public void AfterClockRun(Simulation parent)
        {
        }

        /// <summary>
        /// Dispose the current instance.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> the call originates from the dispose method, otherwise it comes from a finalize method.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:SME.Tracer.Tracer"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:SME.Tracer.Tracer"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:SME.Tracer.Tracer"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="T:SME.Tracer.Tracer"/> so
        /// the garbage collector can reclaim the memory that the <see cref="T:SME.Tracer.Tracer"/> was occupying.</remarks>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
