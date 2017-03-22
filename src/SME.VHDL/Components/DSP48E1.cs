using System;
using SME.VHDL;

namespace SME.VHDL.Components
{
	public abstract class DSP48E1<TA, TB, TC, TD, TOut> : SimpleProcess, IVHDLComponent
	{
		protected abstract void Setup(Clock clock);

		protected abstract int InputAWidth { get; }
		protected abstract int InputBWidth { get; }
		protected abstract int InputCWidth { get; }
		protected abstract int InputDWidth { get; }
		protected abstract int OutputWidth { get; }

		protected abstract ConfigParams DSPConfig { get; }

		public interface IInput : IBus
		{
			TA A { get; set; }
			TA ACIN { get; set; }

			UInt4 ALUMODE { get; set; }

			TB B { get; set; }
			TB BCIN { get; set; }

			TC C { get; set; }

			bool CARRYASCIN { get; set; }

			bool CARRYIN { get; set; }
			UInt2 CARRYINSEL { get; set; }

			bool CEAD { get; set; }
			bool CEALUMODE { get; set; }

			bool CEA1 { get; set; }
			bool CEA2 { get; set; }
			bool CEB1 { get; set; }
			bool CEB2 { get; set; }

			bool CEC { get; set; }
			bool CECARRYIN { get; set; }
			bool CECTRL { get; set; }

			bool CED { get; set; }
			bool CEINMODE { get; set; }
			bool CEM { get; set; }

			bool CEP { get; set; }
			bool CLK { get; set; }

			TD D { get; set; }

			UInt5 INMODE { get; set; }

			bool MULTISIGNIN { get; set; }

			UInt7 OPMODE { get; set; }

			UInt48 PCIN { get; set; }

			bool RSTA { get; set; }
			bool RSTALLCARRYIN { get; set; }
			bool RSTALUMODE { get; set; }
			bool RSTB { get; set; }
			bool RSTC { get; set; }
			bool RSTCTRL { get; set; }
			bool RSTD { get; set; }
			bool RSTINMODE { get; set; }
			bool RSTM { get; set; }
			bool RSTP { get; set; }
		}

		public interface IOutput : IBus
		{
			TA ACOUT { get; set; }

			TB BCOUT { get; set; }

			bool CARRYCASCOUT { get; set; }

			UInt4 CARRYOUT { get; set; }

			bool MULTISIGNOUT { get; set; }
			bool OVERFLOW { get; set; }

			TOut P { get; set; }

			bool PATTERNBDETEXT { get; set; }
			bool PATERNDETECT { get; set; }

			UInt47 PCOUT { get; set; }

			bool UNDERFLOW { get; set; }
		}

		public DSP48E1()
			: this(Clock.DefaultClock)
		{
		}

		public DSP48E1(Clock clock)
			: base(clock)
		{
			Setup(clock);
		}

		private string FormatOutString(string componentname, int indentation, string str)
		{
			var ind = new string(' ', indentation);

			var zeroes_for_a = InputAWidth >= 30 ? "" : string.Format("'{0}' & ", new string('0', 30 - InputAWidth));
			var zeroes_for_b = InputBWidth >= 18 ? "" : string.Format("'{0}' & ", new string('0', 18 - InputBWidth));
			var zeroes_for_c = InputCWidth >= 48 ? "" : string.Format("'{0}' & ", new string('0', 48 - InputCWidth));
			var zeroes_for_d = InputDWidth >= 25 ? "" : string.Format("'{0}' & ", new string('0', 25 - InputDWidth));

			return ind + string.Join(Environment.NewLine + ind, string.Format(str.Trim(), Naming.ToValidName(this.GetType().Name), "DSP48E1", InputAWidth - 1, InputBWidth - 1, InputCWidth - 1, InputDWidth - 1, OutputWidth - 1, zeroes_for_a, zeroes_for_b, zeroes_for_c, zeroes_for_d).Replace("\t", new string(' ', 4)).Replace("\r", "").Split(new string[] { "\n" }, StringSplitOptions.None));
		}

