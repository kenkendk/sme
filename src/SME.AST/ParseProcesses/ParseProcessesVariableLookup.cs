using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace SME.AST
{
	// This partial deals with finding variables in the current scope
	public partial class ParseProcesses
	{
		/// <summary>
		/// Locates the target for an expression, and throws an exception if not found.
		/// </summary>
		/// <returns>The data element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to examine.</param>
		protected virtual DataElement LocateDataElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			var res = TryLocateDataElement(network, proc, method, statement, expression);
			if (res == null)
				throw new Exception($"Unable to locate item for {expression} in {statement.SourceStatement}");

			return res;
		}

		/// <summary>
		/// Locates the target for an expression.
		/// </summary>
		/// <returns>The data element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to examine.</param>
		protected ASTItem TryLocateElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			if (expression is ICSharpCode.NRefactory.CSharp.InvocationExpression)
			{
				var e = expression as ICSharpCode.NRefactory.CSharp.InvocationExpression;
				var target = e.Target;
				return LocateDataElement(network, proc, method, statement, target);
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.IndexerExpression)
			{
				var e = expression as ICSharpCode.NRefactory.CSharp.IndexerExpression;
				var target = e.Target;
				return LocateDataElement(network, proc, method, statement, target);
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
			{
				var e = expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression;
				var name = e.Identifier;

				if (method.LocalRenames.ContainsKey(name))
					name = method.LocalRenames[name];

				Variable variable;
				if (method != null && method.LocalVariables.TryGetValue(name, out variable))
					return variable;

				if (method != null)
				{
					var p = method.Parameters.FirstOrDefault(x => x.Name == name);
					if (p != null)
						return p;
				}

				if (proc.Variables.TryGetValue(name, out variable))
					return variable;

				Signal signal;
				if (proc != null && proc.Signals.TryGetValue(name, out signal))
					return signal;

				return null;
			}
			else if (expression is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
			{
				var e = expression as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;

				var parts = new List<string>();
				ICSharpCode.NRefactory.CSharp.Expression ec = e;
				while (ec != null)
				{
					if (ec is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
					{
						parts.Add(((ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)ec).MemberName);
						ec = ((ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)ec).Target;
					}
					else if (ec is ICSharpCode.NRefactory.CSharp.ThisReferenceExpression)
					{
						parts.Add("this");
						ec = null;
						break;
					}
					else if (ec is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
					{
						parts.Add(((ICSharpCode.NRefactory.CSharp.IdentifierExpression)ec).Identifier);
						ec = null;
						break;
					}
					else if (ec is ICSharpCode.NRefactory.CSharp.TypeReferenceExpression)
					{
						// Static reference, we store it as global
						ec = null;
						break;
					}
					else
					{
						throw new Exception($"Unexpected element in reference chain: {ec.GetType().FullName}");
					}
				}

				parts.Reverse();
				var fullname = string.Join(".", parts);


				ASTItem current = null;
				if (parts.First() == "this")
				{
					parts.RemoveAt(0);
					if (proc == null)
						throw new Exception("Attempting to do a resolve of \this\" but no process context is provided");
					current = proc;
				}

				var first = true;
				foreach (var el in parts)
				{
					var isIsFirst = first;
					first = false;

					if (current == null)
					{
						var pe = network.ConstantLookup.Keys.FirstOrDefault(x => x.Name == el);
						if (pe != null)
						{
							current = network.ConstantLookup[pe];
							continue;
						}
					}

					if (current == method || (isIsFirst && current == null))
					{
						//if (method.LocalRenames.ContainsKey(el))
						//	el = method.LocalRenames[el];

						if (method.LocalVariables.ContainsKey(el))
						{
							current = method.LocalVariables[el];
							continue;
						}

						var p = method.Parameters.FirstOrDefault(x => x.Name == el);
						if (p != null)
						{
							current = p;
							continue;
						}

						if (method.ReturnVariable != null && !string.IsNullOrWhiteSpace(method.ReturnVariable.Name) && el == method.ReturnVariable.Name)
						{
							current = method.ReturnVariable;
							continue;
						}
					}

					if (current == proc || (isIsFirst && current == null))
					{
						if (proc.BusInstances.ContainsKey(el))
						{
							current = proc.BusInstances[el];
							continue;
						}

						if (proc.Signals.ContainsKey(el))
						{
							current = proc.Signals[el];
							continue;
						}

						if (proc.Variables.ContainsKey(el))
						{
							current = proc.Variables[el];
							continue;
						}

						if (proc.Methods != null)
						{
							var p = proc.Methods.FirstOrDefault(x => x.Name == el);
							if (p != null)
							{
								current = p;
								continue;
							}
						}
					}

					if (current is Bus)
					{
						current = ((Bus)current).Signals.FirstOrDefault(x => x.Name == el);
						if (current != null)
							continue;
					}


					if (el == "Length" && (current is DataElement) && ((DataElement)current).CecilType.IsArrayType())
					{
						return new Constant()
						{
							ArrayLengthSource = current as DataElement,
							CecilType = LoadType(typeof(int)),
							DefaultValue = null,
							Source = (current as DataElement).Source
						};
					}

					throw new Exception($"Failed lookup at {el} in {fullname}");
				}

				if (current == null)
					throw new Exception($"Failed to fully resolve {fullname}");

				return current;
			}
			else
			{
				throw new Exception($"Unable to find a data element for an expression of type {expression.GetType().FullName}");
			}
		}

		/// <summary>
		/// Locates the target for an expression.
		/// </summary>
		/// <returns>The data element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the statement is found.</param>
		/// <param name="statement">The statement where the expression is found.</param>
		/// <param name="expression">The expression to examine.</param>
		protected DataElement TryLocateDataElement(NetworkState network, ProcessState proc, MethodState method, Statement statement, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			var el = TryLocateElement(network, proc, method, statement, expression);
			if (el == null)
				return null;

			if (!(el is DataElement))
				throw new Exception($"Failed to fully resolve {expression.ToString()}, got a result of type {el.GetType().FullName}");

			return (DataElement)el;
		}

		/// <summary>
		/// Registers a temporary variable for use within the method
		/// </summary>
		/// <returns>The temporary variable.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method to create the variable in.</param>
		/// <param name="vartype">The data type of the variable.</param>
		/// <param name="source">The source of the variable</param>
		protected virtual Variable RegisterTemporaryVariable(NetworkState network, ProcessState proc, MethodState method, TypeReference vartype, object source)
		{
			var varname = "tmpvar_" + (network.VariableCount++).ToString();
			var res = new Variable()
			{
				CecilType = vartype,
				Name = varname,
				Source = source,
				Parent = (ASTItem)method ?? proc
			};
			method.LocalVariables.Add(varname, res);

			return res;
		}

		/// <summary>
		/// Parses a a field reference and returns the associated variable
		/// </summary>
		/// <returns>The constant element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method where the initializer is found</param>
		/// <param name="vartype">The variable type</param>
		/// <param name="variable">The field to parse.</param>
		protected virtual DataElement RegisterVariable(NetworkState network, ProcessState proc, MethodState method, TypeReference vartype, ICSharpCode.NRefactory.CSharp.VariableInitializer variable)
		{
			var c = new Variable()
			{
				CecilType = vartype,
				Name = variable.Name,
				DefaultValue = variable.Initializer ,
				Source = variable,
				Parent = (ASTItem)method ?? proc
			};

			method.LocalVariables.Add(variable.Name, c);
			return c;
		}

		/// <summary>
		/// Parses a a field reference and returns the associated bus
		/// </summary>
		/// <returns>The constant element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="field">The field to parse.</param>
		protected virtual Bus RegisterBusReference(NetworkState network, ProcessState proc, FieldDefinition field)
		{
			var bus = proc.InputBusses.Union(proc.OutputBusses).Union(proc.InternalBusses).FirstOrDefault(x => LoadType(x.SourceType).IsSameTypeReference(field.FieldType));
			if (bus == null)
				throw new Exception($"No such bus: {field.FieldType.FullName}");

			proc.BusInstances.Add(field.Name, bus);
			return bus;
		}


		/// <summary>
		/// Parses a a field reference and returns the associated variable
		/// </summary>
		/// <returns>The constant element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="field">The field to parse.</param>
		protected virtual DataElement RegisterVariable(NetworkState network, ProcessState proc, FieldDefinition field)
		{
			DataElement res;

			if (field.IsLiteral)
			{
				var c = new Constant() {
					CecilType = field.FieldType,
					DefaultValue = field.Constant,
					Name = field.Name,
					Source = field,
					Parent = proc
				};
				res = c;
				network.ConstantLookup.Add(field, c);
			}
			else if (field.IsStatic && field.IsInitOnly)
			{
				var c = new Constant()
				{
					CecilType = field.FieldType,
					DefaultValue = field.Constant,
					Name = field.Name,
					Source = field,
					Parent = proc
				};
				res = c;
				network.ConstantLookup.Add(field, c);				
			}
			else if (field.IsStatic)
			{
				//Don't care
				res = null;
			}
			else if (field.GetAttribute<Signal>() == null)
			{
				var c = new Variable()
				{
					CecilType = field.FieldType,
					DefaultValue = null,
					Name = field.Name,
					Source = field,
					Type = null,
					Parent = proc
				};
				res = c;
				proc.Variables.Add(field.Name, c);
			}
			else
			{
				var c = new Signal()
				{
					CecilType = field.FieldType,
					DefaultValue = null,
					Name = field.Name,
					Source = field,
					Type = null,
					Parent = proc
				};
				res = c;
				proc.Signals.Add(field.Name, c);
			}

			return res;
		}

		/// <summary>
		/// Parses a a parameter reference and returns a new AST reference
		/// </summary>
		/// <returns>The constant element.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method the parameter belongs to.</param>
		/// <param name="parameter">The parameter to parse.</param>
		protected virtual Parameter ParseParameter(NetworkState network, ProcessState proc, MethodState method, ParameterDefinition parameter)
		{
			return new Parameter()
			{
				CecilType = parameter.ParameterType,
				Name = parameter.Name,
				DefaultValue = null,
				Source = parameter,
				Parent = method
			};
		}

		/// <summary>
		/// Sets the default value for a field
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="item">The data element to set the default value for.</param>
		/// <param name="value">The value to set.</param>
		/// <param name="is_static">A flag indicating if the variable default is statically defined.</param>
		protected virtual void SetDataElementDefaultValue(NetworkState network, ProcessState proc, DataElement item, object value, bool is_static)
		{
			item.DefaultValue = value;
		}

		/// <summary>
		/// Locates a bus by reference
		/// </summary>
		/// <returns>The bus.</returns>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method the expression is found.</param>
		/// <param name="expression">The expression used to initialize the bus.</param>
		protected virtual Bus LocateBus(NetworkState network, ProcessState proc, MethodState method, ICSharpCode.NRefactory.CSharp.Expression expression)
		{
			var de = TryLocateElement(network, proc, method, null, expression);
			if (de is AST.Bus)
				return de as AST.Bus;

			throw new Exception("Need to walk the tree?");
		}
	}
}
