﻿using System;
using System.Linq;
using SME.AST;

namespace SME.VHDL.CustomRenders.Inferred
{
    public class SinglePortRam : ICustomRenderer
    {
        public string IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return string.Empty;
        }

        public string BodyRegion(RenderStateProcess renderer, int indentation)
        {
            var initialdata = (Array)renderer.Process.SharedVariables.First(x => x.Name == "m_memory").DefaultValue;
            var size = initialdata.Length;
            var datawidth = VHDLHelper.GetBitWidthFromType(initialdata.GetType().GetElementType());
            var addrwidth = (int)Math.Ceiling(Math.Log(initialdata.Length, 2));

            var datavhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.First().Signals.First(x => x.Name == nameof(SME.Components.SinglePortMemory<int>.IControl.Data))];
            var addrvhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.First().Signals.First(x => x.Name == nameof(SME.Components.SinglePortMemory<int>.IControl.Address))];

            var control_bus_data_name = $"{ nameof(SME.Components.SinglePortMemory<int>.Control) }_{ nameof(SME.Components.SinglePortMemory<int>.IControl.Data) }";
            var control_bus_addr_name = $"{ nameof(SME.Components.SinglePortMemory<int>.Control) }_{ nameof(SME.Components.SinglePortMemory<int>.IControl.Address) }";
            var control_bus_enabled_name = $"{ nameof(SME.Components.SinglePortMemory<int>.Control) }_{ nameof(SME.Components.SinglePortMemory<int>.IControl.Enabled) }";
            var control_bus_iswriting_name = $"{ nameof(SME.Components.SinglePortMemory<int>.Control) }_{ nameof(SME.Components.SinglePortMemory<int>.IControl.IsWriting) }";

            var readresult_bus_data_name = $"{nameof(SME.Components.SinglePortMemory<int>.ReadResult)}_{ nameof(SME.Components.SinglePortMemory<int>.IReadResult.Data) }";

            var asm_write = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        control_bus_data_name + "_Vector"
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        control_bus_data_name
                    )
                )
            );

            var asm_read = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresult_bus_data_name
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresult_bus_data_name + "_Vector"
                    )
                )
            );

            // Assign the vhdl types so the type conversion is done correctly
            renderer.Parent.TypeLookup[asm_write.Left]
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write.Left).Target]
             = renderer.Parent.TypeLookup[asm_read.Right] 
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read.Right).Target]
                = renderer.Parent.TypeScope.GetStdLogicVector(datawidth);

            renderer.Parent.TypeLookup[asm_write.Right]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write.Right).Target]
            = renderer.Parent.TypeLookup[asm_read.Left]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read.Left).Target]
                = datavhdltype;

            var transformer = new Transformations.InjectTypeConversions(renderer.Parent, null);

            var asm_write_stm = new AST.ExpressionStatement(asm_write);
            asm_write_stm.UpdateParents();
            var asm_read_stm = new AST.ExpressionStatement(asm_read);
            asm_read_stm.UpdateParents();

            transformer.Transform(asm_write);
            transformer.Transform(asm_read);


            var template = $@"
    type ram_type is array ({size - 1} downto 0) of std_logic_vector ({datawidth - 1} downto 0);
    signal RAM : ram_type := { VHDLHelper.GetArrayAsAssignmentList(initialdata, inverse: true) };
    signal { control_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { readresult_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { control_bus_addr_name }_Vector: std_logic_vector ({addrwidth - 1} downto 0);
begin 

    process (CLK)
    begin
        if (CLK'event and CLK = '1') then
            if ({ control_bus_enabled_name } = '1') then
                { readresult_bus_data_name }_Vector <= RAM(to_integer(unsigned({ control_bus_addr_name }_Vector)));

                if ({ control_bus_iswriting_name } = '1') then
                    RAM(to_integer(unsigned({ control_bus_addr_name }_Vector))) <= { control_bus_data_name }_Vector;
                end if;
            end if;
        end if;
    end process;

    {Naming.ProcessNameToValidName(renderer.Process.SourceInstance.Instance)}_Helper: process(RST, CLK, RDY)
    begin
    if RST = '1' then
        FIN <= '0';                        
    elsif rising_edge(CLK) then
        FIN <= not RDY;
    end if;
    end process;

    { control_bus_addr_name }_Vector <= STD_LOGIC_VECTOR(resize(unsigned({ control_bus_addr_name }), {addrwidth}));
    {renderer.Helper.RenderExpression(asm_write_stm.Expression)};
    {renderer.Helper.RenderExpression(asm_read_stm.Expression)};

";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }

    }
}
