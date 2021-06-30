using System;
using System.Linq;

namespace SME.VHDL.CustomRenders.Inferred
{
    /// <summary>
    /// Custom renderer for true dual port RAM.
    /// </summary>
    public class TrueDualPortRam : ICustomRenderer
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

            var datavhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.TrueDualPortMemory<int>.IControl.Data))];
            var addrvhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.TrueDualPortMemory<int>.IControl.Address))];

            var controla_bus_data_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Data) }";
            var controla_bus_addr_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Address) }";
            var controla_bus_enabled_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Enabled) }";
            var controla_bus_iswriting_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.IsWriting) }";

            var controlb_bus_data_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Data) }";
            var controlb_bus_addr_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Address) }";
            var controlb_bus_enabled_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.Enabled) }";
            var controlb_bus_iswriting_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControl.IsWriting) }";

            var readresulta_bus_data_name = $"{nameof(SME.Components.TrueDualPortMemory<int>.ReadResultA)}_{ nameof(SME.Components.TrueDualPortMemory<int>.IReadResult.Data) }";
            var readresultb_bus_data_name = $"{nameof(SME.Components.TrueDualPortMemory<int>.ReadResultB)}_{ nameof(SME.Components.TrueDualPortMemory<int>.IReadResult.Data) }";

            var asm_write_a = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        controla_bus_data_name + "_Vector"
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        controla_bus_data_name
                    )
                )
            );

            var asm_write_b = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        controlb_bus_data_name + "_Vector"
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        controlb_bus_data_name
                    )
                )
            );

            var asm_read_a = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresulta_bus_data_name
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresulta_bus_data_name + "_Vector"
                    )
                )
            );

            var asm_read_b = new AST.AssignmentExpression(
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresultb_bus_data_name
                    )
                ),
                new AST.IdentifierExpression(
                    new AST.Signal(
                        readresultb_bus_data_name + "_Vector"
                    )
                )
            );

            // Assign the vhdl types so the type conversion is done correctly
            renderer.Parent.TypeLookup[asm_write_a.Left]
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write_a.Left).Target]
             = renderer.Parent.TypeLookup[asm_write_b.Left]
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write_b.Left).Target]
             = renderer.Parent.TypeLookup[asm_read_a.Right]
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read_a.Right).Target]
             = renderer.Parent.TypeLookup[asm_read_b.Right]
             = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read_b.Right).Target]
                = renderer.Parent.TypeScope.GetStdLogicVector(datawidth);

            renderer.Parent.TypeLookup[asm_write_a.Right]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write_a.Right).Target]
            = renderer.Parent.TypeLookup[asm_read_a.Left]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read_a.Left).Target]
            = renderer.Parent.TypeLookup[asm_write_b.Right]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_write_b.Right).Target]
            = renderer.Parent.TypeLookup[asm_read_b.Left]
            = renderer.Parent.TypeLookup[((AST.IdentifierExpression)asm_read_b.Left).Target]
                = datavhdltype;

            var transformer = new Transformations.InjectTypeConversions(renderer.Parent, null);

            var asm_write_a_stm = new AST.ExpressionStatement(asm_write_a);
            var asm_write_b_stm = new AST.ExpressionStatement(asm_write_b);
            var asm_read_a_stm = new AST.ExpressionStatement(asm_read_a);
            var asm_read_b_stm = new AST.ExpressionStatement(asm_read_b);

            transformer.Transform(asm_write_a);
            transformer.Transform(asm_write_b);
            transformer.Transform(asm_read_a);
            transformer.Transform(asm_read_b);


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
    signal { controla_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { controlb_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { readresulta_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { readresultb_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal FIN_A : std_logic;
    signal FIN_B : std_logic;
begin

    process (CLK, RST)
    begin
        if RST = '1' then
            { readresulta_bus_data_name }_Vector <= (others => '0');
        elsif rising_edge(CLK) then
            if ({ controla_bus_enabled_name } = '1') then
                { readresulta_bus_data_name }_Vector <= RAM(to_integer({ controla_bus_addr_name }));
            end if;
        end if;

        if RST = '1' then
            { readresultb_bus_data_name }_Vector <= (others => '0');
        elsif rising_edge(CLK) then
            if ({ controlb_bus_enabled_name } = '1') then
                { readresultb_bus_data_name }_Vector <= RAM(to_integer({ controlb_bus_addr_name }));
            end if;
        end if;

        if RST = '1' then
            FIN_A <= '0';
        elsif rising_edge(CLK) then
            if ({ controla_bus_enabled_name } = '1') and ({ controla_bus_iswriting_name } = '1') then
                RAM(to_integer({ controla_bus_addr_name })) <= { controla_bus_data_name }_Vector;
            end if;
            FIN_A <= not RDY;
        end if;

        if RST = '1' then
            FIN_B <= '0';
        elsif rising_edge(CLK) then
            if ({ controlb_bus_enabled_name } = '1') and ({ controlb_bus_iswriting_name } = '1') then
                RAM(to_integer({ controlb_bus_addr_name })) <= { controlb_bus_data_name }_Vector;
            end if;
            FIN_B <= not RDY;
        end if;
    end process;

    {Naming.ProcessNameToValidName(renderer.Process.SourceInstance.Instance)}_Helper: process(RST, FIN_A, FIN_B)
    begin
        if RST = '1' then
            FIN <= '0';
        elsif FIN_A = FIN_B then
            FIN <= FIN_A;
        end if;
    end process;

    { renderer.Helper.RenderExpression(asm_write_a_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_read_a_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_write_b_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_read_b_stm.Expression) };
";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
