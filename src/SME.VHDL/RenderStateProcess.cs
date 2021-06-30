using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;
using Microsoft.CodeAnalysis;

namespace SME.VHDL
{
    /// <summary>
    /// The render state for a process.
    /// </summary>
    public class RenderStateProcess
    {
        /// <summary>
        /// The parent render state.
        /// </summary>
        public readonly RenderState Parent;

        /// <summary>
        /// The process used in this render state.
        /// </summary>
        public readonly AST.Process Process;

        /// <summary>
        /// The render helper to use.
        /// </summary>
        public readonly RenderHelper Helper;

        /// <summary>
        /// A lookup associating an AST node with a VHDL type.
        /// </summary>
        public readonly Dictionary<ASTItem, VHDLType> TypeLookup;

        /// <summary>
        /// The type scope used to resolve VHDL types.
        /// </summary>
        public readonly VHDLTypeScope TypeScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.RenderStateProcess"/> class.
        /// </summary>
        /// <param name="parent">The parent render state.</param>
        /// <param name="process">The process to render.</param>
        public RenderStateProcess(RenderState parent, AST.Process process)
        {
            Parent = parent;
            Process = process;
            Helper = new RenderHelper(parent, process);
            TypeLookup = parent.TypeLookup;
            TypeScope = parent.TypeScope;
        }

        /// <summary>
        /// Returns the VHDL type for a data element.
        /// </summary>
        /// <returns>The VHDL type.</returns>
        /// <param name="element">The element to get the type for.</param>
        public VHDLType VHDLType(AST.DataElement element)
        {
            return Parent.VHDLType(element);
        }

        /// <summary>
        /// Returns the VHDL type for an expression.
        /// </summary>
        /// <returns>The VHDL type.</returns>
        /// <param name="element">The expression to get the type for.</param>
        public VHDLType VHDLType(AST.Expression element)
        {
            return Parent.VHDLType(element);
        }

        /// <summary>
        /// Gets the default value for an item, expressed as a VHDL expression.
        /// </summary>
        /// <returns>The default value.</returns>
        /// <param name="element">The element to get the default value for.</param>
        public string DefaultValue(AST.DataElement element)
        {
            return Parent.DefaultValue(element);
        }

        /// <summary>
        /// Gets the finite state method, if any for this process.
        /// </summary>
        /// <value>The finite state method.</value>
        public Method FiniteStateMethod
        {
            get
            {
                return Process.Methods?.FirstOrDefault(x => x.IsStateMachine);
            }
        }

        /// <summary>
        /// Returns all signals written to a bus from within this process.
        /// </summary>
        /// <returns>The written signals.</returns>
        /// <param name="bus">The bus to get the signals for.</param>
        public IEnumerable<BusSignal> WrittenSignals(AST.Bus bus)
        {
            return Parent.WrittenSignals(Process, bus);
        }

        /// <summary>
        /// Gets the custom renderer for this instance, or null.
        /// </summary>
        /// <returns>The custom renderer.</returns>
        private ICustomRenderer GetCustomRenderer()
        {
            return Parent.GetCustomRenderer(Process);
        }

        /// <summary>
        /// Gets a value indicating if the process has a custom renderer.
        /// </summary>
        public bool HasCustomRenderer => GetCustomRenderer() != null;

        /// <summary>
        /// Gets the include region for a custom renderer.
        /// </summary>
        public string CustomRendererInclude
        {
            get
            {
                var renderer = GetCustomRenderer();
                if (renderer == null)
                    return null;

                return renderer.IncludeRegion(this, 0);
            }
        }

        /// <summary>
        /// Gets the custom renderer body.
        /// </summary>
        public string CustomRendererBody
        {
            get
            {
                var renderer = GetCustomRenderer();
                if (renderer == null)
                    return null;

                return renderer.BodyRegion(this, 0);
            }
        }



        /// <summary>
        /// Returns a sequence of all the reset statements emitted when the RST signal goes high.
        /// </summary>
        public IEnumerable<string> ProcessResetStaments
        {
            get
            {
                // We do these in the FSM method
                if (FiniteStateMethod == null)
                {
                    foreach (var bus in Process.OutputBusses.Concat(Process.InternalBusses).Distinct())
                        foreach (var signal in WrittenSignals(bus))
                            foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(signal), 0))
                                yield return s;
                }
                // For FSM, we reset the current state
                else
                {
                    foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(Process.InternalDataElements[Process.InternalDataElements.Length - 2]), 0))
                        yield return s;
                }

                foreach (var signal in Process.SharedSignals)
                    if (!(signal.Source is IFieldSymbol) || ((IFieldSymbol)signal.Source).GetAttribute<IgnoreAttribute>() == null)
                        foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(signal), 0))
                            yield return s;

                //foreach (var v in Process.SharedVariables)
                    //if (!(v.Source is Mono.Cecil.IMemberDefinition) || ((Mono.Cecil.IMemberDefinition)v.Source).GetAttribute<IgnoreAttribute>() == null)
                        //foreach (var s in RenderStatement(null, GetResetStatement(v), 0))
                            //yield return s;

                if (Process.MainMethod != null)
                {
                    foreach(var variable in Process.MainMethod.AllVariables)
                        if (!variable.isLoopIndex)
                            foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(variable), 0))
                                yield return s;

                    if (Parent.TemporaryVariables.ContainsKey(Process.MainMethod))
                        foreach (var variable in Parent.TemporaryVariables[Process.MainMethod].Values)
                            foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(variable), 0))
                                yield return s;
                }
            }
        }

        /// <summary>
        /// Returns a sequence of statements to perform when the clock signal rises.
        /// </summary>
        public IEnumerable<string> ClockResetStaments
        {
            get
            {
                foreach (var bus in Process.InternalBusses)
                    foreach (var signal in bus.Signals)
                        foreach (var s in Helper.RenderStatement(null, Parent.GetResetStatement(signal), 0))
                            yield return s;
            }
        }

        /// <summary>
        /// Gets a sequence of the shared variables found in the process.
        /// </summary>
        /// <value>The shared variables.</value>
        public IEnumerable<Variable> SharedVariables
        {
            get
            {
                foreach (var v in Process.SharedVariables)
                    if (!(v.Source is IFieldSymbol) || ((IFieldSymbol)v.Source).GetAttribute<IgnoreAttribute>() == null)
                        yield return v;
            }
        }

        /// <summary>
        /// Gets a sequence of the variables found in the main method.
        /// </summary>
        /// <value>The variables.</value>
        public IEnumerable<Variable> Variables
        {
            get
            {
                if (Process.MainMethod != null)
                    foreach (var v in Process.MainMethod.AllVariables)
                        yield return v;

                foreach (var m in Parent.TemporaryVariables)
                    if (m.Key == Process.MainMethod)
                        foreach (var v in m.Value.Values)
                            yield return v;
            }
        }
    }
}
