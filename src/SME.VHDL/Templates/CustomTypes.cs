using System;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for writing the custom types of the network.
    /// </summary>
    public class CustomTypes : BaseTemplate
    {
        /// <summary>
        /// The network containing the custom types.
        /// </summary>
        public readonly Network Network;
        /// <summary>
        /// The current render state.
        /// </summary>
        public readonly RenderState RS;

        /// <summary>
        /// Constructs a new instance of the custom type template class.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        public CustomTypes(RenderState renderer)
        {
            RS = renderer;
            Network = renderer.Network;
        }

        /// <summary>
        /// Writes the template to the VHDL file.
        /// </summary>
        public override string TransformText()
        {
            GenerationEnvironment = null;

            Write(
@"library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

-- library SYSTEM_TYPES;
use work.SYSTEM_TYPES.ALL;

-- User defined packages here
-- #### USER-DATA-IMPORTS-START
-- #### USER-DATA-IMPORTS-END

package CUSTOM_TYPES is

    -- User defined types here
    -- #### USER-DATA-CORETYPES-START
    -- #### USER-DATA-CORETYPES-END

");

            if (RS.CustomTypes.Any())
            {
                Write("    -- Custom types for processes\n");
                foreach (var t in RS.CustomTypes)
                {
                    Write($"    type {ToStringHelper.ToStringWithCulture(t.ToSafeVHDLName())} is (\n");
                    var members = RS.ListMembers(t)
                        .Select(x => $"        {ToStringHelper.ToStringWithCulture(x)}");
                    Write(string.Join(",\n", members));
                    Write("\n    );\n\n");
                }
            }

            if (RS.TypeDefinitions.Any())
            {
                Write("    -- Type definitions\n");
                foreach (var t in RS.TypeDefinitions)
                    Write($"    {ToStringHelper.ToStringWithCulture(t)};\n");
                Write("\n");
            }

            if (RS.BusArrays.Any())
            {
                Write("    -- Bus array definitions\n");
                foreach (var signal in RS.BusArrays)
                {
                    var vhdltype = RS.VHDLType(signal);
                    var elementtype = RS.TypeScope.GetByName(vhdltype.ElementName);
                    var bus = signal.Parent as AST.Bus;
                    var arraylength = RS.GetArrayLength(signal);

                    var busname = ToStringHelper.ToStringWithCulture(bus.Name);
                    var signalname = ToStringHelper.ToStringWithCulture(signal.Name);
                    var elementname = ToStringHelper.ToStringWithCulture(elementtype.ToSafeVHDLName());

                    if (elementtype.IsSystemType)
                    {
                        var arrlen = ToStringHelper.ToStringWithCulture(arraylength);
                        Write($"    subtype {busname}_{signalname}_type is {elementname}_ARRAY(0 to {arrlen} - 1);\n");
                    }
                    else if (RS.Config.USE_EXPLICIT_LITERAL_ARRAY_LENGTH)
                    {
                        var arrlen = ToStringHelper.ToStringWithCulture(arraylength - 1);
                        Write($"    type {busname}_{signalname}_type is array of {elementname};\n");
                    }
                    else
                    {
                        var arrlen = ToStringHelper.ToStringWithCulture(arraylength);
                        Write($"    type {busname}_{signalname}_type is array (0 to {arrlen} - 1) of {elementname};\n");
                    }
                }
                Write("\n");
            }

            if (RS.Constants.Any())
            {
                Write("    -- Constant definitions\n");
                foreach (var c in RS.Constants)
                    Write($"    {ToStringHelper.ToStringWithCulture(c)};\n");
                Write("\n");
            }

            var irregural_enums = RS.EnumTypes.Where(x => x.IsIrregularEnum);
            if (irregural_enums.Any())
            {
                Write("    -- Functions for converting enums to/from integer\n");
                foreach (var enumtype in irregural_enums)
                {
                    var enumname = ToStringHelper.ToStringWithCulture(enumtype.ToSafeVHDLName());
                    Write($"    pure function fromValue_{enumname}(v: INTEGER) return {enumname};\n");
                    Write($"    pure function toValue_{enumname}(v: {enumname}) return INTEGER;\n");
                }
                Write("\n");
            }

            if (RS.EnumTypes.Any())
            {
                Write("    -- Functions for converting enums to string\n");
                foreach (var enumtype in RS.EnumTypes)
                    Write($"    pure function str(b: {ToStringHelper.ToStringWithCulture(enumtype.ToSafeVHDLName())}) return string;\n");
                Write("\n");
            }

            Write(
@"    -- User defined types here
    -- #### USER-DATA-TRAILTYPES-START
    -- #### USER-DATA-TRAILTYPES-END

end CUSTOM_TYPES;

package body CUSTOM_TYPES is

");

            if (RS.EnumTypes.Any())
            {
                foreach (var enumtype in RS.EnumTypes)
                {
                    var enumname = ToStringHelper.ToStringWithCulture(enumtype);
                    var vhdltype = ToStringHelper.ToStringWithCulture(enumtype.ToSafeVHDLName());
                    Write($"    -- converts {enumname} into a string\n");
                    Write($"    pure function str(b: {vhdltype}) return string is\n");
                    Write($"    begin\n");
                    Write($"        return {vhdltype}\'image(b);\n");
                    Write($"    end str;\n\n");

                    if (enumtype.IsIrregularEnum)
                    {
                        Write($"    -- Converts an integer to {vhdltype}\n");
                        Write($"    pure function fromValue_{vhdltype}(v: INTEGER) return {vhdltype} is\n");
                        Write($"    begin\n");
                        Write($"        case v is\n");

                        foreach (var f in RS.GetEnumValues(enumtype))
                        {
                            var val = ToStringHelper.ToStringWithCulture(f.Value);
                            var key = ToStringHelper.ToStringWithCulture(f.Key);
                            Write($"            when {val} => return {key};\n");
                        }

                        var first = ToStringHelper.ToStringWithCulture(RS.GetEnumValues(enumtype).First().Key);
                        Write($"            when others => return {first};\n");
                        Write($"        end case;\n");
                        Write($"    end fromValue_{vhdltype};\n\n");
                        Write($"    -- Converts a {vhdltype} to an integer\n");
                        Write($"    pure function toValue_{vhdltype}(v: {vhdltype}) return INTEGER is\n");
                        Write($"    begin\n");
                        Write($"        case v is\n");

                        foreach (var f in RS.GetEnumValues(enumtype))
                        {
                            var key = ToStringHelper.ToStringWithCulture(f.Key);
                            var val = ToStringHelper.ToStringWithCulture(f.Value);
                            Write($"            when {key} => return {val};\n");
                        }

                        first = ToStringHelper.ToStringWithCulture(RS.GetEnumValues(enumtype).First().Value);
                        Write($"            when others => return {first};\n");
                        Write($"        end case;\n");
                        Write($"    end toValue_{vhdltype};\n\n");
                    }
                }
            }

                Write(
@"    -- User defined bodies here
    -- #### USER-DATA-BODY-START
    -- #### USER-DATA-BODY-END

end CUSTOM_TYPES;");


            return GenerationEnvironment.ToString();
        }
    }
}
