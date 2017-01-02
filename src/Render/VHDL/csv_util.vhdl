library IEEE;

use IEEE.STD_LOGIC_1164.ALL;
use STD.TEXTIO.all;
use IEEE.STD_LOGIC_TEXTIO.all;
use std.textio.all;

package csv_util is

    constant CSV_LINE_LENGTH_MAX: integer := 256;
    subtype CSV_LINE_T is string(1 to CSV_LINE_LENGTH_MAX);

    -- Read until EOL or comma
	procedure read_csv_field(ln: inout LINE; ret: out string);
	
	-- Compare variable length strings	
	function are_strings_equal (ln1: string; ln2: string) return boolean;

	-- Debug print text
	procedure print(text: string);

    -- converts string to STD_LOGIC
    function to_std_logic(b: string) return std_logic;

    -- converts string STD_LOGIC_VECTOR to string
    function to_std_logic_vector(b: string) return std_logic_vector;

    -- converts STD_LOGIC into a string
    function str(b: std_logic) return string;

    -- converts STD_LOGIC_VECTOR into a string
    function str(b: std_logic_vector) return string;

    -- Returns the first occurrence of a a given character
    function index_of_chr(ln: string; c: character) return integer;

    -- Returns the first occurrence of a null character
	function index_of_null(ln: string) return integer;

    -- Returns a substring, from start to finish
	function substr(ln: string; start: integer; finish: integer) return string;
	
	-- Trucates strings with embedded null characters
    function truncate(ln: string) return string;
    

end csv_util;

package body csv_util is

    procedure print(text: string) is
        variable msg: line;
    begin
        write(msg, text);
        writeline(output, msg);       
    end print;

	procedure read_csv_field(ln: inout LINE; ret: out string) is
	    variable return_string: CSV_LINE_T;
	    variable read_char: character;
	    variable read_ok: boolean := true;
	    variable index: integer := 1;
	begin
	    read(ln, read_char, read_ok);
	    while read_ok loop
	        if read_char = ',' then
	            ret := return_string;
	            return;
	        else
	            return_string(index) := read_char;
	            index := index + 1;
	        end if;
	        read(ln, read_char, read_ok);
	    end loop;
	    
	    ret := return_string;
	end;

	function index_of_chr(ln: string; c: character) return integer is
	begin
       for i in 1 to ln'length loop
           if ln(i) = c then
               return i;
           end if;
       end loop;

       return ln'length + 1;

	end;

	function index_of_null(ln: string) return integer is
	begin
	   return index_of_chr(ln, NUL);
	end;
	
	function substr(ln: string; start: integer; finish: integer) return string is
	begin
	    return ln(start to finish);
	end;
	
	function truncate(ln: string) return string is
	begin
	    return substr(ln, 1, index_of_null(ln) - 1);
	end;

	function are_strings_equal(ln1: string; ln2: string) return boolean is
	   variable lhs : string(1 to ln1'length) := ln1;
	   variable rhs : string(1 to ln2'length) := ln2;
	   variable maxlen : integer := ln1'length;
	begin
	   if lhs'length = rhs'length and lhs'length = 0 then
	       return true;
	   else
	       if ln2'length < maxlen then
	           maxlen := ln2'length;
	       end if;
	       
	       for i in 1 to maxlen loop
	           if lhs(i) /= rhs(i) then
	               return false;
	           end if;
	       end loop;

           if lhs'length > maxlen then
                if lhs(maxlen + 1) /= NUL then
                    return false;
                end if;
           end if;

           if rhs'length > maxlen then
                if rhs(maxlen + 1) /= NUL then
                    return false;
                end if;
           end if;
           
	       return true;
	   end if;
	end;

    -- converts string STD_LOGIC_VECTOR to string
   function to_std_logic_vector(b: string) return std_logic_vector is
       variable res : std_logic_vector(1 to b'length);
       variable v : string(1 to b'length) := b; 
   begin
        if v(1) /= '1' and v(1) /= '0' then
            res(1) := std_logic'value(v);
        else
            for i in 1 to b'length loop
                if v(i) = '0' then
                    res(i) := '0';
                elsif v(i) = '1' then
                    res(i) := '1';
                else
                    res(i) := '-';
                end if;
            end loop;            
        end if;
        
        return res;
    end to_std_logic_vector;
    
    -- converts string to STD_LOGIC
   function to_std_logic(b: string) return std_logic is
     variable s: std_logic;
     begin
          s := '-';
          case b(1) is
            when 'U' => s := 'U';
            when 'X' => s := 'X';
            when '0' => s := '0';
            when '1' => s := '1';
            when 'Z' => s := 'Z';
            when 'W' => s := 'W';
            when 'L' => s := 'L';
            when 'H' => s := 'H';
            when '-' => s := '-';
         end case;
         return s;
    end to_std_logic;
    
    -- converts STD_LOGIC into a string
   function str(b: std_logic) return string is
     variable s: string(1 to 1);
     begin
          case b is
            when 'U' => s(1):= 'U';
            when 'X' => s(1):= 'X';
            when '0' => s(1):= '0';
            when '1' => s(1):= '1';
            when 'Z' => s(1):= 'Z';
            when 'W' => s(1):= 'W';
            when 'L' => s(1):= 'L';
            when 'H' => s(1):= 'H';
            when '-' => s(1):= '-';
         end case;
         return s;
    end str;
 
    -- converts STD_LOGIC_VECTOR into a string
   function str(b: std_logic_vector) return string is
       variable res : string(1 to b'length);
       variable v : std_logic_vector(1 to b'length) := b; 
   begin
        if v(1) /= '1' and v(1) /= '0' then
            return  std_logic'image(v(1));
        else
            for i in 1 to b'length loop
                if v(i) = '0' then
                    res(i) := '0';
                elsif v(i) = '1' then
                    res(i) := '1';
                else
                    res(i) := '-';
                end if;
            end loop;
            
            return res;
        end if;
    end str;

 end package body csv_util;