		string IVHDLComponent.SignalRegion(string componentname, int indentation)
		{
			return "";
		}

		protected class ConfigParams
		{
			public enum InputSourceMode
			{
				Direct,
				Cascade
			}

			public enum MultiplierMode
			{
				Multiply,
				Dynamic,
				None
			}

			public enum SIMDMode
			{
				One48,
				Two24,
				Four12
			}

			public enum PatterDetectorAutoResetMode
			{
				No_Reset,
				Reset_Match,
				Reset_Not_Match
			}

			public enum SelectionMaskMode
			{
				C,
				Mask,
				Rounding_Mode1,
				Rounding_Mode2
			}

			public enum SelectionPatternMode
			{
				Pattern,
				C
			}

			public InputSourceMode A_Input;
			public InputSourceMode B_Input;
			public bool UseDPort;
			public MultiplierMode UseMultiplier;
			public SIMDMode UseSIMD;

			public PatterDetectorAutoResetMode AutoResetPatternDetect;
			public UInt48 Mask = 0x3fffffffffffu;
			public UInt48 Pattern;
			public SelectionMaskMode SelectionMask;
			public SelectionPatternMode SelectionPattern;
			public bool UsePatternDetect;

			public int ACASCREG = 1;
			public int ADREG = 1;
			public int ALUMODEREG = 1;
			public int AREG = 1;
			public int BCASCREG = 1;
			public int BREG = 1;
			public int CARRYINREG = 1;
			public int CARRYINSELREG = 1;
			public int CREG = 1;
			public int DREG = 1;
			public int INMODEREG = 1;
			public int MREG = 1;
			public int OPMODEREG = 1;
			public int PREG = 1;

		}

