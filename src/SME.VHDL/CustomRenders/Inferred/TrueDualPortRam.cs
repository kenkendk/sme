using System;
using System.Linq;

namespace SME.VHDL.CustomRenders.Inferred
{
    public class TrueDualPortRam : ICustomRenderer
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

            var datavhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.TrueDualPortMemory<int>.IControlA.Data))];
            var addrvhdltype = renderer.Parent.TypeLookup[renderer.Process.InputBusses.SelectMany(x => x.Signals).First(x => x.Name == nameof(SME.Components.TrueDualPortMemory<int>.IControlA.Address))];

            var controla_bus_data_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlA.Data) }";
            var controla_bus_addr_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlA.Address) }";
            var controla_bus_enabled_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlA.Enabled) }";
            var controla_bus_iswriting_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlA) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlA.IsWriting) }";

            var controlb_bus_data_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlB.Data) }";
            var controlb_bus_addr_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlB.Address) }";
            var controlb_bus_enabled_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlB.Enabled) }";
            var controlb_bus_iswriting_name = $"{ nameof(SME.Components.TrueDualPortMemory<int>.ControlB) }_{ nameof(SME.Components.TrueDualPortMemory<int>.IControlB.IsWriting) }";

            var readresulta_bus_data_name = $"{nameof(SME.Components.TrueDualPortMemory<int>.ReadResultA)}_{ nameof(SME.Components.TrueDualPortMemory<int>.IReadResultA.Data) }";
            var readresultb_bus_data_name = $"{nameof(SME.Components.TrueDualPortMemory<int>.ReadResultB)}_{ nameof(SME.Components.TrueDualPortMemory<int>.IReadResultB.Data) }";

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
    type ram_type is array ({size - 1} downto 0) of std_logic_vector ({datawidth - 1} downto 0);
    shared variable RAM : ram_type := { VHDLHelper.GetArrayAsAssignmentList(initialdata, inverse: true) };
    signal { controla_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { controlb_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { readresulta_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { readresultb_bus_data_name }_Vector: std_logic_vector ({datawidth - 1} downto 0);
    signal { controla_bus_addr_name }_Vector: std_logic_vector ({addrwidth - 1} downto 0);
    signal { controlb_bus_addr_name }_Vector: std_logic_vector ({addrwidth - 1} downto 0);
begin 

    process (CLK)
    begin
        if (CLK'event and CLK = '1') then
            if ({ controla_bus_enabled_name } = '1') then
                { readresulta_bus_data_name }_Vector <= RAM(to_integer(unsigned({ controla_bus_addr_name }_Vector)));

                if ({ controla_bus_iswriting_name } = '1') then
                    RAM(to_integer(unsigned({ controla_bus_addr_name }_Vector))) := { controla_bus_data_name }_Vector;
                end if;
            end if;
        end if;
    end process;

    process (CLK)
    begin
        if (CLK'event and CLK = '1') then
            if ({ controlb_bus_enabled_name } = '1') then
                { readresultb_bus_data_name }_Vector <= RAM(to_integer(unsigned({ controlb_bus_addr_name }_Vector)));

                if ({ controlb_bus_iswriting_name } = '1') then
                    RAM(to_integer(unsigned({ controlb_bus_addr_name }_Vector))) := { controlb_bus_data_name }_Vector;
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

    { controla_bus_addr_name }_Vector <= STD_LOGIC_VECTOR(resize(unsigned({ controla_bus_addr_name }), {addrwidth}));
    { controlb_bus_addr_name }_Vector <= STD_LOGIC_VECTOR(resize(unsigned({ controlb_bus_addr_name }), {addrwidth}));
    { renderer.Helper.RenderExpression(asm_write_a_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_read_a_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_write_b_stm.Expression) };
    { renderer.Helper.RenderExpression(asm_read_b_stm.Expression) };
";

            return VHDLHelper.ReIndentTemplate(template, indentation);
        }
    }
}
