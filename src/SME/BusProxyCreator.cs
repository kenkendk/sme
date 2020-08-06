using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SME
{
    /// <summary>
    /// Class for emitting proxies of <see cref="T:SME.Bus"/> at runtime.
    /// </summary>
    public class BusProxyCreator
    {
        /// <summary>
        /// The interface cache, key is the interface, value is the dynamic type.
        /// </summary>
        private static readonly Dictionary<Type, TypeInfo> _interfaceCache = new Dictionary<Type, TypeInfo>();

        /// <summary>
        /// Creates a new runtime defined type and instance for the given interface type.
        /// </summary>
        /// <param name="interface">The interface to create to instance for.</param>
        /// <param name="clock">The clock to tick with.</param>
        /// <param name="isClocked">A value indicating if the bus is clocked.</param>
        /// <param name="isInternal">A value indicating if the bus is internal.</param>
        internal static IRuntimeBus CreateBusProxy(Type @interface, Clock clock, bool isClocked, bool isInternal)
        {
            if (@interface == null)
                throw new ArgumentNullException(nameof(@interface));
            if (!@interface.IsInterface)
                throw new Exception($"Cannot create proxy from non-interface type: {@interface.FullName}");
            if (!typeof(IBus).IsAssignableFrom(@interface))
                throw new Exception($"Cannot create proxy from interface type: {@interface.FullName} as it does not implement {nameof(IRuntimeBus)}");

            if (!_interfaceCache.ContainsKey(@interface))
            {
                // Check if the interface has methods defined, as we do not support that
                var userprops = new HashSet<MethodInfo>(
                    @interface
                    .GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(x => new[] { x.GetGetMethod(), x.GetSetMethod() })
                    .Where(x => x != null)
                );

                if (@interface.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).Where(x => !userprops.Contains(x)).Any())
                    throw new Exception($"The interface cannot have any methods defined: {@interface.FullName}");

                // Create the assembly and type
                var typename = "DynamicBusProxy." + @interface.Name + "." + Guid.NewGuid().ToString("N").Substring(0, 6);

                // Build an assembly and a module to contain the type
                var assemblyName = new AssemblyName($"{nameof(SME)}.{nameof(Bus)}Proxy.{typename}");
                var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var module = assembly.DefineDynamicModule(assemblyName.Name);

                // Create the type definition
                var typeBuilder = module.DefineType(typename, TypeAttributes.Public, typeof(object), new Type[] { @interface, typeof(IRuntimeBus) });

                // Add the field holding the target instance reference
                var targetField = typeBuilder.DefineField("m_target", typeof(Bus), FieldAttributes.Private | FieldAttributes.InitOnly);

                // The constructor only accepts the target
                var constructorArgs = new Type[] { typeof(Bus) };

                // Create the constructor to set the target field
                var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
                var construtorIL = constructor.GetILGenerator();
                construtorIL.Emit(OpCodes.Ldarg_0);
                construtorIL.Emit(OpCodes.Ldarg_1);
                construtorIL.Emit(OpCodes.Stfld, targetField);
                construtorIL.Emit(OpCodes.Ret);

                // For calls to IRuntimeBus methods, forward calls to the target.
                // To avoid name clashes with the user-defined properties, we use an interface mapping,
                // aka explicit interface implementation in C#
                //
                // The proxy does not have IRuntimeBus properties itself, so these properties cannot be
                // directly accessed on the proxy instance, and cannot be found via reflection.
                // But when the proxy is cast as a IRuntimeBus, the properties can be accessed
                // as the getter/setter methods are wired correctly.
                // Because of this, we do not need explict handling of properties, we can
                // just treat them like methods.
                foreach (var sourceMethod in typeof(IRuntimeBus).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                {
                    var parameterTypes = sourceMethod.GetParameters().Select(x => x.ParameterType).ToArray();

                    // Replicate the source method
                    var method = typeBuilder.DefineMethod(
                        typeof(IRuntimeBus).Name  + "." + sourceMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        sourceMethod.ReturnType,
                        parameterTypes
                    );

                    // Write the IL to call the method through the interface
                    var methodIL = method.GetILGenerator();
                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldfld, targetField);
                    EmitArgumentLoad(methodIL, parameterTypes, 1);
                    methodIL.Emit(OpCodes.Callvirt, sourceMethod);
                    methodIL.Emit(OpCodes.Ret);

                    // Specify that our specially named method implements the interface method
                    typeBuilder.DefineMethodOverride(method, sourceMethod);
                }

                // For calls to user-defined properties we call Read and Write on the target
                // We implement the interface as regular properties so the bus proxy can be examined
                // with reflection and looks like a reasonable implementation of the interface
                foreach (var sourceProperty in @interface.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance))
                {
                    var indexParameters = sourceProperty.GetIndexParameters().Select(x => x.ParameterType).ToArray();
                    if (indexParameters.Length != 0)
                        throw new ArgumentException($"Unexpected indexed property: {sourceProperty.Name}");

                    var property = typeBuilder.DefineProperty(
                        sourceProperty.Name,
                        PropertyAttributes.None,
                        sourceProperty.PropertyType,
                        indexParameters
                    );
                    if (sourceProperty.CanRead)
                    {
                        var getMethod = typeBuilder.DefineMethod(
                            "get_" + sourceProperty.Name,
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                            CallingConventions.HasThis,
                            sourceProperty.PropertyType,
                            indexParameters
                        );

                        var getMethodIL = getMethod.GetILGenerator();
                        getMethodIL.Emit(OpCodes.Ldarg_0);
                        getMethodIL.Emit(OpCodes.Ldfld, targetField);
                        getMethodIL.Emit(OpCodes.Ldstr, sourceProperty.Name);
                        EmitArgumentLoad(getMethodIL, indexParameters, 1);

                        var destMethod = typeof(Bus).GetMethods().Where(x => x.Name == nameof(Bus.Read) && x.IsGenericMethodDefinition).First();
                        destMethod = destMethod.MakeGenericMethod(new Type[] { sourceProperty.PropertyType });
                        getMethodIL.Emit(OpCodes.Call, destMethod);
                        getMethodIL.Emit(OpCodes.Ret);

                        property.SetGetMethod(getMethod);
                    }

                    if (sourceProperty.CanWrite)
                    {
                        var setMethod = typeBuilder.DefineMethod(
                            "set_" + sourceProperty.Name,
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                            CallingConventions.HasThis,
                            typeof(void),
                            new Type[] { sourceProperty.PropertyType }.Concat(indexParameters).ToArray()
                        );

                        var setMethodIL = setMethod.GetILGenerator();
                        setMethodIL.Emit(OpCodes.Ldarg_0);
                        setMethodIL.Emit(OpCodes.Ldfld, targetField);
                        setMethodIL.Emit(OpCodes.Ldstr, sourceProperty.Name);
                        EmitArgumentLoad(setMethodIL, indexParameters, 1);

                        setMethodIL.Emit(OpCodes.Ldarg_1);
                        if (sourceProperty.PropertyType.IsValueType)
                            setMethodIL.Emit(OpCodes.Box, sourceProperty.PropertyType);
                        var destMethod = typeof(Bus).GetMethod(nameof(Bus.Write));
                        setMethodIL.Emit(OpCodes.Call, destMethod);
                        setMethodIL.Emit(OpCodes.Ret);

                        property.SetSetMethod(setMethod);
                    }

                }

                _interfaceCache[@interface] = typeBuilder.CreateTypeInfo();
            }

            return (IRuntimeBus)Activator.CreateInstance(_interfaceCache[@interface], new object[] { new Bus(@interface, clock, isClocked, isInternal) });

        }

        /// <summary>
        /// Loads all arguments onto the stack.
        /// </summary>
        /// <param name="generator">The IL generator to use.</param>
        /// <param name="parameterTypes">The parameters to load.</param>
        /// <param name="offset">The argument offset, should be <c>1</c> if the method is not static.</param>
        private static void EmitArgumentLoad(ILGenerator generator, Type[] parameterTypes, int offset)
        {
            if (parameterTypes == null || parameterTypes.Length == 0)
                return;

            for (var i = 0; i < parameterTypes.Length; i++)
                EmitLdarg(generator, i + offset);
        }

        /// <summary>
        /// Helper method to emit a argument load, using a short opcode if possible.
        /// </summary>
        /// <param name="generator">The IL generator to emit with.</param>
        /// <param name="argno">The argument number to load.</param>
        private static void EmitLdarg(ILGenerator generator, int argno)
        {
            switch (argno)
            {
                case 0:
                    generator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    generator.Emit(OpCodes.Ldarg, argno);
                    break;
            }
        }
    }
}
