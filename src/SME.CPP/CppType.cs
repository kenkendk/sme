using System;
using Microsoft.CodeAnalysis;

namespace SME.CPP
{
    /// <summary>
    /// Representation of a VHDL type
    /// </summary>
    public class CppType
    {
        /// <summary>
        /// Gets or sets a value indicating if the type is an array
        /// </summary>
        public bool IsArray { get; set; }
        /// <summary>
        /// Gets or sets the primary CPP name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the name of the elements in the array
        /// </summary>
        public string ElementName { get; set; }
        /// <summary>
        /// Gets or sets the source CeCiL type
        /// </summary>
        public ITypeSymbol SourceType { get; set; }
        /// <summary>
        /// Gets or sets the length of the array
        /// </summary>
        public int Length { get; set; }
    }


    /// <summary>
    /// Basic VHDL types
    /// </summary>
    public static class CppTypes
    {
        /// <summary>
        /// The UINT 8 UNSIGNED type
        /// </summary>
        public static readonly CppType UINT8 = new CppType()
        {
            Name = "system_uint8",
            IsArray = false,
        };

        /// <summary>
        /// The UINT 16 UNSIGNED type
        /// </summary>
        public static readonly CppType UINT16 = new CppType()
        {
            Name = "system_uint16",
            IsArray = false
        };

        /// <summary>
        /// The UINT 32 UNSIGNED type
        /// </summary>
        public static readonly CppType UINT32 = new CppType()
        {
            Name = "system_uint32",
            IsArray = false
        };

        /// <summary>
        /// The UINT 64 UNSIGNED type
        /// </summary>
        public static readonly CppType UINT64 = new CppType()
        {
            Name = "system_uint64",
            IsArray = false
        };

        /// <summary>
        /// The INT 8 SIGNED type
        /// </summary>
        public static readonly CppType INT8 = new CppType()
        {
            Name = "system_int8",
            IsArray = false
        };

        /// <summary>
        /// The INT 16 SIGNED type
        /// </summary>
        public static readonly CppType INT16 = new CppType()
        {
            Name = "system_int16",
            IsArray = false
        };

        /// <summary>
        /// The INT 32 SIGNED type
        /// </summary>
        public static readonly CppType INT32 = new CppType()
        {
            Name = "system_int32",
            IsArray = false
        };

        /// <summary>
        /// The INT 64 SIGNED type
        /// </summary>
        public static readonly CppType INT64 = new CppType()
        {
            Name = "system_int64",
            IsArray = false
        };

        /// <summary>
        /// The BOOLEAN type
        /// </summary>
        public static readonly CppType BOOL = new CppType()
        {
            Name = "system_bool",
            IsArray = false
        };

    }
}
