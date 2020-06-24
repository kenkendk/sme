using System;
using SME.AST;
using Microsoft.CodeAnalysis;

namespace SME.CPP
{
    public class CppTypeScope
    {
        /// <summary>
        /// The Mono.Cecil module definition
        /// </summary>
        private readonly IAssemblySymbol m_resolveModule;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.CPP.CppTypeScope"/> class.
        /// </summary>
        /// <param name="resolveModule">The module used to resolve types.</param>
        public CppTypeScope(IAssemblySymbol resolveModule)
        {
            m_resolveModule = resolveModule;
        }
        /// <summary>
        /// Gets the Cpp type for the signal
        /// </summary>
        /// <returns>The cpp type.</returns>
        /// <param name="signal">The input signal.</param>
        public CppType GetType(AST.Signal signal)
        {
            return GetType(signal.MSCAType);
        }
        /// <summary>
        /// Gets the Cpp type for the signal
        /// </summary>
        /// <returns>The cpp type.</returns>
        /// <param name="element">The input signal.</param>
        public CppType GetType(AST.DataElement element)
        {
            return GetType(element.MSCAType);
        }

        /// <summary>
        /// Gets the Cpp type for the variable
        /// </summary>
        /// <returns>The cpp type.</returns>
        /// <param name="signal">The input variable.</param>
        public CppType GetType(AST.Variable signal)
        {
            return GetType(signal.MSCAType);
        }

        /// <summary>
        /// Gets the Cpp type for the parameter
        /// </summary>
        /// <returns>The cpp type.</returns>
        /// <param name="parameter">The input parameter.</param>
        public CppType GetType(AST.Parameter parameter)
        {
            return GetType(parameter.MSCAType);
        }

        /// <summary>
        /// Gets the Cpp type for the Cecil input type.
        /// </summary>
        /// <returns>The cpp type.</returns>
        /// <param name="sourcetype">The Cecil sourcetype.</param>
        public CppType GetType(ITypeSymbol sourcetype)
        {
            if (sourcetype.IsArrayType())
            {
                var eltype = GetType(sourcetype.GetArrayElementType());
                return new CppType()
                {
                    Name = eltype.Name + "*",
                    IsArray = true,
                    ElementName = eltype.Name
                };
            }

            if (sourcetype.IsSameTypeReference<bool>())
                return CppTypes.BOOL;
            if (sourcetype.IsSameTypeReference<byte>())
                return CppTypes.UINT8;
            if (sourcetype.IsSameTypeReference<sbyte>())
                return CppTypes.INT8;
            if (sourcetype.IsSameTypeReference<short>())
                return CppTypes.INT16;
            if (sourcetype.IsSameTypeReference<ushort>())
                return CppTypes.UINT16;
            if (sourcetype.IsSameTypeReference<int>())
                return CppTypes.INT32;
            if (sourcetype.IsSameTypeReference<uint>())
                return CppTypes.UINT32;
            if (sourcetype.IsSameTypeReference<long>())
                return CppTypes.INT64;
            if (sourcetype.IsSameTypeReference<ulong>())
                return CppTypes.UINT64;

            if (sourcetype.IsSameTypeReference(typeof(IntPtr)))
            {
                if (IntPtr.Size == 4)
                    return CppTypes.INT32;
                else if (IntPtr.Size == 8)
                    return CppTypes.INT64;
            }

            if (sourcetype.IsSameTypeReference(typeof(UIntPtr)))
            {
                if (IntPtr.Size == 4)
                    return CppTypes.UINT32;
                else if (IntPtr.Size == 8)
                    return CppTypes.UINT64;
            }

            throw new Exception($"Unexpected source type: {sourcetype}");
        }

    }
}
