
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

package SYSTEM_TYPES is
    subtype T_SYSTEM_BOOL   is std_logic;

    subtype T_SYSTEM_UINT8  is std_logic_vector(7 downto 0);
    subtype T_SYSTEM_UINT16 is std_logic_vector(15 downto 0);
    subtype T_SYSTEM_UINT32 is std_logic_vector(31 downto 0);
    subtype T_SYSTEM_UINT64 is std_logic_vector(63 downto 0);

    subtype T_SYSTEM_INT8  is std_logic_vector(7 downto 0);
    subtype T_SYSTEM_INT16 is std_logic_vector(15 downto 0);
    subtype T_SYSTEM_INT32 is std_logic_vector(31 downto 0);
    subtype T_SYSTEM_INT64 is std_logic_vector(63 downto 0);

    type T_SYSTEM_UINT8_ARRAY  is array(natural range <>) of T_SYSTEM_UINT8;
    type T_SYSTEM_UINT16_ARRAY is array(natural range <>) of T_SYSTEM_UINT16;
    type T_SYSTEM_UINT32_ARRAY is array(natural range <>) of T_SYSTEM_UINT32;
    type T_SYSTEM_UINT64_ARRAY is array(natural range <>) of T_SYSTEM_UINT64;

    type T_SYSTEM_INT8_ARRAY  is array(natural range <>) of T_SYSTEM_INT8;
    type T_SYSTEM_INT16_ARRAY is array(natural range <>) of T_SYSTEM_INT16;
    type T_SYSTEM_INT32_ARRAY is array(natural range <>) of T_SYSTEM_INT32;
    type T_SYSTEM_INT64_ARRAY is array(natural range <>) of T_SYSTEM_INT64;


    subtype T_UINT1 is std_logic_vector(0 downto 0);
    subtype T_UINT2 is std_logic_vector(1 downto 0);
    subtype T_UINT3 is std_logic_vector(2 downto 0);
    subtype T_UINT4 is std_logic_vector(3 downto 0);
    subtype T_UINT5 is std_logic_vector(4 downto 0);
    subtype T_UINT6 is std_logic_vector(5 downto 0);
    subtype T_UINT7 is std_logic_vector(6 downto 0);
    subtype T_UINT9 is std_logic_vector(8 downto 0);
    subtype T_UINT10 is std_logic_vector(9 downto 0);
    subtype T_UINT11 is std_logic_vector(10 downto 0);
    subtype T_UINT12 is std_logic_vector(11 downto 0);
    subtype T_UINT13 is std_logic_vector(12 downto 0);
    subtype T_UINT14 is std_logic_vector(13 downto 0);
    subtype T_UINT15 is std_logic_vector(14 downto 0);
    subtype T_UINT17 is std_logic_vector(16 downto 0);
    subtype T_UINT18 is std_logic_vector(17 downto 0);
    subtype T_UINT19 is std_logic_vector(18 downto 0);
    subtype T_UINT20 is std_logic_vector(19 downto 0);
    subtype T_UINT21 is std_logic_vector(20 downto 0);
    subtype T_UINT22 is std_logic_vector(21 downto 0);
    subtype T_UINT23 is std_logic_vector(22 downto 0);
    subtype T_UINT24 is std_logic_vector(23 downto 0);
    subtype T_UINT25 is std_logic_vector(24 downto 0);
    subtype T_UINT26 is std_logic_vector(25 downto 0);
    subtype T_UINT27 is std_logic_vector(26 downto 0);
    subtype T_UINT28 is std_logic_vector(27 downto 0);
    subtype T_UINT29 is std_logic_vector(28 downto 0);
    subtype T_UINT30 is std_logic_vector(29 downto 0);
    subtype T_UINT31 is std_logic_vector(30 downto 0);
    subtype T_UINT33 is std_logic_vector(32 downto 0);
    subtype T_UINT34 is std_logic_vector(33 downto 0);
    subtype T_UINT35 is std_logic_vector(34 downto 0);
    subtype T_UINT36 is std_logic_vector(35 downto 0);
    subtype T_UINT37 is std_logic_vector(36 downto 0);
    subtype T_UINT38 is std_logic_vector(37 downto 0);
    subtype T_UINT39 is std_logic_vector(38 downto 0);
    subtype T_UINT40 is std_logic_vector(39 downto 0);
    subtype T_UINT41 is std_logic_vector(40 downto 0);
    subtype T_UINT42 is std_logic_vector(41 downto 0);
    subtype T_UINT43 is std_logic_vector(42 downto 0);
    subtype T_UINT44 is std_logic_vector(43 downto 0);
    subtype T_UINT45 is std_logic_vector(44 downto 0);
    subtype T_UINT46 is std_logic_vector(45 downto 0);
    subtype T_UINT47 is std_logic_vector(46 downto 0);
    subtype T_UINT48 is std_logic_vector(47 downto 0);
    subtype T_UINT49 is std_logic_vector(48 downto 0);
    subtype T_UINT50 is std_logic_vector(49 downto 0);
    subtype T_UINT51 is std_logic_vector(50 downto 0);
    subtype T_UINT52 is std_logic_vector(51 downto 0);
    subtype T_UINT53 is std_logic_vector(52 downto 0);
    subtype T_UINT54 is std_logic_vector(53 downto 0);
    subtype T_UINT55 is std_logic_vector(54 downto 0);
    subtype T_UINT56 is std_logic_vector(55 downto 0);
    subtype T_UINT57 is std_logic_vector(56 downto 0);
    subtype T_UINT58 is std_logic_vector(57 downto 0);
    subtype T_UINT59 is std_logic_vector(58 downto 0);
    subtype T_UINT60 is std_logic_vector(59 downto 0);
    subtype T_UINT61 is std_logic_vector(60 downto 0);
    subtype T_UINT62 is std_logic_vector(61 downto 0);
    subtype T_UINT63 is std_logic_vector(62 downto 0);

    subtype T_INT1 is std_logic_vector(0 downto 0);
    subtype T_INT2 is std_logic_vector(1 downto 0);
    subtype T_INT3 is std_logic_vector(2 downto 0);
    subtype T_INT4 is std_logic_vector(3 downto 0);
    subtype T_INT5 is std_logic_vector(4 downto 0);
    subtype T_INT6 is std_logic_vector(5 downto 0);
    subtype T_INT7 is std_logic_vector(6 downto 0);
    subtype T_INT9 is std_logic_vector(8 downto 0);
    subtype T_INT10 is std_logic_vector(9 downto 0);
    subtype T_INT11 is std_logic_vector(10 downto 0);
    subtype T_INT12 is std_logic_vector(11 downto 0);
    subtype T_INT13 is std_logic_vector(12 downto 0);
    subtype T_INT14 is std_logic_vector(13 downto 0);
    subtype T_INT15 is std_logic_vector(14 downto 0);
    subtype T_INT17 is std_logic_vector(16 downto 0);
    subtype T_INT18 is std_logic_vector(17 downto 0);
    subtype T_INT19 is std_logic_vector(18 downto 0);
    subtype T_INT20 is std_logic_vector(19 downto 0);
    subtype T_INT21 is std_logic_vector(20 downto 0);
    subtype T_INT22 is std_logic_vector(21 downto 0);
    subtype T_INT23 is std_logic_vector(22 downto 0);
    subtype T_INT24 is std_logic_vector(23 downto 0);
    subtype T_INT25 is std_logic_vector(24 downto 0);
    subtype T_INT26 is std_logic_vector(25 downto 0);
    subtype T_INT27 is std_logic_vector(26 downto 0);
    subtype T_INT28 is std_logic_vector(27 downto 0);
    subtype T_INT29 is std_logic_vector(28 downto 0);
    subtype T_INT30 is std_logic_vector(29 downto 0);
    subtype T_INT31 is std_logic_vector(30 downto 0);
    subtype T_INT33 is std_logic_vector(32 downto 0);
    subtype T_INT34 is std_logic_vector(33 downto 0);
    subtype T_INT35 is std_logic_vector(34 downto 0);
    subtype T_INT36 is std_logic_vector(35 downto 0);
    subtype T_INT37 is std_logic_vector(36 downto 0);
    subtype T_INT38 is std_logic_vector(37 downto 0);
    subtype T_INT39 is std_logic_vector(38 downto 0);
    subtype T_INT40 is std_logic_vector(39 downto 0);
    subtype T_INT41 is std_logic_vector(40 downto 0);
    subtype T_INT42 is std_logic_vector(41 downto 0);
    subtype T_INT43 is std_logic_vector(42 downto 0);
    subtype T_INT44 is std_logic_vector(43 downto 0);
    subtype T_INT45 is std_logic_vector(44 downto 0);
    subtype T_INT46 is std_logic_vector(45 downto 0);
    subtype T_INT47 is std_logic_vector(46 downto 0);
    subtype T_INT48 is std_logic_vector(47 downto 0);
    subtype T_INT49 is std_logic_vector(48 downto 0);
    subtype T_INT50 is std_logic_vector(49 downto 0);
    subtype T_INT51 is std_logic_vector(50 downto 0);
    subtype T_INT52 is std_logic_vector(51 downto 0);
    subtype T_INT53 is std_logic_vector(52 downto 0);
    subtype T_INT54 is std_logic_vector(53 downto 0);
    subtype T_INT55 is std_logic_vector(54 downto 0);
    subtype T_INT56 is std_logic_vector(55 downto 0);
    subtype T_INT57 is std_logic_vector(56 downto 0);
    subtype T_INT58 is std_logic_vector(57 downto 0);
    subtype T_INT59 is std_logic_vector(58 downto 0);
    subtype T_INT60 is std_logic_vector(59 downto 0);
    subtype T_INT61 is std_logic_vector(60 downto 0);
    subtype T_INT62 is std_logic_vector(61 downto 0);
    subtype T_INT63 is std_logic_vector(62 downto 0);

    type T_UINT1_ARRAY is array(natural range <>) of T_UINT1;
    type T_UINT2_ARRAY is array(natural range <>) of T_UINT2;
    type T_UINT3_ARRAY is array(natural range <>) of T_UINT3;
    type T_UINT4_ARRAY is array(natural range <>) of T_UINT4;
    type T_UINT5_ARRAY is array(natural range <>) of T_UINT5;
    type T_UINT6_ARRAY is array(natural range <>) of T_UINT6;
    type T_UINT7_ARRAY is array(natural range <>) of T_UINT7;
    type T_UINT9_ARRAY is array(natural range <>) of T_UINT9;
    type T_UINT10_ARRAY is array(natural range <>) of T_UINT10;
    type T_UINT11_ARRAY is array(natural range <>) of T_UINT11;
    type T_UINT12_ARRAY is array(natural range <>) of T_UINT12;
    type T_UINT13_ARRAY is array(natural range <>) of T_UINT13;
    type T_UINT14_ARRAY is array(natural range <>) of T_UINT14;
    type T_UINT15_ARRAY is array(natural range <>) of T_UINT15;
    type T_UINT17_ARRAY is array(natural range <>) of T_UINT17;
    type T_UINT18_ARRAY is array(natural range <>) of T_UINT18;
    type T_UINT19_ARRAY is array(natural range <>) of T_UINT19;
    type T_UINT20_ARRAY is array(natural range <>) of T_UINT20;
    type T_UINT21_ARRAY is array(natural range <>) of T_UINT21;
    type T_UINT22_ARRAY is array(natural range <>) of T_UINT22;
    type T_UINT23_ARRAY is array(natural range <>) of T_UINT23;
    type T_UINT24_ARRAY is array(natural range <>) of T_UINT24;
    type T_UINT25_ARRAY is array(natural range <>) of T_UINT25;
    type T_UINT26_ARRAY is array(natural range <>) of T_UINT26;
    type T_UINT27_ARRAY is array(natural range <>) of T_UINT27;
    type T_UINT28_ARRAY is array(natural range <>) of T_UINT28;
    type T_UINT29_ARRAY is array(natural range <>) of T_UINT29;
    type T_UINT30_ARRAY is array(natural range <>) of T_UINT30;
    type T_UINT31_ARRAY is array(natural range <>) of T_UINT31;
    type T_UINT33_ARRAY is array(natural range <>) of T_UINT33;
    type T_UINT34_ARRAY is array(natural range <>) of T_UINT34;
    type T_UINT35_ARRAY is array(natural range <>) of T_UINT35;
    type T_UINT36_ARRAY is array(natural range <>) of T_UINT36;
    type T_UINT37_ARRAY is array(natural range <>) of T_UINT37;
    type T_UINT38_ARRAY is array(natural range <>) of T_UINT38;
    type T_UINT39_ARRAY is array(natural range <>) of T_UINT39;
    type T_UINT40_ARRAY is array(natural range <>) of T_UINT40;
    type T_UINT41_ARRAY is array(natural range <>) of T_UINT41;
    type T_UINT42_ARRAY is array(natural range <>) of T_UINT42;
    type T_UINT43_ARRAY is array(natural range <>) of T_UINT43;
    type T_UINT44_ARRAY is array(natural range <>) of T_UINT44;
    type T_UINT45_ARRAY is array(natural range <>) of T_UINT45;
    type T_UINT46_ARRAY is array(natural range <>) of T_UINT46;
    type T_UINT47_ARRAY is array(natural range <>) of T_UINT47;
    type T_UINT48_ARRAY is array(natural range <>) of T_UINT48;
    type T_UINT49_ARRAY is array(natural range <>) of T_UINT49;
    type T_UINT50_ARRAY is array(natural range <>) of T_UINT50;
    type T_UINT51_ARRAY is array(natural range <>) of T_UINT51;
    type T_UINT52_ARRAY is array(natural range <>) of T_UINT52;
    type T_UINT53_ARRAY is array(natural range <>) of T_UINT53;
    type T_UINT54_ARRAY is array(natural range <>) of T_UINT54;
    type T_UINT55_ARRAY is array(natural range <>) of T_UINT55;
    type T_UINT56_ARRAY is array(natural range <>) of T_UINT56;
    type T_UINT57_ARRAY is array(natural range <>) of T_UINT57;
    type T_UINT58_ARRAY is array(natural range <>) of T_UINT58;
    type T_UINT59_ARRAY is array(natural range <>) of T_UINT59;
    type T_UINT60_ARRAY is array(natural range <>) of T_UINT60;
    type T_UINT61_ARRAY is array(natural range <>) of T_UINT61;
    type T_UINT62_ARRAY is array(natural range <>) of T_UINT62;
    type T_UINT63_ARRAY is array(natural range <>) of T_UINT63;

    type T_INT1_ARRAY is array(natural range <>) of T_INT1;
    type T_INT2_ARRAY is array(natural range <>) of T_INT2;
    type T_INT3_ARRAY is array(natural range <>) of T_INT3;
    type T_INT4_ARRAY is array(natural range <>) of T_INT4;
    type T_INT5_ARRAY is array(natural range <>) of T_INT5;
    type T_INT6_ARRAY is array(natural range <>) of T_INT6;
    type T_INT7_ARRAY is array(natural range <>) of T_INT7;
    type T_INT9_ARRAY is array(natural range <>) of T_INT9;
    type T_INT10_ARRAY is array(natural range <>) of T_INT10;
    type T_INT11_ARRAY is array(natural range <>) of T_INT11;
    type T_INT12_ARRAY is array(natural range <>) of T_INT12;
    type T_INT13_ARRAY is array(natural range <>) of T_INT13;
    type T_INT14_ARRAY is array(natural range <>) of T_INT14;
    type T_INT15_ARRAY is array(natural range <>) of T_INT15;
    type T_INT17_ARRAY is array(natural range <>) of T_INT17;
    type T_INT18_ARRAY is array(natural range <>) of T_INT18;
    type T_INT19_ARRAY is array(natural range <>) of T_INT19;
    type T_INT20_ARRAY is array(natural range <>) of T_INT20;
    type T_INT21_ARRAY is array(natural range <>) of T_INT21;
    type T_INT22_ARRAY is array(natural range <>) of T_INT22;
    type T_INT23_ARRAY is array(natural range <>) of T_INT23;
    type T_INT24_ARRAY is array(natural range <>) of T_INT24;
    type T_INT25_ARRAY is array(natural range <>) of T_INT25;
    type T_INT26_ARRAY is array(natural range <>) of T_INT26;
    type T_INT27_ARRAY is array(natural range <>) of T_INT27;
    type T_INT28_ARRAY is array(natural range <>) of T_INT28;
    type T_INT29_ARRAY is array(natural range <>) of T_INT29;
    type T_INT30_ARRAY is array(natural range <>) of T_INT30;
    type T_INT31_ARRAY is array(natural range <>) of T_INT31;
    type T_INT33_ARRAY is array(natural range <>) of T_INT33;
    type T_INT34_ARRAY is array(natural range <>) of T_INT34;
    type T_INT35_ARRAY is array(natural range <>) of T_INT35;
    type T_INT36_ARRAY is array(natural range <>) of T_INT36;
    type T_INT37_ARRAY is array(natural range <>) of T_INT37;
    type T_INT38_ARRAY is array(natural range <>) of T_INT38;
    type T_INT39_ARRAY is array(natural range <>) of T_INT39;
    type T_INT40_ARRAY is array(natural range <>) of T_INT40;
    type T_INT41_ARRAY is array(natural range <>) of T_INT41;
    type T_INT42_ARRAY is array(natural range <>) of T_INT42;
    type T_INT43_ARRAY is array(natural range <>) of T_INT43;
    type T_INT44_ARRAY is array(natural range <>) of T_INT44;
    type T_INT45_ARRAY is array(natural range <>) of T_INT45;
    type T_INT46_ARRAY is array(natural range <>) of T_INT46;
    type T_INT47_ARRAY is array(natural range <>) of T_INT47;
    type T_INT48_ARRAY is array(natural range <>) of T_INT48;
    type T_INT49_ARRAY is array(natural range <>) of T_INT49;
    type T_INT50_ARRAY is array(natural range <>) of T_INT50;
    type T_INT51_ARRAY is array(natural range <>) of T_INT51;
    type T_INT52_ARRAY is array(natural range <>) of T_INT52;
    type T_INT53_ARRAY is array(natural range <>) of T_INT53;
    type T_INT54_ARRAY is array(natural range <>) of T_INT54;
    type T_INT55_ARRAY is array(natural range <>) of T_INT55;
    type T_INT56_ARRAY is array(natural range <>) of T_INT56;
    type T_INT57_ARRAY is array(natural range <>) of T_INT57;
    type T_INT58_ARRAY is array(natural range <>) of T_INT58;
    type T_INT59_ARRAY is array(natural range <>) of T_INT59;
    type T_INT60_ARRAY is array(natural range <>) of T_INT60;
    type T_INT61_ARRAY is array(natural range <>) of T_INT61;
    type T_INT62_ARRAY is array(natural range <>) of T_INT62;
    type T_INT63_ARRAY is array(natural range <>) of T_INT63;


    -- converts an integer to UINT1
    pure function UINT1(v: integer) return T_UINT1;

    -- converts an integer to UINT2
    pure function UINT2(v: integer) return T_UINT2;

    -- converts an integer to UINT3
    pure function UINT3(v: integer) return T_UINT3;

    -- converts an integer to UINT4
    pure function UINT4(v: integer) return T_UINT4;

    -- converts an integer to UINT5
    pure function UINT5(v: integer) return T_UINT5;

    -- converts an integer to UINT6
    pure function UINT6(v: integer) return T_UINT6;

    -- converts an integer to UINT7
    pure function UINT7(v: integer) return T_UINT7;

    -- converts an integer to UINT9
    pure function UINT9(v: integer) return T_UINT9;

    -- converts an integer to UINT10
    pure function UINT10(v: integer) return T_UINT10;

    -- converts an integer to UINT11
    pure function UINT11(v: integer) return T_UINT11;

    -- converts an integer to UINT12
    pure function UINT12(v: integer) return T_UINT12;

    -- converts an integer to UINT13
    pure function UINT13(v: integer) return T_UINT13;

    -- converts an integer to UINT14
    pure function UINT14(v: integer) return T_UINT14;

    -- converts an integer to UINT15
    pure function UINT15(v: integer) return T_UINT15;

    -- converts an integer to UINT17
    pure function UINT17(v: integer) return T_UINT17;

    -- converts an integer to UINT18
    pure function UINT18(v: integer) return T_UINT18;

    -- converts an integer to UINT19
    pure function UINT19(v: integer) return T_UINT19;

    -- converts an integer to UINT20
    pure function UINT20(v: integer) return T_UINT20;

    -- converts an integer to UINT21
    pure function UINT21(v: integer) return T_UINT21;

    -- converts an integer to UINT22
    pure function UINT22(v: integer) return T_UINT22;

    -- converts an integer to UINT23
    pure function UINT23(v: integer) return T_UINT23;

    -- converts an integer to UINT24
    pure function UINT24(v: integer) return T_UINT24;

    -- converts an integer to UINT25
    pure function UINT25(v: integer) return T_UINT25;

    -- converts an integer to UINT26
    pure function UINT26(v: integer) return T_UINT26;

    -- converts an integer to UINT27
    pure function UINT27(v: integer) return T_UINT27;

    -- converts an integer to UINT28
    pure function UINT28(v: integer) return T_UINT28;

    -- converts an integer to UINT29
    pure function UINT29(v: integer) return T_UINT29;

    -- converts an integer to UINT30
    pure function UINT30(v: integer) return T_UINT30;

    -- converts an integer to UINT31
    pure function UINT31(v: integer) return T_UINT31;

    -- converts an integer to UINT33
    pure function UINT33(v: integer) return T_UINT33;

    -- converts an integer to UINT34
    pure function UINT34(v: integer) return T_UINT34;

    -- converts an integer to UINT35
    pure function UINT35(v: integer) return T_UINT35;

    -- converts an integer to UINT36
    pure function UINT36(v: integer) return T_UINT36;

    -- converts an integer to UINT37
    pure function UINT37(v: integer) return T_UINT37;

    -- converts an integer to UINT38
    pure function UINT38(v: integer) return T_UINT38;

    -- converts an integer to UINT39
    pure function UINT39(v: integer) return T_UINT39;

    -- converts an integer to UINT40
    pure function UINT40(v: integer) return T_UINT40;

    -- converts an integer to UINT41
    pure function UINT41(v: integer) return T_UINT41;

    -- converts an integer to UINT42
    pure function UINT42(v: integer) return T_UINT42;

    -- converts an integer to UINT43
    pure function UINT43(v: integer) return T_UINT43;

    -- converts an integer to UINT44
    pure function UINT44(v: integer) return T_UINT44;

    -- converts an integer to UINT45
    pure function UINT45(v: integer) return T_UINT45;

    -- converts an integer to UINT46
    pure function UINT46(v: integer) return T_UINT46;

    -- converts an integer to UINT47
    pure function UINT47(v: integer) return T_UINT47;

    -- converts an integer to UINT48
    pure function UINT48(v: integer) return T_UINT48;

    -- converts an integer to UINT49
    pure function UINT49(v: integer) return T_UINT49;

    -- converts an integer to UINT50
    pure function UINT50(v: integer) return T_UINT50;

    -- converts an integer to UINT51
    pure function UINT51(v: integer) return T_UINT51;

    -- converts an integer to UINT52
    pure function UINT52(v: integer) return T_UINT52;

    -- converts an integer to UINT53
    pure function UINT53(v: integer) return T_UINT53;

    -- converts an integer to UINT54
    pure function UINT54(v: integer) return T_UINT54;

    -- converts an integer to UINT55
    pure function UINT55(v: integer) return T_UINT55;

    -- converts an integer to UINT56
    pure function UINT56(v: integer) return T_UINT56;

    -- converts an integer to UINT57
    pure function UINT57(v: integer) return T_UINT57;

    -- converts an integer to UINT58
    pure function UINT58(v: integer) return T_UINT58;

    -- converts an integer to UINT59
    pure function UINT59(v: integer) return T_UINT59;

    -- converts an integer to UINT60
    pure function UINT60(v: integer) return T_UINT60;

    -- converts an integer to UINT61
    pure function UINT61(v: integer) return T_UINT61;

    -- converts an integer to UINT62
    pure function UINT62(v: integer) return T_UINT62;

    -- converts an integer to UINT63
    pure function UINT63(v: integer) return T_UINT63;


    -- converts an integer to INT1
    pure function INT1(v: integer) return T_INT1;

    -- converts an integer to INT2
    pure function INT2(v: integer) return T_INT2;

    -- converts an integer to INT3
    pure function INT3(v: integer) return T_INT3;

    -- converts an integer to INT4
    pure function INT4(v: integer) return T_INT4;

    -- converts an integer to INT5
    pure function INT5(v: integer) return T_INT5;

    -- converts an integer to INT6
    pure function INT6(v: integer) return T_INT6;

    -- converts an integer to INT7
    pure function INT7(v: integer) return T_INT7;

    -- converts an integer to INT9
    pure function INT9(v: integer) return T_INT9;

    -- converts an integer to INT10
    pure function INT10(v: integer) return T_INT10;

    -- converts an integer to INT11
    pure function INT11(v: integer) return T_INT11;

    -- converts an integer to INT12
    pure function INT12(v: integer) return T_INT12;

    -- converts an integer to INT13
    pure function INT13(v: integer) return T_INT13;

    -- converts an integer to INT14
    pure function INT14(v: integer) return T_INT14;

    -- converts an integer to INT15
    pure function INT15(v: integer) return T_INT15;

    -- converts an integer to INT17
    pure function INT17(v: integer) return T_INT17;

    -- converts an integer to INT18
    pure function INT18(v: integer) return T_INT18;

    -- converts an integer to INT19
    pure function INT19(v: integer) return T_INT19;

    -- converts an integer to INT20
    pure function INT20(v: integer) return T_INT20;

    -- converts an integer to INT21
    pure function INT21(v: integer) return T_INT21;

    -- converts an integer to INT22
    pure function INT22(v: integer) return T_INT22;

    -- converts an integer to INT23
    pure function INT23(v: integer) return T_INT23;

    -- converts an integer to INT24
    pure function INT24(v: integer) return T_INT24;

    -- converts an integer to INT25
    pure function INT25(v: integer) return T_INT25;

    -- converts an integer to INT26
    pure function INT26(v: integer) return T_INT26;

    -- converts an integer to INT27
    pure function INT27(v: integer) return T_INT27;

    -- converts an integer to INT28
    pure function INT28(v: integer) return T_INT28;

    -- converts an integer to INT29
    pure function INT29(v: integer) return T_INT29;

    -- converts an integer to INT30
    pure function INT30(v: integer) return T_INT30;

    -- converts an integer to INT31
    pure function INT31(v: integer) return T_INT31;

    -- converts an integer to INT33
    pure function INT33(v: integer) return T_INT33;

    -- converts an integer to INT34
    pure function INT34(v: integer) return T_INT34;

    -- converts an integer to INT35
    pure function INT35(v: integer) return T_INT35;

    -- converts an integer to INT36
    pure function INT36(v: integer) return T_INT36;

    -- converts an integer to INT37
    pure function INT37(v: integer) return T_INT37;

    -- converts an integer to INT38
    pure function INT38(v: integer) return T_INT38;

    -- converts an integer to INT39
    pure function INT39(v: integer) return T_INT39;

    -- converts an integer to INT40
    pure function INT40(v: integer) return T_INT40;

    -- converts an integer to INT41
    pure function INT41(v: integer) return T_INT41;

    -- converts an integer to INT42
    pure function INT42(v: integer) return T_INT42;

    -- converts an integer to INT43
    pure function INT43(v: integer) return T_INT43;

    -- converts an integer to INT44
    pure function INT44(v: integer) return T_INT44;

    -- converts an integer to INT45
    pure function INT45(v: integer) return T_INT45;

    -- converts an integer to INT46
    pure function INT46(v: integer) return T_INT46;

    -- converts an integer to INT47
    pure function INT47(v: integer) return T_INT47;

    -- converts an integer to INT48
    pure function INT48(v: integer) return T_INT48;

    -- converts an integer to INT49
    pure function INT49(v: integer) return T_INT49;

    -- converts an integer to INT50
    pure function INT50(v: integer) return T_INT50;

    -- converts an integer to INT51
    pure function INT51(v: integer) return T_INT51;

    -- converts an integer to INT52
    pure function INT52(v: integer) return T_INT52;

    -- converts an integer to INT53
    pure function INT53(v: integer) return T_INT53;

    -- converts an integer to INT54
    pure function INT54(v: integer) return T_INT54;

    -- converts an integer to INT55
    pure function INT55(v: integer) return T_INT55;

    -- converts an integer to INT56
    pure function INT56(v: integer) return T_INT56;

    -- converts an integer to INT57
    pure function INT57(v: integer) return T_INT57;

    -- converts an integer to INT58
    pure function INT58(v: integer) return T_INT58;

    -- converts an integer to INT59
    pure function INT59(v: integer) return T_INT59;

    -- converts an integer to INT60
    pure function INT60(v: integer) return T_INT60;

    -- converts an integer to INT61
    pure function INT61(v: integer) return T_INT61;

    -- converts an integer to INT62
    pure function INT62(v: integer) return T_INT62;

    -- converts an integer to INT63
    pure function INT63(v: integer) return T_INT63;


    -- converts an integer to SYSTEM_UINT8
    pure function SYSTEM_UINT8(v: integer) return T_SYSTEM_UINT8;
    -- converts an integer to SYSTEM_UINT16
    pure function SYSTEM_UINT16(v: integer) return T_SYSTEM_UINT16;
    -- converts an integer to SYSTEM_UINT32
    pure function SYSTEM_UINT32(v: integer) return T_SYSTEM_UINT32;
    -- converts an integer to SYSTEM_UINT64
    pure function SYSTEM_UINT64(v: integer) return T_SYSTEM_UINT64;

    -- converts an integer to SYSTEM_INT8
    pure function SYSTEM_INT8(v: integer) return T_SYSTEM_INT8;
    -- converts an integer to SYSTEM_INT16
    pure function SYSTEM_INT16(v: integer) return T_SYSTEM_INT16;
    -- converts an integer to SYSTEM_INT32
    pure function SYSTEM_INT32(v: integer) return T_SYSTEM_INT32;
    -- converts an integer to SYSTEM_INT64
    pure function SYSTEM_INT64(v: integer) return T_SYSTEM_INT64;


