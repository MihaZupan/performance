using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Linq;

namespace System.Text
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Ascii
    {
        [Params(32, 64, 100, 128, 1_000, 10_000)]
        public int Size;

        private byte[] _bytes;
        private char[] _characters;

        [GlobalSetup]
        public void Setup()
        {
            _bytes = new byte[Size];

            for (int i = 0; i < Size; i++)
            {
                // let ToLower and ToUpper perform the same amount of work
                _bytes[i] = i % 2 == 0 ? (byte)'a' : (byte)'A';
            }
            _characters = _bytes.Select(b => (char)b).ToArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus FromUtf16() => Ascii.FromUtf16(_characters, _bytes, out _);
    }
}
