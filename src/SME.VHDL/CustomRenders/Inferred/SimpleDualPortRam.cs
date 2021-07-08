using System;
using System.Linq;

namespace SME.VHDL.CustomRenders.Inferred
{
    /// <summary>
    /// Custom renderer for simple dual port RAM.
    /// </summary>
    public class SimpleDualPortRam : ICustomRenderer
    {
        /// <summary>
        /// Returns the string, which should be written in the include region of the VHDL file.
        /// </summary>
        /// <param name="renderer">The renderer currently rendering VHDL files.</param>
        /// <param name="indentation">The indentation at the current location in the VHDL file.</param>
        public string IncludeRegion(RenderStateProcess renderer, int indentation)
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the string, which should be written in the body region of the VHDL file.
        /// </summary>
        /// <param name="renderer">The renderer currently rendering VHDL files.</param>
        /// <param name="indentation">The indentation at the current location in the VHDL file.</param>
        public string BodyRegion(RenderStateProcess renderer, int indentation)
        {
            var initialdata = (Array)renderer.Process.SharedVariables.First(x => x.Name == "m_memory").DefaultValue;
            var size = initialdata.Length;
            var datawidth = VHDLHelper.GetBitWidthFromType(initialdata.GetType().GetElementType());
            var addrwidth = (int)Math.Ceiling(Math.Log(initialdata.Length, 2));

            var datavhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Data))];
            var addrvhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Address))];

            var read_control_addr_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.ReadControl) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IReadControl.Address) }";
            var read_control_enabled_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.ReadControl) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IReadControl.Enabled) }";
            var read_result_data_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.ReadResult) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IReadResult.Data) }";

            var write_control_addr_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.WriteControl) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Address) }";
            var write_control_data_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.WriteControl) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Data) }";
            var write_control_enabled_name = $"{ nameof(SME.Components.SimpleDualPortMemory<int>.WriteControl) }_{ nameof(SME.Components.SimpleDualPortMemory<int>.IWriteControl.Enabled) }";

            var asm_write = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        write_control_data_name + "_Vector"
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        write_control_data_name
                    )
                )
            );

            var asm_read = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        read_result_data_name
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        read_result_data_name + "_Vector"
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
            var asm_read_stm = new AST.ExpressionStatement(asm_read);

            transformer.Transform(asm_write);
            transformer.Transform(asm_read);

            // TODO double check that this does not break inferrence
            // TODO The same transformations should also be applied to the other RAM types
            var template = $@"
    type ram_type is array (reset_m_memory'range) of std_logic_vector ({datawidth - 1} downto 0);
    function load_reset_memory return ram_type is
        variable tmp_arr : ram_type;
    begin
        for i in reset_m_memory'range loop
            tmp_arr(i) := std_logic_vector(reset_m_memory(i));
        end loop;
        return tmp_arr;
    end load_reset_memory;
    signal RAM : ram_type := load_reset_memory;
    signal { read_result_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { write_control_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal FIN_A : std_logic;
    signal FIN_B : std_logic;
begin

    process (CLK)
    begin
        if RST = '1' then
            FIN_A <= '0';
        elsif rising_edge(CLK) then
            if ({ read_control_enabled_name } = '1') then
                { read_result_data_name }_Vector <= RAM(to_integer(unsigned({ read_control_addr_name })));
            end if;
            FIN_A <= RDY;
        end if;
    end process;

    process (CLK)
    begin
        if RST = '1' then
            FIN_B <= '0';
        elsif rising_edge(CLK) then
            if ({ write_control_enabled_name } = '1') then
               RAM(to_integer(unsigned({ write_control_addr_name }))) <= { write_control_data_name }_Vector;
            end if;
            FIN_B <= RDY;
        end if;
    end process;

    {Naming.ProcessNameToValidName(renderer.Process.SourceInstance.Instance)}_Helper: process(RST, CLK, RDY)
    begin
        if RST = '1' then
            FIN <= '0';
        elsif FIN_A = FIN_B then
            FIN <= not RDY;
        end if;
    end process;

    {renderer.Helper.RenderExpression(asm_write_stm.Expression)};
    {renderer.Helper.RenderExpression(asm_read_stm.Expression)};

";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
