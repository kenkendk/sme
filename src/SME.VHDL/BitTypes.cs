using System;
using System.Diagnostics;

namespace SME.VHDL
{
	internal static class UIntFormatHelper
	{
		public static string ToBinaryString(ulong value, int width)
		{
			var res = new char[width];
			for (var i = 0; i < width; i++)
				res[width - 1 - i] = ((value >> i) & 0x1) == 0x1 ? '1' : '0';

			return new string(res);
		}
	}

	internal static class IntFormatHelper
	{
		public static string ToBinaryString(long value, int width)
		{
			var res = new char[width];
			for (var i = 0; i < width; i++)
                res[width - 1 - i] = ((value >> i) & 0x1) == 0x1 ? '1' : '0';

			return new string(res);
		}
	}

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(0 downto 0)", "T_UINT1")]
	public struct UInt1 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 1;

		public UInt1(byte v)
		{
			this.Value = (byte)(v & 0x1);
		}

		public static implicit operator UInt1(byte v)
		{
			return new UInt1(v);
		}

		public static implicit operator byte(UInt1 v)
		{
			return (byte)(v.Value & 0x1);
		}

		public static UInt1 operator++(UInt1 v) 
		{
			return new UInt1((byte)(v + 1));
		}

		public static UInt1 operator--(UInt1 v) 
		{
			return new UInt1((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(1 downto 0)", "T_UINT2")]
	public struct UInt2 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 2;

		public UInt2(byte v)
		{
			this.Value = (byte)(v & 0x3);
		}

		public static implicit operator UInt2(byte v)
		{
			return new UInt2(v);
		}

		public static implicit operator byte(UInt2 v)
		{
			return (byte)(v.Value & 0x3);
		}

		public static UInt2 operator++(UInt2 v) 
		{
			return new UInt2((byte)(v + 1));
		}

		public static UInt2 operator--(UInt2 v) 
		{
			return new UInt2((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(2 downto 0)", "T_UINT3")]
	public struct UInt3 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 3;

		public UInt3(byte v)
		{
			this.Value = (byte)(v & 0x7);
		}

		public static implicit operator UInt3(byte v)
		{
			return new UInt3(v);
		}

		public static implicit operator byte(UInt3 v)
		{
			return (byte)(v.Value & 0x7);
		}

		public static UInt3 operator++(UInt3 v) 
		{
			return new UInt3((byte)(v + 1));
		}

		public static UInt3 operator--(UInt3 v) 
		{
			return new UInt3((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(3 downto 0)", "T_UINT4")]
	public struct UInt4 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 4;

		public UInt4(byte v)
		{
			this.Value = (byte)(v & 0xf);
		}

		public static implicit operator UInt4(byte v)
		{
			return new UInt4(v);
		}

		public static implicit operator byte(UInt4 v)
		{
			return (byte)(v.Value & 0xf);
		}

		public static UInt4 operator++(UInt4 v) 
		{
			return new UInt4((byte)(v + 1));
		}

		public static UInt4 operator--(UInt4 v) 
		{
			return new UInt4((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(4 downto 0)", "T_UINT5")]
	public struct UInt5 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 5;

		public UInt5(byte v)
		{
			this.Value = (byte)(v & 0x1f);
		}

		public static implicit operator UInt5(byte v)
		{
			return new UInt5(v);
		}

		public static implicit operator byte(UInt5 v)
		{
			return (byte)(v.Value & 0x1f);
		}

		public static UInt5 operator++(UInt5 v) 
		{
			return new UInt5((byte)(v + 1));
		}

		public static UInt5 operator--(UInt5 v) 
		{
			return new UInt5((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(5 downto 0)", "T_UINT6")]
	public struct UInt6 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 6;

		public UInt6(byte v)
		{
			this.Value = (byte)(v & 0x3f);
		}

		public static implicit operator UInt6(byte v)
		{
			return new UInt6(v);
		}

		public static implicit operator byte(UInt6 v)
		{
			return (byte)(v.Value & 0x3f);
		}

		public static UInt6 operator++(UInt6 v) 
		{
			return new UInt6((byte)(v + 1));
		}

		public static UInt6 operator--(UInt6 v) 
		{
			return new UInt6((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(6 downto 0)", "T_UINT7")]
	public struct UInt7 : SME.Tracer.ITracerSerializable {

		private readonly byte Value;
		private const int WIDTH = 7;

		public UInt7(byte v)
		{
			this.Value = (byte)(v & 0x7f);
		}

		public static implicit operator UInt7(byte v)
		{
			return new UInt7(v);
		}

		public static implicit operator byte(UInt7 v)
		{
			return (byte)(v.Value & 0x7f);
		}

		public static UInt7 operator++(UInt7 v) 
		{
			return new UInt7((byte)(v + 1));
		}

		public static UInt7 operator--(UInt7 v) 
		{
			return new UInt7((byte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(8 downto 0)", "T_UINT9")]
	public struct UInt9 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 9;

		public UInt9(ushort v)
		{
			this.Value = (ushort)(v & 0x1ff);
		}

		public static implicit operator UInt9(ushort v)
		{
			return new UInt9(v);
		}

		public static implicit operator ushort(UInt9 v)
		{
			return (ushort)(v.Value & 0x1ff);
		}

		public static UInt9 operator++(UInt9 v) 
		{
			return new UInt9((ushort)(v + 1));
		}

		public static UInt9 operator--(UInt9 v) 
		{
			return new UInt9((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(9 downto 0)", "T_UINT10")]
	public struct UInt10 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 10;

		public UInt10(ushort v)
		{
			this.Value = (ushort)(v & 0x3ff);
		}

		public static implicit operator UInt10(ushort v)
		{
			return new UInt10(v);
		}

		public static implicit operator ushort(UInt10 v)
		{
			return (ushort)(v.Value & 0x3ff);
		}

		public static UInt10 operator++(UInt10 v) 
		{
			return new UInt10((ushort)(v + 1));
		}

		public static UInt10 operator--(UInt10 v) 
		{
			return new UInt10((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(10 downto 0)", "T_UINT11")]
	public struct UInt11 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 11;

		public UInt11(ushort v)
		{
			this.Value = (ushort)(v & 0x7ff);
		}

		public static implicit operator UInt11(ushort v)
		{
			return new UInt11(v);
		}

		public static implicit operator ushort(UInt11 v)
		{
			return (ushort)(v.Value & 0x7ff);
		}

		public static UInt11 operator++(UInt11 v) 
		{
			return new UInt11((ushort)(v + 1));
		}

		public static UInt11 operator--(UInt11 v) 
		{
			return new UInt11((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(11 downto 0)", "T_UINT12")]
	public struct UInt12 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 12;

		public UInt12(ushort v)
		{
			this.Value = (ushort)(v & 0xfff);
		}

		public static implicit operator UInt12(ushort v)
		{
			return new UInt12(v);
		}

		public static implicit operator ushort(UInt12 v)
		{
			return (ushort)(v.Value & 0xfff);
		}

		public static UInt12 operator++(UInt12 v) 
		{
			return new UInt12((ushort)(v + 1));
		}

		public static UInt12 operator--(UInt12 v) 
		{
			return new UInt12((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(12 downto 0)", "T_UINT13")]
	public struct UInt13 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 13;

		public UInt13(ushort v)
		{
			this.Value = (ushort)(v & 0x1fff);
		}

		public static implicit operator UInt13(ushort v)
		{
			return new UInt13(v);
		}

		public static implicit operator ushort(UInt13 v)
		{
			return (ushort)(v.Value & 0x1fff);
		}

		public static UInt13 operator++(UInt13 v) 
		{
			return new UInt13((ushort)(v + 1));
		}

		public static UInt13 operator--(UInt13 v) 
		{
			return new UInt13((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(13 downto 0)", "T_UINT14")]
	public struct UInt14 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 14;

		public UInt14(ushort v)
		{
			this.Value = (ushort)(v & 0x3fff);
		}

		public static implicit operator UInt14(ushort v)
		{
			return new UInt14(v);
		}

		public static implicit operator ushort(UInt14 v)
		{
			return (ushort)(v.Value & 0x3fff);
		}

		public static UInt14 operator++(UInt14 v) 
		{
			return new UInt14((ushort)(v + 1));
		}

		public static UInt14 operator--(UInt14 v) 
		{
			return new UInt14((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(14 downto 0)", "T_UINT15")]
	public struct UInt15 : SME.Tracer.ITracerSerializable {

		private readonly ushort Value;
		private const int WIDTH = 15;

		public UInt15(ushort v)
		{
			this.Value = (ushort)(v & 0x7fff);
		}

		public static implicit operator UInt15(ushort v)
		{
			return new UInt15(v);
		}

		public static implicit operator ushort(UInt15 v)
		{
			return (ushort)(v.Value & 0x7fff);
		}

		public static UInt15 operator++(UInt15 v) 
		{
			return new UInt15((ushort)(v + 1));
		}

		public static UInt15 operator--(UInt15 v) 
		{
			return new UInt15((ushort)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(16 downto 0)", "T_UINT17")]
	public struct UInt17 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 17;

		public UInt17(uint v)
		{
			this.Value = (uint)(v & 0x1ffff);
		}

		public static implicit operator UInt17(uint v)
		{
			return new UInt17(v);
		}

		public static implicit operator uint(UInt17 v)
		{
			return (uint)(v.Value & 0x1ffff);
		}

		public static UInt17 operator++(UInt17 v) 
		{
			return new UInt17((uint)(v + 1));
		}

		public static UInt17 operator--(UInt17 v) 
		{
			return new UInt17((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(17 downto 0)", "T_UINT18")]
	public struct UInt18 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 18;

		public UInt18(uint v)
		{
			this.Value = (uint)(v & 0x3ffff);
		}

		public static implicit operator UInt18(uint v)
		{
			return new UInt18(v);
		}

		public static implicit operator uint(UInt18 v)
		{
			return (uint)(v.Value & 0x3ffff);
		}

		public static UInt18 operator++(UInt18 v) 
		{
			return new UInt18((uint)(v + 1));
		}

		public static UInt18 operator--(UInt18 v) 
		{
			return new UInt18((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(18 downto 0)", "T_UINT19")]
	public struct UInt19 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 19;

		public UInt19(uint v)
		{
			this.Value = (uint)(v & 0x7ffff);
		}

		public static implicit operator UInt19(uint v)
		{
			return new UInt19(v);
		}

		public static implicit operator uint(UInt19 v)
		{
			return (uint)(v.Value & 0x7ffff);
		}

		public static UInt19 operator++(UInt19 v) 
		{
			return new UInt19((uint)(v + 1));
		}

		public static UInt19 operator--(UInt19 v) 
		{
			return new UInt19((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(19 downto 0)", "T_UINT20")]
	public struct UInt20 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 20;

		public UInt20(uint v)
		{
			this.Value = (uint)(v & 0xfffff);
		}

		public static implicit operator UInt20(uint v)
		{
			return new UInt20(v);
		}

		public static implicit operator uint(UInt20 v)
		{
			return (uint)(v.Value & 0xfffff);
		}

		public static UInt20 operator++(UInt20 v) 
		{
			return new UInt20((uint)(v + 1));
		}

		public static UInt20 operator--(UInt20 v) 
		{
			return new UInt20((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(20 downto 0)", "T_UINT21")]
	public struct UInt21 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 21;

		public UInt21(uint v)
		{
			this.Value = (uint)(v & 0x1fffff);
		}

		public static implicit operator UInt21(uint v)
		{
			return new UInt21(v);
		}

		public static implicit operator uint(UInt21 v)
		{
			return (uint)(v.Value & 0x1fffff);
		}

		public static UInt21 operator++(UInt21 v) 
		{
			return new UInt21((uint)(v + 1));
		}

		public static UInt21 operator--(UInt21 v) 
		{
			return new UInt21((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(21 downto 0)", "T_UINT22")]
	public struct UInt22 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 22;

		public UInt22(uint v)
		{
			this.Value = (uint)(v & 0x3fffff);
		}

		public static implicit operator UInt22(uint v)
		{
			return new UInt22(v);
		}

		public static implicit operator uint(UInt22 v)
		{
			return (uint)(v.Value & 0x3fffff);
		}

		public static UInt22 operator++(UInt22 v) 
		{
			return new UInt22((uint)(v + 1));
		}

		public static UInt22 operator--(UInt22 v) 
		{
			return new UInt22((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(22 downto 0)", "T_UINT23")]
	public struct UInt23 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 23;

		public UInt23(uint v)
		{
			this.Value = (uint)(v & 0x7fffff);
		}

		public static implicit operator UInt23(uint v)
		{
			return new UInt23(v);
		}

		public static implicit operator uint(UInt23 v)
		{
			return (uint)(v.Value & 0x7fffff);
		}

		public static UInt23 operator++(UInt23 v) 
		{
			return new UInt23((uint)(v + 1));
		}

		public static UInt23 operator--(UInt23 v) 
		{
			return new UInt23((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(23 downto 0)", "T_UINT24")]
	public struct UInt24 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 24;

		public UInt24(uint v)
		{
			this.Value = (uint)(v & 0xffffff);
		}

		public static implicit operator UInt24(uint v)
		{
			return new UInt24(v);
		}

		public static implicit operator uint(UInt24 v)
		{
			return (uint)(v.Value & 0xffffff);
		}

		public static UInt24 operator++(UInt24 v) 
		{
			return new UInt24((uint)(v + 1));
		}

		public static UInt24 operator--(UInt24 v) 
		{
			return new UInt24((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(24 downto 0)", "T_UINT25")]
	public struct UInt25 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 25;

		public UInt25(uint v)
		{
			this.Value = (uint)(v & 0x1ffffff);
		}

		public static implicit operator UInt25(uint v)
		{
			return new UInt25(v);
		}

		public static implicit operator uint(UInt25 v)
		{
			return (uint)(v.Value & 0x1ffffff);
		}

		public static UInt25 operator++(UInt25 v) 
		{
			return new UInt25((uint)(v + 1));
		}

		public static UInt25 operator--(UInt25 v) 
		{
			return new UInt25((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(25 downto 0)", "T_UINT26")]
	public struct UInt26 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 26;

		public UInt26(uint v)
		{
			this.Value = (uint)(v & 0x3ffffff);
		}

		public static implicit operator UInt26(uint v)
		{
			return new UInt26(v);
		}

		public static implicit operator uint(UInt26 v)
		{
			return (uint)(v.Value & 0x3ffffff);
		}

		public static UInt26 operator++(UInt26 v) 
		{
			return new UInt26((uint)(v + 1));
		}

		public static UInt26 operator--(UInt26 v) 
		{
			return new UInt26((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(26 downto 0)", "T_UINT27")]
	public struct UInt27 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 27;

		public UInt27(uint v)
		{
			this.Value = (uint)(v & 0x7ffffff);
		}

		public static implicit operator UInt27(uint v)
		{
			return new UInt27(v);
		}

		public static implicit operator uint(UInt27 v)
		{
			return (uint)(v.Value & 0x7ffffff);
		}

		public static UInt27 operator++(UInt27 v) 
		{
			return new UInt27((uint)(v + 1));
		}

		public static UInt27 operator--(UInt27 v) 
		{
			return new UInt27((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(27 downto 0)", "T_UINT28")]
	public struct UInt28 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 28;

		public UInt28(uint v)
		{
			this.Value = (uint)(v & 0xfffffff);
		}

		public static implicit operator UInt28(uint v)
		{
			return new UInt28(v);
		}

		public static implicit operator uint(UInt28 v)
		{
			return (uint)(v.Value & 0xfffffff);
		}

		public static UInt28 operator++(UInt28 v) 
		{
			return new UInt28((uint)(v + 1));
		}

		public static UInt28 operator--(UInt28 v) 
		{
			return new UInt28((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(28 downto 0)", "T_UINT29")]
	public struct UInt29 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 29;

		public UInt29(uint v)
		{
			this.Value = (uint)(v & 0x1fffffff);
		}

		public static implicit operator UInt29(uint v)
		{
			return new UInt29(v);
		}

		public static implicit operator uint(UInt29 v)
		{
			return (uint)(v.Value & 0x1fffffff);
		}

		public static UInt29 operator++(UInt29 v) 
		{
			return new UInt29((uint)(v + 1));
		}

		public static UInt29 operator--(UInt29 v) 
		{
			return new UInt29((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(29 downto 0)", "T_UINT30")]
	public struct UInt30 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 30;

		public UInt30(uint v)
		{
			this.Value = (uint)(v & 0x3fffffff);
		}

		public static implicit operator UInt30(uint v)
		{
			return new UInt30(v);
		}

		public static implicit operator uint(UInt30 v)
		{
			return (uint)(v.Value & 0x3fffffff);
		}

		public static UInt30 operator++(UInt30 v) 
		{
			return new UInt30((uint)(v + 1));
		}

		public static UInt30 operator--(UInt30 v) 
		{
			return new UInt30((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(30 downto 0)", "T_UINT31")]
	public struct UInt31 : SME.Tracer.ITracerSerializable {

		private readonly uint Value;
		private const int WIDTH = 31;

		public UInt31(uint v)
		{
			this.Value = (uint)(v & 0x7fffffff);
		}

		public static implicit operator UInt31(uint v)
		{
			return new UInt31(v);
		}

		public static implicit operator uint(UInt31 v)
		{
			return (uint)(v.Value & 0x7fffffff);
		}

		public static UInt31 operator++(UInt31 v) 
		{
			return new UInt31((uint)(v + 1));
		}

		public static UInt31 operator--(UInt31 v) 
		{
			return new UInt31((uint)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(32 downto 0)", "T_UINT33")]
	public struct UInt33 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 33;

		public UInt33(ulong v)
		{
			this.Value = (ulong)(v & 0x1ffffffff);
		}

		public static implicit operator UInt33(ulong v)
		{
			return new UInt33(v);
		}

		public static implicit operator ulong(UInt33 v)
		{
			return (ulong)(v.Value & 0x1ffffffff);
		}

		public static UInt33 operator++(UInt33 v) 
		{
			return new UInt33((ulong)(v + 1));
		}

		public static UInt33 operator--(UInt33 v) 
		{
			return new UInt33((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(33 downto 0)", "T_UINT34")]
	public struct UInt34 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 34;

		public UInt34(ulong v)
		{
			this.Value = (ulong)(v & 0x3ffffffff);
		}

		public static implicit operator UInt34(ulong v)
		{
			return new UInt34(v);
		}

		public static implicit operator ulong(UInt34 v)
		{
			return (ulong)(v.Value & 0x3ffffffff);
		}

		public static UInt34 operator++(UInt34 v) 
		{
			return new UInt34((ulong)(v + 1));
		}

		public static UInt34 operator--(UInt34 v) 
		{
			return new UInt34((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(34 downto 0)", "T_UINT35")]
	public struct UInt35 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 35;

		public UInt35(ulong v)
		{
			this.Value = (ulong)(v & 0x7ffffffff);
		}

		public static implicit operator UInt35(ulong v)
		{
			return new UInt35(v);
		}

		public static implicit operator ulong(UInt35 v)
		{
			return (ulong)(v.Value & 0x7ffffffff);
		}

		public static UInt35 operator++(UInt35 v) 
		{
			return new UInt35((ulong)(v + 1));
		}

		public static UInt35 operator--(UInt35 v) 
		{
			return new UInt35((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(35 downto 0)", "T_UINT36")]
	public struct UInt36 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 36;

		public UInt36(ulong v)
		{
			this.Value = (ulong)(v & 0xfffffffff);
		}

		public static implicit operator UInt36(ulong v)
		{
			return new UInt36(v);
		}

		public static implicit operator ulong(UInt36 v)
		{
			return (ulong)(v.Value & 0xfffffffff);
		}

		public static UInt36 operator++(UInt36 v) 
		{
			return new UInt36((ulong)(v + 1));
		}

		public static UInt36 operator--(UInt36 v) 
		{
			return new UInt36((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(36 downto 0)", "T_UINT37")]
	public struct UInt37 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 37;

		public UInt37(ulong v)
		{
			this.Value = (ulong)(v & 0x1fffffffff);
		}

		public static implicit operator UInt37(ulong v)
		{
			return new UInt37(v);
		}

		public static implicit operator ulong(UInt37 v)
		{
			return (ulong)(v.Value & 0x1fffffffff);
		}

		public static UInt37 operator++(UInt37 v) 
		{
			return new UInt37((ulong)(v + 1));
		}

		public static UInt37 operator--(UInt37 v) 
		{
			return new UInt37((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(37 downto 0)", "T_UINT38")]
	public struct UInt38 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 38;

		public UInt38(ulong v)
		{
			this.Value = (ulong)(v & 0x3fffffffff);
		}

		public static implicit operator UInt38(ulong v)
		{
			return new UInt38(v);
		}

		public static implicit operator ulong(UInt38 v)
		{
			return (ulong)(v.Value & 0x3fffffffff);
		}

		public static UInt38 operator++(UInt38 v) 
		{
			return new UInt38((ulong)(v + 1));
		}

		public static UInt38 operator--(UInt38 v) 
		{
			return new UInt38((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(38 downto 0)", "T_UINT39")]
	public struct UInt39 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 39;

		public UInt39(ulong v)
		{
			this.Value = (ulong)(v & 0x7fffffffff);
		}

		public static implicit operator UInt39(ulong v)
		{
			return new UInt39(v);
		}

		public static implicit operator ulong(UInt39 v)
		{
			return (ulong)(v.Value & 0x7fffffffff);
		}

		public static UInt39 operator++(UInt39 v) 
		{
			return new UInt39((ulong)(v + 1));
		}

		public static UInt39 operator--(UInt39 v) 
		{
			return new UInt39((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(39 downto 0)", "T_UINT40")]
	public struct UInt40 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 40;

		public UInt40(ulong v)
		{
			this.Value = (ulong)(v & 0xffffffffff);
		}

		public static implicit operator UInt40(ulong v)
		{
			return new UInt40(v);
		}

		public static implicit operator ulong(UInt40 v)
		{
			return (ulong)(v.Value & 0xffffffffff);
		}

		public static UInt40 operator++(UInt40 v) 
		{
			return new UInt40((ulong)(v + 1));
		}

		public static UInt40 operator--(UInt40 v) 
		{
			return new UInt40((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(40 downto 0)", "T_UINT41")]
	public struct UInt41 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 41;

		public UInt41(ulong v)
		{
			this.Value = (ulong)(v & 0x1ffffffffff);
		}

		public static implicit operator UInt41(ulong v)
		{
			return new UInt41(v);
		}

		public static implicit operator ulong(UInt41 v)
		{
			return (ulong)(v.Value & 0x1ffffffffff);
		}

		public static UInt41 operator++(UInt41 v) 
		{
			return new UInt41((ulong)(v + 1));
		}

		public static UInt41 operator--(UInt41 v) 
		{
			return new UInt41((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(41 downto 0)", "T_UINT42")]
	public struct UInt42 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 42;

		public UInt42(ulong v)
		{
			this.Value = (ulong)(v & 0x3ffffffffff);
		}

		public static implicit operator UInt42(ulong v)
		{
			return new UInt42(v);
		}

		public static implicit operator ulong(UInt42 v)
		{
			return (ulong)(v.Value & 0x3ffffffffff);
		}

		public static UInt42 operator++(UInt42 v) 
		{
			return new UInt42((ulong)(v + 1));
		}

		public static UInt42 operator--(UInt42 v) 
		{
			return new UInt42((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(42 downto 0)", "T_UINT43")]
	public struct UInt43 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 43;

		public UInt43(ulong v)
		{
			this.Value = (ulong)(v & 0x7ffffffffff);
		}

		public static implicit operator UInt43(ulong v)
		{
			return new UInt43(v);
		}

		public static implicit operator ulong(UInt43 v)
		{
			return (ulong)(v.Value & 0x7ffffffffff);
		}

		public static UInt43 operator++(UInt43 v) 
		{
			return new UInt43((ulong)(v + 1));
		}

		public static UInt43 operator--(UInt43 v) 
		{
			return new UInt43((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(43 downto 0)", "T_UINT44")]
	public struct UInt44 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 44;

		public UInt44(ulong v)
		{
			this.Value = (ulong)(v & 0xfffffffffff);
		}

		public static implicit operator UInt44(ulong v)
		{
			return new UInt44(v);
		}

		public static implicit operator ulong(UInt44 v)
		{
			return (ulong)(v.Value & 0xfffffffffff);
		}

		public static UInt44 operator++(UInt44 v) 
		{
			return new UInt44((ulong)(v + 1));
		}

		public static UInt44 operator--(UInt44 v) 
		{
			return new UInt44((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(44 downto 0)", "T_UINT45")]
	public struct UInt45 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 45;

		public UInt45(ulong v)
		{
			this.Value = (ulong)(v & 0x1fffffffffff);
		}

		public static implicit operator UInt45(ulong v)
		{
			return new UInt45(v);
		}

		public static implicit operator ulong(UInt45 v)
		{
			return (ulong)(v.Value & 0x1fffffffffff);
		}

		public static UInt45 operator++(UInt45 v) 
		{
			return new UInt45((ulong)(v + 1));
		}

		public static UInt45 operator--(UInt45 v) 
		{
			return new UInt45((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(45 downto 0)", "T_UINT46")]
	public struct UInt46 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 46;

		public UInt46(ulong v)
		{
			this.Value = (ulong)(v & 0x3fffffffffff);
		}

		public static implicit operator UInt46(ulong v)
		{
			return new UInt46(v);
		}

		public static implicit operator ulong(UInt46 v)
		{
			return (ulong)(v.Value & 0x3fffffffffff);
		}

		public static UInt46 operator++(UInt46 v) 
		{
			return new UInt46((ulong)(v + 1));
		}

		public static UInt46 operator--(UInt46 v) 
		{
			return new UInt46((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(46 downto 0)", "T_UINT47")]
	public struct UInt47 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 47;

		public UInt47(ulong v)
		{
			this.Value = (ulong)(v & 0x7fffffffffff);
		}

		public static implicit operator UInt47(ulong v)
		{
			return new UInt47(v);
		}

		public static implicit operator ulong(UInt47 v)
		{
			return (ulong)(v.Value & 0x7fffffffffff);
		}

		public static UInt47 operator++(UInt47 v) 
		{
			return new UInt47((ulong)(v + 1));
		}

		public static UInt47 operator--(UInt47 v) 
		{
			return new UInt47((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(47 downto 0)", "T_UINT48")]
	public struct UInt48 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 48;

		public UInt48(ulong v)
		{
			this.Value = (ulong)(v & 0xffffffffffff);
		}

		public static implicit operator UInt48(ulong v)
		{
			return new UInt48(v);
		}

		public static implicit operator ulong(UInt48 v)
		{
			return (ulong)(v.Value & 0xffffffffffff);
		}

		public static UInt48 operator++(UInt48 v) 
		{
			return new UInt48((ulong)(v + 1));
		}

		public static UInt48 operator--(UInt48 v) 
		{
			return new UInt48((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(48 downto 0)", "T_UINT49")]
	public struct UInt49 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 49;

		public UInt49(ulong v)
		{
			this.Value = (ulong)(v & 0x1ffffffffffff);
		}

		public static implicit operator UInt49(ulong v)
		{
			return new UInt49(v);
		}

		public static implicit operator ulong(UInt49 v)
		{
			return (ulong)(v.Value & 0x1ffffffffffff);
		}

		public static UInt49 operator++(UInt49 v) 
		{
			return new UInt49((ulong)(v + 1));
		}

		public static UInt49 operator--(UInt49 v) 
		{
			return new UInt49((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(49 downto 0)", "T_UINT50")]
	public struct UInt50 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 50;

		public UInt50(ulong v)
		{
			this.Value = (ulong)(v & 0x3ffffffffffff);
		}

		public static implicit operator UInt50(ulong v)
		{
			return new UInt50(v);
		}

		public static implicit operator ulong(UInt50 v)
		{
			return (ulong)(v.Value & 0x3ffffffffffff);
		}

		public static UInt50 operator++(UInt50 v) 
		{
			return new UInt50((ulong)(v + 1));
		}

		public static UInt50 operator--(UInt50 v) 
		{
			return new UInt50((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(50 downto 0)", "T_UINT51")]
	public struct UInt51 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 51;

		public UInt51(ulong v)
		{
			this.Value = (ulong)(v & 0x7ffffffffffff);
		}

		public static implicit operator UInt51(ulong v)
		{
			return new UInt51(v);
		}

		public static implicit operator ulong(UInt51 v)
		{
			return (ulong)(v.Value & 0x7ffffffffffff);
		}

		public static UInt51 operator++(UInt51 v) 
		{
			return new UInt51((ulong)(v + 1));
		}

		public static UInt51 operator--(UInt51 v) 
		{
			return new UInt51((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(51 downto 0)", "T_UINT52")]
	public struct UInt52 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 52;

		public UInt52(ulong v)
		{
			this.Value = (ulong)(v & 0xfffffffffffff);
		}

		public static implicit operator UInt52(ulong v)
		{
			return new UInt52(v);
		}

		public static implicit operator ulong(UInt52 v)
		{
			return (ulong)(v.Value & 0xfffffffffffff);
		}

		public static UInt52 operator++(UInt52 v) 
		{
			return new UInt52((ulong)(v + 1));
		}

		public static UInt52 operator--(UInt52 v) 
		{
			return new UInt52((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(52 downto 0)", "T_UINT53")]
	public struct UInt53 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 53;

		public UInt53(ulong v)
		{
			this.Value = (ulong)(v & 0x1fffffffffffff);
		}

		public static implicit operator UInt53(ulong v)
		{
			return new UInt53(v);
		}

		public static implicit operator ulong(UInt53 v)
		{
			return (ulong)(v.Value & 0x1fffffffffffff);
		}

		public static UInt53 operator++(UInt53 v) 
		{
			return new UInt53((ulong)(v + 1));
		}

		public static UInt53 operator--(UInt53 v) 
		{
			return new UInt53((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(53 downto 0)", "T_UINT54")]
	public struct UInt54 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 54;

		public UInt54(ulong v)
		{
			this.Value = (ulong)(v & 0x3fffffffffffff);
		}

		public static implicit operator UInt54(ulong v)
		{
			return new UInt54(v);
		}

		public static implicit operator ulong(UInt54 v)
		{
			return (ulong)(v.Value & 0x3fffffffffffff);
		}

		public static UInt54 operator++(UInt54 v) 
		{
			return new UInt54((ulong)(v + 1));
		}

		public static UInt54 operator--(UInt54 v) 
		{
			return new UInt54((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(54 downto 0)", "T_UINT55")]
	public struct UInt55 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 55;

		public UInt55(ulong v)
		{
			this.Value = (ulong)(v & 0x7fffffffffffff);
		}

		public static implicit operator UInt55(ulong v)
		{
			return new UInt55(v);
		}

		public static implicit operator ulong(UInt55 v)
		{
			return (ulong)(v.Value & 0x7fffffffffffff);
		}

		public static UInt55 operator++(UInt55 v) 
		{
			return new UInt55((ulong)(v + 1));
		}

		public static UInt55 operator--(UInt55 v) 
		{
			return new UInt55((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(55 downto 0)", "T_UINT56")]
	public struct UInt56 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 56;

		public UInt56(ulong v)
		{
			this.Value = (ulong)(v & 0xffffffffffffff);
		}

		public static implicit operator UInt56(ulong v)
		{
			return new UInt56(v);
		}

		public static implicit operator ulong(UInt56 v)
		{
			return (ulong)(v.Value & 0xffffffffffffff);
		}

		public static UInt56 operator++(UInt56 v) 
		{
			return new UInt56((ulong)(v + 1));
		}

		public static UInt56 operator--(UInt56 v) 
		{
			return new UInt56((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(56 downto 0)", "T_UINT57")]
	public struct UInt57 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 57;

		public UInt57(ulong v)
		{
			this.Value = (ulong)(v & 0x1ffffffffffffff);
		}

		public static implicit operator UInt57(ulong v)
		{
			return new UInt57(v);
		}

		public static implicit operator ulong(UInt57 v)
		{
			return (ulong)(v.Value & 0x1ffffffffffffff);
		}

		public static UInt57 operator++(UInt57 v) 
		{
			return new UInt57((ulong)(v + 1));
		}

		public static UInt57 operator--(UInt57 v) 
		{
			return new UInt57((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(57 downto 0)", "T_UINT58")]
	public struct UInt58 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 58;

		public UInt58(ulong v)
		{
			this.Value = (ulong)(v & 0x3ffffffffffffff);
		}

		public static implicit operator UInt58(ulong v)
		{
			return new UInt58(v);
		}

		public static implicit operator ulong(UInt58 v)
		{
			return (ulong)(v.Value & 0x3ffffffffffffff);
		}

		public static UInt58 operator++(UInt58 v) 
		{
			return new UInt58((ulong)(v + 1));
		}

		public static UInt58 operator--(UInt58 v) 
		{
			return new UInt58((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(58 downto 0)", "T_UINT59")]
	public struct UInt59 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 59;

		public UInt59(ulong v)
		{
			this.Value = (ulong)(v & 0x7ffffffffffffff);
		}

		public static implicit operator UInt59(ulong v)
		{
			return new UInt59(v);
		}

		public static implicit operator ulong(UInt59 v)
		{
			return (ulong)(v.Value & 0x7ffffffffffffff);
		}

		public static UInt59 operator++(UInt59 v) 
		{
			return new UInt59((ulong)(v + 1));
		}

		public static UInt59 operator--(UInt59 v) 
		{
			return new UInt59((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(59 downto 0)", "T_UINT60")]
	public struct UInt60 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 60;

		public UInt60(ulong v)
		{
			this.Value = (ulong)(v & 0xfffffffffffffff);
		}

		public static implicit operator UInt60(ulong v)
		{
			return new UInt60(v);
		}

		public static implicit operator ulong(UInt60 v)
		{
			return (ulong)(v.Value & 0xfffffffffffffff);
		}

		public static UInt60 operator++(UInt60 v) 
		{
			return new UInt60((ulong)(v + 1));
		}

		public static UInt60 operator--(UInt60 v) 
		{
			return new UInt60((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(60 downto 0)", "T_UINT61")]
	public struct UInt61 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 61;

		public UInt61(ulong v)
		{
			this.Value = (ulong)(v & 0x1fffffffffffffff);
		}

		public static implicit operator UInt61(ulong v)
		{
			return new UInt61(v);
		}

		public static implicit operator ulong(UInt61 v)
		{
			return (ulong)(v.Value & 0x1fffffffffffffff);
		}

		public static UInt61 operator++(UInt61 v) 
		{
			return new UInt61((ulong)(v + 1));
		}

		public static UInt61 operator--(UInt61 v) 
		{
			return new UInt61((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(61 downto 0)", "T_UINT62")]
	public struct UInt62 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 62;

		public UInt62(ulong v)
		{
			this.Value = (ulong)(v & 0x3fffffffffffffff);
		}

		public static implicit operator UInt62(ulong v)
		{
			return new UInt62(v);
		}

		public static implicit operator ulong(UInt62 v)
		{
			return (ulong)(v.Value & 0x3fffffffffffffff);
		}

		public static UInt62 operator++(UInt62 v) 
		{
			return new UInt62((ulong)(v + 1));
		}

		public static UInt62 operator--(UInt62 v) 
		{
			return new UInt62((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(62 downto 0)", "T_UINT63")]
	public struct UInt63 : SME.Tracer.ITracerSerializable {

		private readonly ulong Value;
		private const int WIDTH = 63;

		public UInt63(ulong v)
		{
			this.Value = (ulong)(v & 0x7fffffffffffffff);
		}

		public static implicit operator UInt63(ulong v)
		{
			return new UInt63(v);
		}

		public static implicit operator ulong(UInt63 v)
		{
			return (ulong)(v.Value & 0x7fffffffffffffff);
		}

		public static UInt63 operator++(UInt63 v) 
		{
			return new UInt63((ulong)(v + 1));
		}

		public static UInt63 operator--(UInt63 v) 
		{
			return new UInt63((ulong)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return UIntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};


	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(0 downto 0)", "T_INT1")]
	public struct Int1 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 1;

		public Int1(sbyte v)
		{
			this.Value = (sbyte)(v & 0x1);
		}

		public static implicit operator Int1(sbyte v)
		{
			return new Int1(v);
		}

		public static implicit operator sbyte(Int1 v)
		{
			return (sbyte)(v.Value & 0x1);
		}

		public static Int1 operator++(Int1 v) 
		{
			return new Int1((sbyte)(v + 1));
		}

		public static Int1 operator--(Int1 v) 
		{
			return new Int1((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(1 downto 0)", "T_INT2")]
	public struct Int2 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 2;

		public Int2(sbyte v)
		{
			this.Value = (sbyte)(v & 0x3);
		}

		public static implicit operator Int2(sbyte v)
		{
			return new Int2(v);
		}

		public static implicit operator sbyte(Int2 v)
		{
			return (sbyte)(v.Value & 0x3);
		}

		public static Int2 operator++(Int2 v) 
		{
			return new Int2((sbyte)(v + 1));
		}

		public static Int2 operator--(Int2 v) 
		{
			return new Int2((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(2 downto 0)", "T_INT3")]
	public struct Int3 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 3;

		public Int3(sbyte v)
		{
			this.Value = (sbyte)(v & 0x7);
		}

		public static implicit operator Int3(sbyte v)
		{
			return new Int3(v);
		}

		public static implicit operator sbyte(Int3 v)
		{
			return (sbyte)(v.Value & 0x7);
		}

		public static Int3 operator++(Int3 v) 
		{
			return new Int3((sbyte)(v + 1));
		}

		public static Int3 operator--(Int3 v) 
		{
			return new Int3((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(3 downto 0)", "T_INT4")]
	public struct Int4 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 4;

		public Int4(sbyte v)
		{
			this.Value = (sbyte)(v & 0xf);
		}

		public static implicit operator Int4(sbyte v)
		{
			return new Int4(v);
		}

		public static implicit operator sbyte(Int4 v)
		{
			return (sbyte)(v.Value & 0xf);
		}

		public static Int4 operator++(Int4 v) 
		{
			return new Int4((sbyte)(v + 1));
		}

		public static Int4 operator--(Int4 v) 
		{
			return new Int4((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(4 downto 0)", "T_INT5")]
	public struct Int5 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 5;

		public Int5(sbyte v)
		{
			this.Value = (sbyte)(v & 0x1f);
		}

		public static implicit operator Int5(sbyte v)
		{
			return new Int5(v);
		}

		public static implicit operator sbyte(Int5 v)
		{
			return (sbyte)(v.Value & 0x1f);
		}

		public static Int5 operator++(Int5 v) 
		{
			return new Int5((sbyte)(v + 1));
		}

		public static Int5 operator--(Int5 v) 
		{
			return new Int5((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(5 downto 0)", "T_INT6")]
	public struct Int6 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 6;

		public Int6(sbyte v)
		{
			this.Value = (sbyte)(v & 0x3f);
		}

		public static implicit operator Int6(sbyte v)
		{
			return new Int6(v);
		}

		public static implicit operator sbyte(Int6 v)
		{
			return (sbyte)(v.Value & 0x3f);
		}

		public static Int6 operator++(Int6 v) 
		{
			return new Int6((sbyte)(v + 1));
		}

		public static Int6 operator--(Int6 v) 
		{
			return new Int6((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(6 downto 0)", "T_INT7")]
	public struct Int7 : SME.Tracer.ITracerSerializable {

		private readonly sbyte Value;
		private const int WIDTH = 7;

		public Int7(sbyte v)
		{
			this.Value = (sbyte)(v & 0x7f);
		}

		public static implicit operator Int7(sbyte v)
		{
			return new Int7(v);
		}

		public static implicit operator sbyte(Int7 v)
		{
			return (sbyte)(v.Value & 0x7f);
		}

		public static Int7 operator++(Int7 v) 
		{
			return new Int7((sbyte)(v + 1));
		}

		public static Int7 operator--(Int7 v) 
		{
			return new Int7((sbyte)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(8 downto 0)", "T_INT9")]
	public struct Int9 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 9;

		public Int9(short v)
		{
			this.Value = (short)(v & 0x1ff);
		}

		public static implicit operator Int9(short v)
		{
			return new Int9(v);
		}

		public static implicit operator short(Int9 v)
		{
			return (short)(v.Value & 0x1ff);
		}

		public static Int9 operator++(Int9 v) 
		{
			return new Int9((short)(v + 1));
		}

		public static Int9 operator--(Int9 v) 
		{
			return new Int9((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(9 downto 0)", "T_INT10")]
	public struct Int10 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 10;

		public Int10(short v)
		{
			this.Value = (short)(v & 0x3ff);
		}

		public static implicit operator Int10(short v)
		{
			return new Int10(v);
		}

		public static implicit operator short(Int10 v)
		{
			return (short)(v.Value & 0x3ff);
		}

		public static Int10 operator++(Int10 v) 
		{
			return new Int10((short)(v + 1));
		}

		public static Int10 operator--(Int10 v) 
		{
			return new Int10((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(10 downto 0)", "T_INT11")]
	public struct Int11 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 11;

		public Int11(short v)
		{
			this.Value = (short)(v & 0x7ff);
		}

		public static implicit operator Int11(short v)
		{
			return new Int11(v);
		}

		public static implicit operator short(Int11 v)
		{
			return (short)(v.Value & 0x7ff);
		}

		public static Int11 operator++(Int11 v) 
		{
			return new Int11((short)(v + 1));
		}

		public static Int11 operator--(Int11 v) 
		{
			return new Int11((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(11 downto 0)", "T_INT12")]
	public struct Int12 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 12;

		public Int12(short v)
		{
			this.Value = (short)(v & 0xfff);
		}

		public static implicit operator Int12(short v)
		{
			return new Int12(v);
		}

		public static implicit operator short(Int12 v)
		{
			return (short)(v.Value & 0xfff);
		}

		public static Int12 operator++(Int12 v) 
		{
			return new Int12((short)(v + 1));
		}

		public static Int12 operator--(Int12 v) 
		{
			return new Int12((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(12 downto 0)", "T_INT13")]
	public struct Int13 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 13;

		public Int13(short v)
		{
			this.Value = (short)(v & 0x1fff);
		}

		public static implicit operator Int13(short v)
		{
			return new Int13(v);
		}

		public static implicit operator short(Int13 v)
		{
			return (short)(v.Value & 0x1fff);
		}

		public static Int13 operator++(Int13 v) 
		{
			return new Int13((short)(v + 1));
		}

		public static Int13 operator--(Int13 v) 
		{
			return new Int13((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(13 downto 0)", "T_INT14")]
	public struct Int14 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 14;

		public Int14(short v)
		{
			this.Value = (short)(v & 0x3fff);
		}

		public static implicit operator Int14(short v)
		{
			return new Int14(v);
		}

		public static implicit operator short(Int14 v)
		{
			return (short)(v.Value & 0x3fff);
		}

		public static Int14 operator++(Int14 v) 
		{
			return new Int14((short)(v + 1));
		}

		public static Int14 operator--(Int14 v) 
		{
			return new Int14((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(14 downto 0)", "T_INT15")]
	public struct Int15 : SME.Tracer.ITracerSerializable {

		private readonly short Value;
		private const int WIDTH = 15;

		public Int15(short v)
		{
			this.Value = (short)(v & 0x7fff);
		}

		public static implicit operator Int15(short v)
		{
			return new Int15(v);
		}

		public static implicit operator short(Int15 v)
		{
			return (short)(v.Value & 0x7fff);
		}

		public static Int15 operator++(Int15 v) 
		{
			return new Int15((short)(v + 1));
		}

		public static Int15 operator--(Int15 v) 
		{
			return new Int15((short)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(16 downto 0)", "T_INT17")]
	public struct Int17 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 17;

		public Int17(int v)
		{
			this.Value = (int)(v & 0x1ffff);
		}

		public static implicit operator Int17(int v)
		{
			return new Int17(v);
		}

		public static implicit operator int(Int17 v)
		{
			return (int)(v.Value & 0x1ffff);
		}

		public static Int17 operator++(Int17 v) 
		{
			return new Int17((int)(v + 1));
		}

		public static Int17 operator--(Int17 v) 
		{
			return new Int17((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(17 downto 0)", "T_INT18")]
	public struct Int18 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 18;

		public Int18(int v)
		{
			this.Value = (int)(v & 0x3ffff);
		}

		public static implicit operator Int18(int v)
		{
			return new Int18(v);
		}

		public static implicit operator int(Int18 v)
		{
			return (int)(v.Value & 0x3ffff);
		}

		public static Int18 operator++(Int18 v) 
		{
			return new Int18((int)(v + 1));
		}

		public static Int18 operator--(Int18 v) 
		{
			return new Int18((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(18 downto 0)", "T_INT19")]
	public struct Int19 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 19;

		public Int19(int v)
		{
			this.Value = (int)(v & 0x7ffff);
		}

		public static implicit operator Int19(int v)
		{
			return new Int19(v);
		}

		public static implicit operator int(Int19 v)
		{
			return (int)(v.Value & 0x7ffff);
		}

		public static Int19 operator++(Int19 v) 
		{
			return new Int19((int)(v + 1));
		}

		public static Int19 operator--(Int19 v) 
		{
			return new Int19((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(19 downto 0)", "T_INT20")]
	public struct Int20 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 20;

		public Int20(int v)
		{
			this.Value = (int)(v & 0xfffff);
		}

		public static implicit operator Int20(int v)
		{
			return new Int20(v);
		}

		public static implicit operator int(Int20 v)
		{
			return (int)(v.Value & 0xfffff);
		}

		public static Int20 operator++(Int20 v) 
		{
			return new Int20((int)(v + 1));
		}

		public static Int20 operator--(Int20 v) 
		{
			return new Int20((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(20 downto 0)", "T_INT21")]
	public struct Int21 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 21;

		public Int21(int v)
		{
			this.Value = (int)(v & 0x1fffff);
		}

		public static implicit operator Int21(int v)
		{
			return new Int21(v);
		}

		public static implicit operator int(Int21 v)
		{
			return (int)(v.Value & 0x1fffff);
		}

		public static Int21 operator++(Int21 v) 
		{
			return new Int21((int)(v + 1));
		}

		public static Int21 operator--(Int21 v) 
		{
			return new Int21((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(21 downto 0)", "T_INT22")]
	public struct Int22 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 22;

		public Int22(int v)
		{
			this.Value = (int)(v & 0x3fffff);
		}

		public static implicit operator Int22(int v)
		{
			return new Int22(v);
		}

		public static implicit operator int(Int22 v)
		{
			return (int)(v.Value & 0x3fffff);
		}

		public static Int22 operator++(Int22 v) 
		{
			return new Int22((int)(v + 1));
		}

		public static Int22 operator--(Int22 v) 
		{
			return new Int22((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(22 downto 0)", "T_INT23")]
	public struct Int23 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 23;

		public Int23(int v)
		{
			this.Value = (int)(v & 0x7fffff);
		}

		public static implicit operator Int23(int v)
		{
			return new Int23(v);
		}

		public static implicit operator int(Int23 v)
		{
			return (int)(v.Value & 0x7fffff);
		}

		public static Int23 operator++(Int23 v) 
		{
			return new Int23((int)(v + 1));
		}

		public static Int23 operator--(Int23 v) 
		{
			return new Int23((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(23 downto 0)", "T_INT24")]
	public struct Int24 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 24;

		public Int24(int v)
		{
			this.Value = (int)(v & 0xffffff);
		}

		public static implicit operator Int24(int v)
		{
			return new Int24(v);
		}

		public static implicit operator int(Int24 v)
		{
			return (int)(v.Value & 0xffffff);
		}

		public static Int24 operator++(Int24 v) 
		{
			return new Int24((int)(v + 1));
		}

		public static Int24 operator--(Int24 v) 
		{
			return new Int24((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(24 downto 0)", "T_INT25")]
	public struct Int25 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 25;

		public Int25(int v)
		{
			this.Value = (int)(v & 0x1ffffff);
		}

		public static implicit operator Int25(int v)
		{
			return new Int25(v);
		}

		public static implicit operator int(Int25 v)
		{
			return (int)(v.Value & 0x1ffffff);
		}

		public static Int25 operator++(Int25 v) 
		{
			return new Int25((int)(v + 1));
		}

		public static Int25 operator--(Int25 v) 
		{
			return new Int25((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(25 downto 0)", "T_INT26")]
	public struct Int26 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 26;

		public Int26(int v)
		{
			this.Value = (int)(v & 0x3ffffff);
		}

		public static implicit operator Int26(int v)
		{
			return new Int26(v);
		}

		public static implicit operator int(Int26 v)
		{
			return (int)(v.Value & 0x3ffffff);
		}

		public static Int26 operator++(Int26 v) 
		{
			return new Int26((int)(v + 1));
		}

		public static Int26 operator--(Int26 v) 
		{
			return new Int26((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(26 downto 0)", "T_INT27")]
	public struct Int27 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 27;

		public Int27(int v)
		{
			this.Value = (int)(v & 0x7ffffff);
		}

		public static implicit operator Int27(int v)
		{
			return new Int27(v);
		}

		public static implicit operator int(Int27 v)
		{
			return (int)(v.Value & 0x7ffffff);
		}

		public static Int27 operator++(Int27 v) 
		{
			return new Int27((int)(v + 1));
		}

		public static Int27 operator--(Int27 v) 
		{
			return new Int27((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(27 downto 0)", "T_INT28")]
	public struct Int28 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 28;

		public Int28(int v)
		{
			this.Value = (int)(v & 0xfffffff);
		}

		public static implicit operator Int28(int v)
		{
			return new Int28(v);
		}

		public static implicit operator int(Int28 v)
		{
			return (int)(v.Value & 0xfffffff);
		}

		public static Int28 operator++(Int28 v) 
		{
			return new Int28((int)(v + 1));
		}

		public static Int28 operator--(Int28 v) 
		{
			return new Int28((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(28 downto 0)", "T_INT29")]
	public struct Int29 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 29;

		public Int29(int v)
		{
			this.Value = (int)(v & 0x1fffffff);
		}

		public static implicit operator Int29(int v)
		{
			return new Int29(v);
		}

		public static implicit operator int(Int29 v)
		{
			return (int)(v.Value & 0x1fffffff);
		}

		public static Int29 operator++(Int29 v) 
		{
			return new Int29((int)(v + 1));
		}

		public static Int29 operator--(Int29 v) 
		{
			return new Int29((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(29 downto 0)", "T_INT30")]
	public struct Int30 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 30;

		public Int30(int v)
		{
			this.Value = (int)(v & 0x3fffffff);
		}

		public static implicit operator Int30(int v)
		{
			return new Int30(v);
		}

		public static implicit operator int(Int30 v)
		{
			return (int)(v.Value & 0x3fffffff);
		}

		public static Int30 operator++(Int30 v) 
		{
			return new Int30((int)(v + 1));
		}

		public static Int30 operator--(Int30 v) 
		{
			return new Int30((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(30 downto 0)", "T_INT31")]
	public struct Int31 : SME.Tracer.ITracerSerializable {

		private readonly int Value;
		private const int WIDTH = 31;

		public Int31(int v)
		{
			this.Value = (int)(v & 0x7fffffff);
		}

		public static implicit operator Int31(int v)
		{
			return new Int31(v);
		}

		public static implicit operator int(Int31 v)
		{
			return (int)(v.Value & 0x7fffffff);
		}

		public static Int31 operator++(Int31 v) 
		{
			return new Int31((int)(v + 1));
		}

		public static Int31 operator--(Int31 v) 
		{
			return new Int31((int)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(32 downto 0)", "T_INT33")]
	public struct Int33 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 33;

		public Int33(long v)
		{
			this.Value = (long)(v & 0x1ffffffff);
		}

		public static implicit operator Int33(long v)
		{
			return new Int33(v);
		}

		public static implicit operator long(Int33 v)
		{
			return (long)(v.Value & 0x1ffffffff);
		}

		public static Int33 operator++(Int33 v) 
		{
			return new Int33((long)(v + 1));
		}

		public static Int33 operator--(Int33 v) 
		{
			return new Int33((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(33 downto 0)", "T_INT34")]
	public struct Int34 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 34;

		public Int34(long v)
		{
			this.Value = (long)(v & 0x3ffffffff);
		}

		public static implicit operator Int34(long v)
		{
			return new Int34(v);
		}

		public static implicit operator long(Int34 v)
		{
			return (long)(v.Value & 0x3ffffffff);
		}

		public static Int34 operator++(Int34 v) 
		{
			return new Int34((long)(v + 1));
		}

		public static Int34 operator--(Int34 v) 
		{
			return new Int34((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(34 downto 0)", "T_INT35")]
	public struct Int35 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 35;

		public Int35(long v)
		{
			this.Value = (long)(v & 0x7ffffffff);
		}

		public static implicit operator Int35(long v)
		{
			return new Int35(v);
		}

		public static implicit operator long(Int35 v)
		{
			return (long)(v.Value & 0x7ffffffff);
		}

		public static Int35 operator++(Int35 v) 
		{
			return new Int35((long)(v + 1));
		}

		public static Int35 operator--(Int35 v) 
		{
			return new Int35((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(35 downto 0)", "T_INT36")]
	public struct Int36 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 36;

		public Int36(long v)
		{
			this.Value = (long)(v & 0xfffffffff);
		}

		public static implicit operator Int36(long v)
		{
			return new Int36(v);
		}

		public static implicit operator long(Int36 v)
		{
			return (long)(v.Value & 0xfffffffff);
		}

		public static Int36 operator++(Int36 v) 
		{
			return new Int36((long)(v + 1));
		}

		public static Int36 operator--(Int36 v) 
		{
			return new Int36((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(36 downto 0)", "T_INT37")]
	public struct Int37 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 37;

		public Int37(long v)
		{
			this.Value = (long)(v & 0x1fffffffff);
		}

		public static implicit operator Int37(long v)
		{
			return new Int37(v);
		}

		public static implicit operator long(Int37 v)
		{
			return (long)(v.Value & 0x1fffffffff);
		}

		public static Int37 operator++(Int37 v) 
		{
			return new Int37((long)(v + 1));
		}

		public static Int37 operator--(Int37 v) 
		{
			return new Int37((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(37 downto 0)", "T_INT38")]
	public struct Int38 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 38;

		public Int38(long v)
		{
			this.Value = (long)(v & 0x3fffffffff);
		}

		public static implicit operator Int38(long v)
		{
			return new Int38(v);
		}

		public static implicit operator long(Int38 v)
		{
			return (long)(v.Value & 0x3fffffffff);
		}

		public static Int38 operator++(Int38 v) 
		{
			return new Int38((long)(v + 1));
		}

		public static Int38 operator--(Int38 v) 
		{
			return new Int38((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(38 downto 0)", "T_INT39")]
	public struct Int39 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 39;

		public Int39(long v)
		{
			this.Value = (long)(v & 0x7fffffffff);
		}

		public static implicit operator Int39(long v)
		{
			return new Int39(v);
		}

		public static implicit operator long(Int39 v)
		{
			return (long)(v.Value & 0x7fffffffff);
		}

		public static Int39 operator++(Int39 v) 
		{
			return new Int39((long)(v + 1));
		}

		public static Int39 operator--(Int39 v) 
		{
			return new Int39((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(39 downto 0)", "T_INT40")]
	public struct Int40 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 40;

		public Int40(long v)
		{
			this.Value = (long)(v & 0xffffffffff);
		}

		public static implicit operator Int40(long v)
		{
			return new Int40(v);
		}

		public static implicit operator long(Int40 v)
		{
			return (long)(v.Value & 0xffffffffff);
		}

		public static Int40 operator++(Int40 v) 
		{
			return new Int40((long)(v + 1));
		}

		public static Int40 operator--(Int40 v) 
		{
			return new Int40((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(40 downto 0)", "T_INT41")]
	public struct Int41 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 41;

		public Int41(long v)
		{
			this.Value = (long)(v & 0x1ffffffffff);
		}

		public static implicit operator Int41(long v)
		{
			return new Int41(v);
		}

		public static implicit operator long(Int41 v)
		{
			return (long)(v.Value & 0x1ffffffffff);
		}

		public static Int41 operator++(Int41 v) 
		{
			return new Int41((long)(v + 1));
		}

		public static Int41 operator--(Int41 v) 
		{
			return new Int41((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(41 downto 0)", "T_INT42")]
	public struct Int42 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 42;

		public Int42(long v)
		{
			this.Value = (long)(v & 0x3ffffffffff);
		}

		public static implicit operator Int42(long v)
		{
			return new Int42(v);
		}

		public static implicit operator long(Int42 v)
		{
			return (long)(v.Value & 0x3ffffffffff);
		}

		public static Int42 operator++(Int42 v) 
		{
			return new Int42((long)(v + 1));
		}

		public static Int42 operator--(Int42 v) 
		{
			return new Int42((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(42 downto 0)", "T_INT43")]
	public struct Int43 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 43;

		public Int43(long v)
		{
			this.Value = (long)(v & 0x7ffffffffff);
		}

		public static implicit operator Int43(long v)
		{
			return new Int43(v);
		}

		public static implicit operator long(Int43 v)
		{
			return (long)(v.Value & 0x7ffffffffff);
		}

		public static Int43 operator++(Int43 v) 
		{
			return new Int43((long)(v + 1));
		}

		public static Int43 operator--(Int43 v) 
		{
			return new Int43((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(43 downto 0)", "T_INT44")]
	public struct Int44 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 44;

		public Int44(long v)
		{
			this.Value = (long)(v & 0xfffffffffff);
		}

		public static implicit operator Int44(long v)
		{
			return new Int44(v);
		}

		public static implicit operator long(Int44 v)
		{
			return (long)(v.Value & 0xfffffffffff);
		}

		public static Int44 operator++(Int44 v) 
		{
			return new Int44((long)(v + 1));
		}

		public static Int44 operator--(Int44 v) 
		{
			return new Int44((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(44 downto 0)", "T_INT45")]
	public struct Int45 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 45;

		public Int45(long v)
		{
			this.Value = (long)(v & 0x1fffffffffff);
		}

		public static implicit operator Int45(long v)
		{
			return new Int45(v);
		}

		public static implicit operator long(Int45 v)
		{
			return (long)(v.Value & 0x1fffffffffff);
		}

		public static Int45 operator++(Int45 v) 
		{
			return new Int45((long)(v + 1));
		}

		public static Int45 operator--(Int45 v) 
		{
			return new Int45((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(45 downto 0)", "T_INT46")]
	public struct Int46 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 46;

		public Int46(long v)
		{
			this.Value = (long)(v & 0x3fffffffffff);
		}

		public static implicit operator Int46(long v)
		{
			return new Int46(v);
		}

		public static implicit operator long(Int46 v)
		{
			return (long)(v.Value & 0x3fffffffffff);
		}

		public static Int46 operator++(Int46 v) 
		{
			return new Int46((long)(v + 1));
		}

		public static Int46 operator--(Int46 v) 
		{
			return new Int46((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(46 downto 0)", "T_INT47")]
	public struct Int47 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 47;

		public Int47(long v)
		{
			this.Value = (long)(v & 0x7fffffffffff);
		}

		public static implicit operator Int47(long v)
		{
			return new Int47(v);
		}

		public static implicit operator long(Int47 v)
		{
			return (long)(v.Value & 0x7fffffffffff);
		}

		public static Int47 operator++(Int47 v) 
		{
			return new Int47((long)(v + 1));
		}

		public static Int47 operator--(Int47 v) 
		{
			return new Int47((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(47 downto 0)", "T_INT48")]
	public struct Int48 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 48;

		public Int48(long v)
		{
			this.Value = (long)(v & 0xffffffffffff);
		}

		public static implicit operator Int48(long v)
		{
			return new Int48(v);
		}

		public static implicit operator long(Int48 v)
		{
			return (long)(v.Value & 0xffffffffffff);
		}

		public static Int48 operator++(Int48 v) 
		{
			return new Int48((long)(v + 1));
		}

		public static Int48 operator--(Int48 v) 
		{
			return new Int48((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(48 downto 0)", "T_INT49")]
	public struct Int49 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 49;

		public Int49(long v)
		{
			this.Value = (long)(v & 0x1ffffffffffff);
		}

		public static implicit operator Int49(long v)
		{
			return new Int49(v);
		}

		public static implicit operator long(Int49 v)
		{
			return (long)(v.Value & 0x1ffffffffffff);
		}

		public static Int49 operator++(Int49 v) 
		{
			return new Int49((long)(v + 1));
		}

		public static Int49 operator--(Int49 v) 
		{
			return new Int49((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(49 downto 0)", "T_INT50")]
	public struct Int50 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 50;

		public Int50(long v)
		{
			this.Value = (long)(v & 0x3ffffffffffff);
		}

		public static implicit operator Int50(long v)
		{
			return new Int50(v);
		}

		public static implicit operator long(Int50 v)
		{
			return (long)(v.Value & 0x3ffffffffffff);
		}

		public static Int50 operator++(Int50 v) 
		{
			return new Int50((long)(v + 1));
		}

		public static Int50 operator--(Int50 v) 
		{
			return new Int50((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(50 downto 0)", "T_INT51")]
	public struct Int51 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 51;

		public Int51(long v)
		{
			this.Value = (long)(v & 0x7ffffffffffff);
		}

		public static implicit operator Int51(long v)
		{
			return new Int51(v);
		}

		public static implicit operator long(Int51 v)
		{
			return (long)(v.Value & 0x7ffffffffffff);
		}

		public static Int51 operator++(Int51 v) 
		{
			return new Int51((long)(v + 1));
		}

		public static Int51 operator--(Int51 v) 
		{
			return new Int51((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(51 downto 0)", "T_INT52")]
	public struct Int52 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 52;

		public Int52(long v)
		{
			this.Value = (long)(v & 0xfffffffffffff);
		}

		public static implicit operator Int52(long v)
		{
			return new Int52(v);
		}

		public static implicit operator long(Int52 v)
		{
			return (long)(v.Value & 0xfffffffffffff);
		}

		public static Int52 operator++(Int52 v) 
		{
			return new Int52((long)(v + 1));
		}

		public static Int52 operator--(Int52 v) 
		{
			return new Int52((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(52 downto 0)", "T_INT53")]
	public struct Int53 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 53;

		public Int53(long v)
		{
			this.Value = (long)(v & 0x1fffffffffffff);
		}

		public static implicit operator Int53(long v)
		{
			return new Int53(v);
		}

		public static implicit operator long(Int53 v)
		{
			return (long)(v.Value & 0x1fffffffffffff);
		}

		public static Int53 operator++(Int53 v) 
		{
			return new Int53((long)(v + 1));
		}

		public static Int53 operator--(Int53 v) 
		{
			return new Int53((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(53 downto 0)", "T_INT54")]
	public struct Int54 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 54;

		public Int54(long v)
		{
			this.Value = (long)(v & 0x3fffffffffffff);
		}

		public static implicit operator Int54(long v)
		{
			return new Int54(v);
		}

		public static implicit operator long(Int54 v)
		{
			return (long)(v.Value & 0x3fffffffffffff);
		}

		public static Int54 operator++(Int54 v) 
		{
			return new Int54((long)(v + 1));
		}

		public static Int54 operator--(Int54 v) 
		{
			return new Int54((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(54 downto 0)", "T_INT55")]
	public struct Int55 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 55;

		public Int55(long v)
		{
			this.Value = (long)(v & 0x7fffffffffffff);
		}

		public static implicit operator Int55(long v)
		{
			return new Int55(v);
		}

		public static implicit operator long(Int55 v)
		{
			return (long)(v.Value & 0x7fffffffffffff);
		}

		public static Int55 operator++(Int55 v) 
		{
			return new Int55((long)(v + 1));
		}

		public static Int55 operator--(Int55 v) 
		{
			return new Int55((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(55 downto 0)", "T_INT56")]
	public struct Int56 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 56;

		public Int56(long v)
		{
			this.Value = (long)(v & 0xffffffffffffff);
		}

		public static implicit operator Int56(long v)
		{
			return new Int56(v);
		}

		public static implicit operator long(Int56 v)
		{
			return (long)(v.Value & 0xffffffffffffff);
		}

		public static Int56 operator++(Int56 v) 
		{
			return new Int56((long)(v + 1));
		}

		public static Int56 operator--(Int56 v) 
		{
			return new Int56((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(56 downto 0)", "T_INT57")]
	public struct Int57 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 57;

		public Int57(long v)
		{
			this.Value = (long)(v & 0x1ffffffffffffff);
		}

		public static implicit operator Int57(long v)
		{
			return new Int57(v);
		}

		public static implicit operator long(Int57 v)
		{
			return (long)(v.Value & 0x1ffffffffffffff);
		}

		public static Int57 operator++(Int57 v) 
		{
			return new Int57((long)(v + 1));
		}

		public static Int57 operator--(Int57 v) 
		{
			return new Int57((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(57 downto 0)", "T_INT58")]
	public struct Int58 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 58;

		public Int58(long v)
		{
			this.Value = (long)(v & 0x3ffffffffffffff);
		}

		public static implicit operator Int58(long v)
		{
			return new Int58(v);
		}

		public static implicit operator long(Int58 v)
		{
			return (long)(v.Value & 0x3ffffffffffffff);
		}

		public static Int58 operator++(Int58 v) 
		{
			return new Int58((long)(v + 1));
		}

		public static Int58 operator--(Int58 v) 
		{
			return new Int58((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(58 downto 0)", "T_INT59")]
	public struct Int59 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 59;

		public Int59(long v)
		{
			this.Value = (long)(v & 0x7ffffffffffffff);
		}

		public static implicit operator Int59(long v)
		{
			return new Int59(v);
		}

		public static implicit operator long(Int59 v)
		{
			return (long)(v.Value & 0x7ffffffffffffff);
		}

		public static Int59 operator++(Int59 v) 
		{
			return new Int59((long)(v + 1));
		}

		public static Int59 operator--(Int59 v) 
		{
			return new Int59((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(59 downto 0)", "T_INT60")]
	public struct Int60 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 60;

		public Int60(long v)
		{
			this.Value = (long)(v & 0xfffffffffffffff);
		}

		public static implicit operator Int60(long v)
		{
			return new Int60(v);
		}

		public static implicit operator long(Int60 v)
		{
			return (long)(v.Value & 0xfffffffffffffff);
		}

		public static Int60 operator++(Int60 v) 
		{
			return new Int60((long)(v + 1));
		}

		public static Int60 operator--(Int60 v) 
		{
			return new Int60((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(60 downto 0)", "T_INT61")]
	public struct Int61 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 61;

		public Int61(long v)
		{
			this.Value = (long)(v & 0x1fffffffffffffff);
		}

		public static implicit operator Int61(long v)
		{
			return new Int61(v);
		}

		public static implicit operator long(Int61 v)
		{
			return (long)(v.Value & 0x1fffffffffffffff);
		}

		public static Int61 operator++(Int61 v) 
		{
			return new Int61((long)(v + 1));
		}

		public static Int61 operator--(Int61 v) 
		{
			return new Int61((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(61 downto 0)", "T_INT62")]
	public struct Int62 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 62;

		public Int62(long v)
		{
			this.Value = (long)(v & 0x3fffffffffffffff);
		}

		public static implicit operator Int62(long v)
		{
			return new Int62(v);
		}

		public static implicit operator long(Int62 v)
		{
			return (long)(v.Value & 0x3fffffffffffffff);
		}

		public static Int62 operator++(Int62 v) 
		{
			return new Int62((long)(v + 1));
		}

		public static Int62 operator--(Int62 v) 
		{
			return new Int62((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

	[DebuggerDisplay("{Value}")]
	[VHDLType("STD_LOGIC_VECTOR(62 downto 0)", "T_INT63")]
	public struct Int63 : SME.Tracer.ITracerSerializable {

		private readonly long Value;
		private const int WIDTH = 63;

		public Int63(long v)
		{
			this.Value = (long)(v & 0x7fffffffffffffff);
		}

		public static implicit operator Int63(long v)
		{
			return new Int63(v);
		}

		public static implicit operator long(Int63 v)
		{
			return (long)(v.Value & 0x7fffffffffffffff);
		}

		public static Int63 operator++(Int63 v) 
		{
			return new Int63((long)(v + 1));
		}

		public static Int63 operator--(Int63 v) 
		{
			return new Int63((long)(v - 1));
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		string SME.Tracer.ITracerSerializable.Serialize(SME.Tracer.Tracer tracer)
		{
			return IntFormatHelper.ToBinaryString(this.Value, WIDTH);
		}
	};

}
