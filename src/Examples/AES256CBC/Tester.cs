using SME;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AES256CBC
{
	public class Tester : SimulationProcess
	{
        // The AES test reference vectors
        private static readonly byte[][] REFERENCE_VECTORS = new[] {
			StringToByteArray("6bc1bee22e409f96e93d7e117393172a"),
			StringToByteArray("ae2d8a571e03ac9c9eb76fac45af8e51"),
			StringToByteArray("30c81c46a35ce411e5fbc1191a0a52ef"),
			StringToByteArray("f69f2445df4f9b17ad2b417be66c3710"),
		};

        // Set to zero or less to use the reference vectors
        public static int NUMBER_OF_RUNS = -1;

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

		public static string ToHex(uint data)
		{
            string hex = BitConverter.ToString(BitConverter.GetBytes(data));
			return hex.Replace("-", "");
		}

		[OutputBus]
        public AES256CBC.AESCore.IInput Input = Scope.CreateBus<AES256CBC.AESCore.IInput>();

        [InputBus]
        public AES256CBC.AESCore.IOutput Output;

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

		/// <summary>
		/// Computes the resulting encrypted value from the sequence of input values
		/// </summary>
		/// <returns>The pairs.</returns>
		/// <param name="key">The encryption key</param>
		/// <param name="iv">The initialization vector</param>
		/// <param name="input">The inputs to encrypt.</param>
		private static IEnumerable<KeyValuePair<byte[], byte[]>> Pairs(byte[] key, byte[] iv, IEnumerable<byte[]> input)
        {
            var cr = System.Security.Cryptography.Aes.Create();

            foreach (var n in input)
            {
                byte[] res;
                using (var enc = cr.CreateEncryptor(key, iv))
                    res = enc.TransformFinalBlock(n, 0, n.Length);

                Array.Copy(res, iv, iv.Length);

                yield return new KeyValuePair<byte[], byte[]>(n, iv);
            }
        }

        private static IEnumerable<byte[]> RandomVectors(int count)
        {
            if (count <= 0)
            {
                foreach (var v in REFERENCE_VECTORS)
                    yield return v;
            }
            else
            {
                var rng = new Random();
                byte[] tmp = new byte[16];
                while (count-- > 0)
                {
                    rng.NextBytes(tmp);
                    yield return tmp;
                }
            }
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

            foreach (var testvector in Pairs(key, iv, RandomVectors(NUMBER_OF_RUNS)))
			{
				Input.DataReady = true;

				Input.Data0 = PackArrayToLong(testvector.Key, 0);
				Input.Data1 = PackArrayToLong(testvector.Key, 8);

                // Since both processes are clocked, we need to wait twice
				await ClockAsync();
                Input.DataReady = false;

                await ClockAsync();
				Debug.Assert(Output.DataReady, "Failed to produce data?");

				var d0 = PackArrayToLong(testvector.Value, 0);
				var d1 = PackArrayToLong(testvector.Value, 8);

				Debug.Assert(Output.Data0 == d0, "Failed to produce correct output");
				Debug.Assert(Output.Data1 == d1, "Failed to produce correct output");
			}

			Input.DataReady = false;

			await ClockAsync();

		}
	}
}

