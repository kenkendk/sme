using System;
using SME;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using SME.VHDL;

namespace AES256CBC
{
	[Ignore]
	//[ClockedProcess]
	public class Tester : Process
	{
		public static byte[] StringToByteArray(string hex) {
			return Enumerable.Range(0, hex.Length)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}


		public static string ByteArrayToString(byte[] ba)
		{
			string hex = BitConverter.ToString(ba);
			return hex.Replace("-","");
		}

		[OutputBus]
		private AES256CBC.AESCore.IInput Input;

		[InputBus]
		private AES256CBC.AESCore.IOutput Output;


		public static ulong PackArrayToLong(byte[] source, byte offset)
		{
			return
				(((ulong)source[offset + 0] << (64 - 8)) & 0xff00000000000000)
				|
				(((ulong)source[offset + 1] << (64 - 16)) & 0x00ff000000000000)
				|
				(((ulong)source[offset + 2] << (64 - 24)) & 0x0000ff0000000000)
				|
				(((ulong)source[offset + 3] << (64 - 32)) & 0x000000ff00000000)
				|
				(((ulong)source[offset + 4] << (64 - 40)) & 0x00000000ff000000)
				|
				(((ulong)source[offset + 5] << (64 - 48)) & 0x0000000000ff0000)
				|
				(((ulong)source[offset + 6] << (64 - 56)) & 0x000000000000ff00)
				|
				(((ulong)source[offset + 7] << (64 - 64)) & 0x00000000000000ff)
				;
		}

		public override async Task Run()
		{
			var key = StringToByteArray("603deb1015ca71be2b73aef0857d77811f352c073b6108d72d9810a30914dff4");
			var iv = StringToByteArray("000102030405060708090A0B0C0D0E0F");

			await ClockAsync();

			Input.DataReady = false;
			Input.LoadKey = true;
			Input.Data0 = PackArrayToLong(iv, 0);
			Input.Data1 = PackArrayToLong(iv, 8);

			Input.Key0 = PackArrayToLong(key, 0);
			Input.Key1 = PackArrayToLong(key, 8);
			Input.Key2 = PackArrayToLong(key, 16);
			Input.Key3 = PackArrayToLong(key, 24);

			await ClockAsync();

			Input.LoadKey = false;


			var testvectors = new[] {
				new KeyValuePair<byte[], byte[]>(
					StringToByteArray("6bc1bee22e409f96e93d7e117393172a"),
					StringToByteArray("f58c4c04d6e5f1ba779eabfb5f7bfbd6")
				),
				new KeyValuePair<byte[], byte[]>(
					StringToByteArray("ae2d8a571e03ac9c9eb76fac45af8e51"),
					StringToByteArray("9cfc4e967edb808d679f777bc6702c7d")
				),
				new KeyValuePair<byte[], byte[]>(
					StringToByteArray("30c81c46a35ce411e5fbc1191a0a52ef"),
					StringToByteArray("39f23369a9d9bacfa530e26304231461")
				),
				new KeyValuePair<byte[], byte[]>(
					StringToByteArray("f69f2445df4f9b17ad2b417be66c3710"),
					StringToByteArray("b2eb05e2c39be9fcda6c19078c6a9d1b")
				)
			};

			foreach (var testvector in testvectors)
			{
				Input.DataReady = true;

				Input.Data0 = PackArrayToLong(testvector.Key, 0);
				Input.Data1 = PackArrayToLong(testvector.Key, 8);

				await ClockAsync();

				if (!Output.DataReady)
					throw new Exception("Failed to produce data?");

				var d0 = PackArrayToLong(testvector.Value, 0);
				var d1 = PackArrayToLong(testvector.Value, 8);

				if (Output.Data0 != d0 || Output.Data1 != d1)
					throw new Exception("Failed to produce correct output");
			}

			Input.DataReady = false;

			await ClockAsync();

		}
	}
}