end SYSTEM_TYPES;


package body SYSTEM_TYPES is

    -- converts an integer to UINT1
    pure function UINT1(v: integer) return T_UINT1 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT1'length));
    end UINT1;
    -- converts an integer to UINT2
    pure function UINT2(v: integer) return T_UINT2 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT2'length));
    end UINT2;
    -- converts an integer to UINT3
    pure function UINT3(v: integer) return T_UINT3 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT3'length));
    end UINT3;
    -- converts an integer to UINT4
    pure function UINT4(v: integer) return T_UINT4 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT4'length));
    end UINT4;
    -- converts an integer to UINT5
    pure function UINT5(v: integer) return T_UINT5 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT5'length));
    end UINT5;
    -- converts an integer to UINT6
    pure function UINT6(v: integer) return T_UINT6 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT6'length));
    end UINT6;
    -- converts an integer to UINT7
    pure function UINT7(v: integer) return T_UINT7 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT7'length));
    end UINT7;
    -- converts an integer to UINT9
    pure function UINT9(v: integer) return T_UINT9 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT9'length));
    end UINT9;
    -- converts an integer to UINT10
    pure function UINT10(v: integer) return T_UINT10 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT10'length));
    end UINT10;
    -- converts an integer to UINT11
    pure function UINT11(v: integer) return T_UINT11 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT11'length));
    end UINT11;
    -- converts an integer to UINT12
    pure function UINT12(v: integer) return T_UINT12 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT12'length));
    end UINT12;
    -- converts an integer to UINT13
    pure function UINT13(v: integer) return T_UINT13 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT13'length));
    end UINT13;
    -- converts an integer to UINT14
    pure function UINT14(v: integer) return T_UINT14 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT14'length));
    end UINT14;
    -- converts an integer to UINT15
    pure function UINT15(v: integer) return T_UINT15 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT15'length));
    end UINT15;
    -- converts an integer to UINT17
    pure function UINT17(v: integer) return T_UINT17 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT17'length));
    end UINT17;
    -- converts an integer to UINT18
    pure function UINT18(v: integer) return T_UINT18 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT18'length));
    end UINT18;
    -- converts an integer to UINT19
    pure function UINT19(v: integer) return T_UINT19 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT19'length));
    end UINT19;
    -- converts an integer to UINT20
    pure function UINT20(v: integer) return T_UINT20 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT20'length));
    end UINT20;
    -- converts an integer to UINT21
    pure function UINT21(v: integer) return T_UINT21 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT21'length));
    end UINT21;
    -- converts an integer to UINT22
    pure function UINT22(v: integer) return T_UINT22 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT22'length));
    end UINT22;
    -- converts an integer to UINT23
    pure function UINT23(v: integer) return T_UINT23 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT23'length));
    end UINT23;
    -- converts an integer to UINT24
    pure function UINT24(v: integer) return T_UINT24 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT24'length));
    end UINT24;
    -- converts an integer to UINT25
    pure function UINT25(v: integer) return T_UINT25 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT25'length));
    end UINT25;
    -- converts an integer to UINT26
    pure function UINT26(v: integer) return T_UINT26 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT26'length));
    end UINT26;
    -- converts an integer to UINT27
    pure function UINT27(v: integer) return T_UINT27 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT27'length));
    end UINT27;
    -- converts an integer to UINT28
    pure function UINT28(v: integer) return T_UINT28 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT28'length));
    end UINT28;
    -- converts an integer to UINT29
    pure function UINT29(v: integer) return T_UINT29 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT29'length));
    end UINT29;
    -- converts an integer to UINT30
    pure function UINT30(v: integer) return T_UINT30 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT30'length));
    end UINT30;
    -- converts an integer to UINT31
    pure function UINT31(v: integer) return T_UINT31 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT31'length));
    end UINT31;
    -- converts an integer to UINT33
    pure function UINT33(v: integer) return T_UINT33 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT33'length));
    end UINT33;
    -- converts an integer to UINT34
    pure function UINT34(v: integer) return T_UINT34 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT34'length));
    end UINT34;
    -- converts an integer to UINT35
    pure function UINT35(v: integer) return T_UINT35 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT35'length));
    end UINT35;
    -- converts an integer to UINT36
    pure function UINT36(v: integer) return T_UINT36 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT36'length));
    end UINT36;
    -- converts an integer to UINT37
    pure function UINT37(v: integer) return T_UINT37 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT37'length));
    end UINT37;
    -- converts an integer to UINT38
    pure function UINT38(v: integer) return T_UINT38 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT38'length));
    end UINT38;
    -- converts an integer to UINT39
    pure function UINT39(v: integer) return T_UINT39 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT39'length));
    end UINT39;
    -- converts an integer to UINT40
    pure function UINT40(v: integer) return T_UINT40 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT40'length));
    end UINT40;
    -- converts an integer to UINT41
    pure function UINT41(v: integer) return T_UINT41 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT41'length));
    end UINT41;
    -- converts an integer to UINT42
    pure function UINT42(v: integer) return T_UINT42 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT42'length));
    end UINT42;
    -- converts an integer to UINT43
    pure function UINT43(v: integer) return T_UINT43 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT43'length));
    end UINT43;
    -- converts an integer to UINT44
    pure function UINT44(v: integer) return T_UINT44 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT44'length));
    end UINT44;
    -- converts an integer to UINT45
    pure function UINT45(v: integer) return T_UINT45 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT45'length));
    end UINT45;
    -- converts an integer to UINT46
    pure function UINT46(v: integer) return T_UINT46 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT46'length));
    end UINT46;
    -- converts an integer to UINT47
    pure function UINT47(v: integer) return T_UINT47 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT47'length));
    end UINT47;
    -- converts an integer to UINT48
    pure function UINT48(v: integer) return T_UINT48 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT48'length));
    end UINT48;
    -- converts an integer to UINT49
    pure function UINT49(v: integer) return T_UINT49 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT49'length));
    end UINT49;
    -- converts an integer to UINT50
    pure function UINT50(v: integer) return T_UINT50 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT50'length));
    end UINT50;
    -- converts an integer to UINT51
    pure function UINT51(v: integer) return T_UINT51 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT51'length));
    end UINT51;
    -- converts an integer to UINT52
    pure function UINT52(v: integer) return T_UINT52 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT52'length));
    end UINT52;
    -- converts an integer to UINT53
    pure function UINT53(v: integer) return T_UINT53 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT53'length));
    end UINT53;
    -- converts an integer to UINT54
    pure function UINT54(v: integer) return T_UINT54 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT54'length));
    end UINT54;
    -- converts an integer to UINT55
    pure function UINT55(v: integer) return T_UINT55 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT55'length));
    end UINT55;
    -- converts an integer to UINT56
    pure function UINT56(v: integer) return T_UINT56 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT56'length));
    end UINT56;
    -- converts an integer to UINT57
    pure function UINT57(v: integer) return T_UINT57 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT57'length));
    end UINT57;
    -- converts an integer to UINT58
    pure function UINT58(v: integer) return T_UINT58 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT58'length));
    end UINT58;
    -- converts an integer to UINT59
    pure function UINT59(v: integer) return T_UINT59 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT59'length));
    end UINT59;
    -- converts an integer to UINT60
    pure function UINT60(v: integer) return T_UINT60 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT60'length));
    end UINT60;
    -- converts an integer to UINT61
    pure function UINT61(v: integer) return T_UINT61 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT61'length));
    end UINT61;
    -- converts an integer to UINT62
    pure function UINT62(v: integer) return T_UINT62 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT62'length));
    end UINT62;
    -- converts an integer to UINT63
    pure function UINT63(v: integer) return T_UINT63 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_UINT63'length));
    end UINT63;

    -- converts an integer to INT1
    pure function INT1(v: integer) return T_INT1 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT1'length));
    end INT1;
    -- converts an integer to INT2
    pure function INT2(v: integer) return T_INT2 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT2'length));
    end INT2;
    -- converts an integer to INT3
    pure function INT3(v: integer) return T_INT3 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT3'length));
    end INT3;
    -- converts an integer to INT4
    pure function INT4(v: integer) return T_INT4 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT4'length));
    end INT4;
    -- converts an integer to INT5
    pure function INT5(v: integer) return T_INT5 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT5'length));
    end INT5;
    -- converts an integer to INT6
    pure function INT6(v: integer) return T_INT6 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT6'length));
    end INT6;
    -- converts an integer to INT7
    pure function INT7(v: integer) return T_INT7 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT7'length));
    end INT7;
    -- converts an integer to INT9
    pure function INT9(v: integer) return T_INT9 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT9'length));
    end INT9;
    -- converts an integer to INT10
    pure function INT10(v: integer) return T_INT10 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT10'length));
    end INT10;
    -- converts an integer to INT11
    pure function INT11(v: integer) return T_INT11 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT11'length));
    end INT11;
    -- converts an integer to INT12
    pure function INT12(v: integer) return T_INT12 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT12'length));
    end INT12;
    -- converts an integer to INT13
    pure function INT13(v: integer) return T_INT13 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT13'length));
    end INT13;
    -- converts an integer to INT14
    pure function INT14(v: integer) return T_INT14 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT14'length));
    end INT14;
    -- converts an integer to INT15
    pure function INT15(v: integer) return T_INT15 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT15'length));
    end INT15;
    -- converts an integer to INT17
    pure function INT17(v: integer) return T_INT17 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT17'length));
    end INT17;
    -- converts an integer to INT18
    pure function INT18(v: integer) return T_INT18 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT18'length));
    end INT18;
    -- converts an integer to INT19
    pure function INT19(v: integer) return T_INT19 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT19'length));
    end INT19;
    -- converts an integer to INT20
    pure function INT20(v: integer) return T_INT20 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT20'length));
    end INT20;
    -- converts an integer to INT21
    pure function INT21(v: integer) return T_INT21 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT21'length));
    end INT21;
    -- converts an integer to INT22
    pure function INT22(v: integer) return T_INT22 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT22'length));
    end INT22;
    -- converts an integer to INT23
    pure function INT23(v: integer) return T_INT23 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT23'length));
    end INT23;
    -- converts an integer to INT24
    pure function INT24(v: integer) return T_INT24 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT24'length));
    end INT24;
    -- converts an integer to INT25
    pure function INT25(v: integer) return T_INT25 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT25'length));
    end INT25;
    -- converts an integer to INT26
    pure function INT26(v: integer) return T_INT26 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT26'length));
    end INT26;
    -- converts an integer to INT27
    pure function INT27(v: integer) return T_INT27 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT27'length));
    end INT27;
    -- converts an integer to INT28
    pure function INT28(v: integer) return T_INT28 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT28'length));
    end INT28;
    -- converts an integer to INT29
    pure function INT29(v: integer) return T_INT29 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT29'length));
    end INT29;
    -- converts an integer to INT30
    pure function INT30(v: integer) return T_INT30 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT30'length));
    end INT30;
    -- converts an integer to INT31
    pure function INT31(v: integer) return T_INT31 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT31'length));
    end INT31;
    -- converts an integer to INT33
    pure function INT33(v: integer) return T_INT33 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT33'length));
    end INT33;
    -- converts an integer to INT34
    pure function INT34(v: integer) return T_INT34 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT34'length));
    end INT34;
    -- converts an integer to INT35
    pure function INT35(v: integer) return T_INT35 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT35'length));
    end INT35;
    -- converts an integer to INT36
    pure function INT36(v: integer) return T_INT36 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT36'length));
    end INT36;
    -- converts an integer to INT37
    pure function INT37(v: integer) return T_INT37 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT37'length));
    end INT37;
    -- converts an integer to INT38
    pure function INT38(v: integer) return T_INT38 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT38'length));
    end INT38;
    -- converts an integer to INT39
    pure function INT39(v: integer) return T_INT39 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT39'length));
    end INT39;
    -- converts an integer to INT40
    pure function INT40(v: integer) return T_INT40 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT40'length));
    end INT40;
    -- converts an integer to INT41
    pure function INT41(v: integer) return T_INT41 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT41'length));
    end INT41;
    -- converts an integer to INT42
    pure function INT42(v: integer) return T_INT42 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT42'length));
    end INT42;
    -- converts an integer to INT43
    pure function INT43(v: integer) return T_INT43 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT43'length));
    end INT43;
    -- converts an integer to INT44
    pure function INT44(v: integer) return T_INT44 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT44'length));
    end INT44;
    -- converts an integer to INT45
    pure function INT45(v: integer) return T_INT45 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT45'length));
    end INT45;
    -- converts an integer to INT46
    pure function INT46(v: integer) return T_INT46 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT46'length));
    end INT46;
    -- converts an integer to INT47
    pure function INT47(v: integer) return T_INT47 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT47'length));
    end INT47;
    -- converts an integer to INT48
    pure function INT48(v: integer) return T_INT48 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT48'length));
    end INT48;
    -- converts an integer to INT49
    pure function INT49(v: integer) return T_INT49 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT49'length));
    end INT49;
    -- converts an integer to INT50
    pure function INT50(v: integer) return T_INT50 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT50'length));
    end INT50;
    -- converts an integer to INT51
    pure function INT51(v: integer) return T_INT51 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT51'length));
    end INT51;
    -- converts an integer to INT52
    pure function INT52(v: integer) return T_INT52 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT52'length));
    end INT52;
    -- converts an integer to INT53
    pure function INT53(v: integer) return T_INT53 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT53'length));
    end INT53;
    -- converts an integer to INT54
    pure function INT54(v: integer) return T_INT54 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT54'length));
    end INT54;
    -- converts an integer to INT55
    pure function INT55(v: integer) return T_INT55 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT55'length));
    end INT55;
    -- converts an integer to INT56
    pure function INT56(v: integer) return T_INT56 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT56'length));
    end INT56;
    -- converts an integer to INT57
    pure function INT57(v: integer) return T_INT57 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT57'length));
    end INT57;
    -- converts an integer to INT58
    pure function INT58(v: integer) return T_INT58 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT58'length));
    end INT58;
    -- converts an integer to INT59
    pure function INT59(v: integer) return T_INT59 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT59'length));
    end INT59;
    -- converts an integer to INT60
    pure function INT60(v: integer) return T_INT60 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT60'length));
    end INT60;
    -- converts an integer to INT61
    pure function INT61(v: integer) return T_INT61 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT61'length));
    end INT61;
    -- converts an integer to INT62
    pure function INT62(v: integer) return T_INT62 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT62'length));
    end INT62;
    -- converts an integer to INT63
    pure function INT63(v: integer) return T_INT63 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_INT63'length));
    end INT63;

    -- converts an integer to SYSTEM_UINT8
    pure function SYSTEM_UINT8(v: integer) return T_SYSTEM_UINT8 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_SYSTEM_UINT8'length));
    end SYSTEM_UINT8;

    -- converts an integer to SYSTEM_UINT16
    pure function SYSTEM_UINT16(v: integer) return T_SYSTEM_UINT16 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_SYSTEM_UINT16'length));
    end SYSTEM_UINT16;

    -- converts an integer to SYSTEM_UINT32
    pure function SYSTEM_UINT32(v: integer) return T_SYSTEM_UINT32 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_SYSTEM_UINT32'length));
    end SYSTEM_UINT32;

    -- converts an integer to SYSTEM_UINT64
    pure function SYSTEM_UINT64(v: integer) return T_SYSTEM_UINT64 is
    begin
        return STD_LOGIC_VECTOR(TO_UNSIGNED(v, T_SYSTEM_UINT64'length));
    end SYSTEM_UINT64;

    -- converts an integer to SYSTEM_INT8
    pure function SYSTEM_INT8(v: integer) return T_SYSTEM_INT8 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_SYSTEM_INT8'length));
    end SYSTEM_INT8;

    -- converts an integer to SYSTEM_INT16
    pure function SYSTEM_INT16(v: integer) return T_SYSTEM_INT16 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_SYSTEM_INT16'length));
    end SYSTEM_INT16;

    -- converts an integer to SYSTEM_INT32
    pure function SYSTEM_INT32(v: integer) return T_SYSTEM_INT32 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_SYSTEM_INT32'length));
    end SYSTEM_INT32;

    -- converts an integer to SYSTEM_INT64
    pure function SYSTEM_INT64(v: integer) return T_SYSTEM_INT64 is
    begin
        return STD_LOGIC_VECTOR(TO_SIGNED(v, T_SYSTEM_INT64'length));
    end SYSTEM_INT64;

end SYSTEM_TYPES;

