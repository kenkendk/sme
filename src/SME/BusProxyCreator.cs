using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SME
{
    public class BusProxyCreator
    {
        /// <summary>
        /// The interface cache, key is the interface, value is the dynamic type.
        /// </summary>
        private static readonly Dictionary<Type, TypeInfo> _interfaceCache = new Dictionary<Type, TypeInfo>();

        /// <summary>
        /// Creates a new runtime defined type and instance for the given interface type
        /// </summary>
        /// <param name="interface">The interface to create to instance for</param>
		/// <param name="clock">The clock to tick with</param>
		/// <param name="isClocked">A value indicating if the bus is clocked</param>
		/// <param name="isInternal">A value indicating if the bus is internal</param>
        internal static IBus CreateBusProxy(Type @interface, Clock clock, bool isClocked, bool isInternal)
        {
            if (@interface == null)
                throw new ArgumentNullException(nameof(@interface));
            if (!@interface.IsInterface)
                throw new Exception($"Cannot create proxy from non-interface type: {@interface.FullName}");
            if (!typeof(IBus).IsAssignableFrom(@interface))
                throw new Exception($"Cannot create proxy from interface type: {@interface.FullName} as it does not implement {nameof(IBus)}");
    
            if (!_interfaceCache.ContainsKey(@interface))
            {
                // Create the assembly and type
                var typename = "DynamicBusProxy." + @interface.Name + "." + Guid.NewGuid().ToString("N").Substring(0, 6);

                // Build an assembly and a module to contain the type
                var assemblyName = new AssemblyName($"{nameof(SME)}.{nameof(Bus)}Proxy.{typename}");
                var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var module = assembly.DefineDynamicModule(assemblyName.Name);

                // Create the type definition
                var typeBuilder = module.DefineType(typename, TypeAttributes.Public, typeof(object), new Type[] { @interface });

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

                // Get a list of property names that are used internally and need special handling
                var coreprops = typeof(IBus).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

                // The internal property names needs to be filtered as they are handled different than user values
                var basenames = coreprops.ToDictionary(x => x.Name);
                
                // Also remove the property methods as we implement them directly as properties
                var gettersetter = new HashSet<MethodInfo>(coreprops.SelectMany(x => new [] { x.GetGetMethod(), x.GetSetMethod() }).Where(x => x != null));

                // Some methods are implemented explicitly, so they need a special mapping access
                var ifmap = typeof(Bus).GetInterfaceMap(typeof(IBus));

                // For calls to IBus methods, we simply call the target
                foreach (var sourceMethod in typeof(IBus).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(x => !gettersetter.Contains(x)))
                {
                    var parameterTypes = sourceMethod.GetParameters().Select(x => x.ParameterType).ToArray();

                    // Replicate the source method
                    var method = typeBuilder.DefineMethod(
                        sourceMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        sourceMethod.ReturnType,
                        parameterTypes
                    );

                    // Since we invoke members through the interface map, we do not need this here

                    // var ix = Array.IndexOf(ifmap.InterfaceMethods, sourceMethod);
                    // var destMethod = ifmap.TargetMethods[ix];

                    //var destMethod = typeof(Bus).GetMethod(sourceMethod.Name, parameterTypes);
                    var methodIL = method.GetILGenerator();
                    methodIL.Emit(OpCodes.Ldarg_0);
                    methodIL.Emit(OpCodes.Ldfld, targetField);
                    EmitArgumentLoad(methodIL, parameterTypes, 1);
                    methodIL.Emit(OpCodes.Callvirt, sourceMethod);
                    methodIL.Emit(OpCodes.Ret);
                }

                // For calls to IBus properties, we simply call the target
                foreach (var sourceProperty in typeof(IBus).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
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
                            "get_" + property.Name,
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                            CallingConventions.HasThis,
                            sourceProperty.PropertyType,
                            indexParameters
                        );

                        // Since we invoke members through the interface map, we do not need this here
                        // var ix = Array.IndexOf(ifmap.InterfaceMethods, sourceProperty.GetGetMethod());
                        // var destMethod = ifmap.TargetMethods[ix];
                        //var destMethod = typeof(Bus).GetProperty(sourceProperty.Name, sourceProperty.PropertyType).GetMethod;

                        var getMethodIL = getMethod.GetILGenerator();
                        getMethodIL.Emit(OpCodes.Ldarg_0);
                        getMethodIL.Emit(OpCodes.Ldfld, targetField);
                        EmitArgumentLoad(getMethodIL, indexParameters, 1);
                        getMethodIL.Emit(OpCodes.Callvirt, sourceProperty.GetGetMethod());
                        getMethodIL.Emit(OpCodes.Ret);

                        property.SetGetMethod(getMethod);
                    }

                    if (sourceProperty.CanWrite)
                    {
                        var setMethod = typeBuilder.DefineMethod(
                            "set_" + property.Name,
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                            CallingConventions.HasThis,
                            typeof(void),
                            new Type[] { sourceProperty.PropertyType }.Concat(indexParameters).ToArray()
                        );

                        // Since we invoke members through the interface map, we do not need this here
                        // var ix = Array.IndexOf(ifmap.InterfaceMethods, sourceProperty.GetSetMethod());
                        // var destMethod = ifmap.TargetMethods[ix];
                        //var destMethod = typeof(Bus).GetProperty(sourceProperty.Name, sourceProperty.PropertyType).SetMethod;

                        var setMethodIL = setMethod.GetILGenerator();
                        setMethodIL.Emit(OpCodes.Ldfld, targetField);
                        EmitArgumentLoad(setMethodIL, indexParameters, 1);
                        setMethodIL.Emit(OpCodes.Callvirt, sourceProperty.GetSetMethod());
                        setMethodIL.Emit(OpCodes.Ret);

                        property.SetSetMethod(setMethod);
                    }
                }

                // For calls to other properties we call Read and Write
                foreach (var sourceProperty in @interface.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(x => !basenames.ContainsKey(x.Name)))
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
                            "get_" + property.Name,
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
                            "set_" + property.Name,
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

            return (IBus)Activator.CreateInstance(_interfaceCache[@interface], new object[] { new Bus(@interface, clock, isClocked, isInternal) });

        }

        /// <summary>
        /// Loads all arguments onto the stack
        /// </summary>
        /// <param name="generator">The IL generator to use.</param>
        /// <param name="parameterTypes">The parameters to load</param>
        /// <param name="offset">The argument offset, should be <c>1</c> if the method is not static.</param>
        private static void EmitArgumentLoad(ILGenerator generator, Type[] parameterTypes, int offset)
        {
            if (parameterTypes == null || parameterTypes.Length == 0)
                return;

            for (var i = 0; i < parameterTypes.Length; i++)
                EmitLdarg(generator, i + offset);
        }

        /// <summary>
        /// Helper method to emit a argument load,
        /// using a short opcode if possible
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