		string IVHDLComponent.ProcessRegion(string componentname, int indentation)
		{
			var cfg = DSPConfig;
			var configbody = string.Format(@"
   -- Feature Control Attributes: Data Path Selection
	A_INPUT =>               ""{0}"",           -- Selects A input source, ""DIRECT"" (A port) or ""CASCADE"" (ACIN port)
	B_INPUT =>               ""{1}"",           -- Selects B input source, ""DIRECT"" (B port) or ""CASCADE"" (BCIN port)
	USE_DPORT =>             {2},                -- Select D port usage (TRUE or FALSE)
	USE_MULT =>              ""{3}"",         -- Select multiplier usage (""MULTIPLY"", ""DYNAMIC"", or ""NONE"")
	USE_SIMD =>              ""{4}"",            -- SIMD selection (""ONE48"", ""TWO24"", ""FOUR12"")

	-- Pattern Detector Attributes: Pattern Detection Configuration
	AUTORESET_PATDET =>      ""{5}"",         -- ""NO_RESET"", ""RESET_MATCH"", ""RESET_NOT_MATCH""
	MASK =>                  X""{6:x12}"",    -- 48-bit mask value for pattern detect (1=ignore)
	PATTERN =>               X""{7:x12}"",    -- 48-bit pattern match for pattern detect
	SEL_MASK =>              ""{8}"",             -- ""C"", ""MASK"", ""ROUNDING_MODE1"", ""ROUNDING_MODE2""
	SEL_PATTERN =>           ""{9}"" ,         -- Select pattern value (""PATTERN"" or ""C"")
	USE_PATTERN_DETECT =>    ""{10}"",        -- Enable pattern detect (""PATDET"" or ""NO_PATDET"")

	-- Register Control Attributes: Pipeline Register Configuration
	ACASCREG =>              {11},                    -- Number of pipeline stages between A/ACIN and ACOUT (0, 1 or 2)
	ADREG =>                 {12},                    -- Number of pipeline stages for pre-adder (0 or 1)
	ALUMODEREG =>            {13},                    -- Number of pipeline stages for ALUMODE (0 or 1)
	AREG =>                  {14},                    -- Number of pipeline stages for A(0,1 or 2)
	BCASCREG =>              {15},                    -- Number of pipeline stages between B/BCIN and BCOUT (0, 1 or 2)
	BREG =>                  {16},                    -- Number of pipeline stages for B(0,1or 2)
	CARRYINREG =>            {17},                    -- Number of pipeline stages for CARRYIN (0 or 1)
	CARRYINSELREG =>         {18},                    -- Number of pipeline stages for CARRYINSEL (0 or 1)
	CREG =>                  {19},                    -- Number of pipeline stages for C (0 or 1)
	DREG =>                  {20},                    -- Number of pipeline stages for D (0 or 1)
	INMODEREG =>             {21},                    -- Number of pipeline stages for INMODE (0 or 1)
	MREG =>                  {22},                    -- Number of multiplier pipeline stages (0 or 1)
	OPMODEREG =>             {23},                    -- Number of pipeline stages for OPMODE (0 or 1)
	PREG =>                  {24}                     -- Number of pipeline stages for P (0 or 1)
",
			cfg.A_Input.ToString().ToUpper(),
			cfg.B_Input.ToString().ToUpper(),
			cfg.UseDPort ? "TRUE" : "FALSE",
			cfg.UseMultiplier.ToString().ToUpper(),
			cfg.UseSIMD.ToString().ToUpper(),
			cfg.AutoResetPatternDetect.ToString().ToUpper(),
			(ulong)cfg.Mask,
			(ulong)cfg.Pattern,
			cfg.SelectionMask.ToString().ToUpper(),
			cfg.SelectionPattern.ToString().ToUpper(),
			cfg.UsePatternDetect ? "PATDET" : "NO_PATDET",
			cfg.ACASCREG,
			cfg.ADREG,
			cfg.ALUMODEREG,
			cfg.AREG,
			cfg.BCASCREG,
			cfg.BREG,
			cfg.CARRYINREG,
			cfg.CARRYINSELREG,
			cfg.CREG,
			cfg.DREG,
			cfg.INMODEREG,
			cfg.MREG,
			cfg.OPMODEREG,
			cfg.PREG
		);


			return FormatOutString(componentname, indentation, @"
{0}_implementation: {1}
DSP48E1_inst : DSP48E1
generic map (
" + configbody + @"
)
port map (
-- Cascade: 30-bit (each) output: Cascade Ports
	ACOUT => {7}IOutput_ACOUT({2} DOWNTO 0), -- 30-bit output: A port cascade output
	BCOUT => {8}IOutput_BCOUT({3} DOWNTO 0), -- 18-bit output: B port cascade output
	CARRYCASCOUT => IInput_CARRYCASCOUT, -- 1-bit output: Cascade carry output
	MULTSIGNOUT => IOutput_MULTSIGNOUT, -- 1-bit output: Multiplier sign cascade output
	PCOUT => IOutput_PCOUT, -- 48-bit output: Cascade output

	-- Control: 1-bit (each) output: Control Inputs/Status Bits
	OVERFLOW => IOutput_OVERFLOW,             -- 1-bit output: Overflow in add/acc output
	PATTERNBDETECT => IOutput_PATTERNBDETECT, -- 1-bit output: Pattern bar detect output
	PATTERNDETECT => IOutput_PATTERNDETECT,   -- 1-bit output: Pattern detect output
	UNDERFLOW => IOutput_UNDERFLOW,           -- 1-bit output: Underflow in add/acc output

	-- Data: 4-bit (each) output: Data Ports
	CARRYOUT => IOutput_CARRYOUT,             -- 4-bit output: Carry output
	P => P,                           -- 48-bit output: Primary data output

	-- Cascade: 30-bit (each) input: Cascade Ports
	ACIN => {7}IInput_ACIN({2} DOWNTO 0), -- 30-bit input: A cascade data input
	BCIN => {8}IInput_BCIN({3} DOWNTO 0), -- 18-bit input: B cascade input
	CARRYCASCIN => IInput_CARRYCASCIN, -- 1-bit input: Cascade carry input
	MULTSIGNIN => IInput_MULTSIGNIN, -- 1-bit input: Multiplier sign input
	PCIN => IInput_PCIN, -- 48-bit input: P cascade input
	
	-- Control: 4-bit (each) input: Control Inputs/Status Bits
	ALUMODE => IInput_ALUMODE, -- 4-bit input: ALU control input
	CARRYINSEL => IInput_CARRYINSEL, -- 3-bit input: Carry select input
	CLK => CLK, -- 1-bit input: Clock input
	INMODE => IInput_INMODE, -- 5-bit input: INMODE control input
	OPMODE => IInput_OPMODE, -- 7-bit input: Operation mode input

	-- Data: 30-bit (each) input: Data Ports
	A=> {7}IInput_A({2} DOWNTO 0), -- 30-bit input: A data input
	B=> {8}IInput_B({3} DOWNTO 0), -- 18-bit input: B data input
	C=> {9}IInput_C({4} DOWNTO 0), -- 48-bit input: C data input
	CARRYIN => IInput_CARRYIN, -- 1-bit input: Carry input signal
	D=> {10}IInput_D({5} DOWNTO 0), -- 25-bit input: D data input

	-- Reset/Clock Enable: 1-bit (each) input: Reset/Clock Enable Inputs
	CEA1 => IInput_CEA1, -- 1-bit input: Clock enable input for 1st stage AREG
	CEA2 => IInput_CEA2, -- 1-bit input: Clock enable input for 2nd stage AREG
	CEAD => IInput_CEAD, -- 1-bit input: Clock enable input for ADREG
	
	CEALUMODE => IInput_CEALUMODE, -- 1-bit input: Clock enable input for ALUMODE
	CEB1 => IInput_CEB1, -- 1-bit input: Clock enable input for 1st stage BREG
	CEB2 => IInput_CEB2, -- 1-bit input: Clock enable input for 2nd stage BREG
	CEC => IInput_CEC, -- 1-bit input: Clock enable input for CREG
	CECARRYIN => IInput_CECARRYIN, -- 1-bit input: Clock enable input for CARRYINREG
	CECTRL => IInput_CECTRL, -- 1-bit input: Clock enable input for OPMODEREG and CARRYINSELREG
	CED => IInput_CED, -- 1-bit input: Clock enable input for DREG
	CEINMODE => IInput_CEINMODE, -- 1-bit input: Clock enable input for INMODEREG
	CEM => IInput_CEM, -- 1-bit input: Clock enable input for MREG
	CEP => IInput_CEP, -- 1-bit input: Clock enable input for PREG
	RSTA => IInput_RSTA, -- 1-bit input: Reset input for AREG
	RSTALLCARRYIN => IInput_RSTALLCARRYIN, -- 1-bit input: Reset input for CARRYINREG
	RSTALUMODE => IInput_RSTALUMODE, -- 1-bit input: Reset input for ALUMODEREG
	RSTB => IInput_RSTB, -- 1-bit input: Reset input for BREG
	RSTC => IInput_RSTC, -- 1-bit input: Reset input for CREG
	RSTCTRL => IInput_RSTCTRL, -- 1-bit input: Reset input for OPMODEREG and CARRYINSELREG
	RSTD => IInput_RSTD, -- 1-bit input: Reset input for DREG and ADREG
	RSTINMODE => IInput_RSTINMODE, -- 1-bit input: Reset input for INMODEREG
	RSTM => IInput_RSTM, -- 1-bit input: Reset input for MREG
	RSTP => IInput_RSTP -- 1-bit input: Reset input for PREG
);
"
			);

		}

	}
}
