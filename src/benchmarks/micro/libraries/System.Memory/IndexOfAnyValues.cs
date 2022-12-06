using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Memory
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
    public class IndexOfAnyValues
    {
        private static readonly Buffers.IndexOfAnyValues<byte> _asciiByteValues = Buffers.IndexOfAnyValues.Create("aeiouz"u8);
        private static readonly Buffers.IndexOfAnyValues<byte> _anyByteValues = Buffers.IndexOfAnyValues.Create("aeiouz\u00FF"u8);
        private static readonly Buffers.IndexOfAnyValues<char> _asciiCharValues = Buffers.IndexOfAnyValues.Create("aeiouz");
        private static readonly Buffers.IndexOfAnyValues<char> _probabilisticCharValues = Buffers.IndexOfAnyValues.Create("aeiou\u00FC");

        private byte[] _byteBuffer;
        private char[] _charBuffer;

        [Params(1, 7, 16, 32, 1000)]
        public int Length;

        [Params(false, true)]
        public bool HasMatch;

        [GlobalSetup]
        public void Setup()
        {
            _byteBuffer = new byte[Length];
            _charBuffer = new char[Length];

            if (HasMatch)
            {
                _byteBuffer[Length / 2] = (byte)'a';
                _charBuffer[Length / 2] = 'a';
            }
        }

        [Benchmark]
        public int IndexOfAny_Byte_Ascii() => _byteBuffer.AsSpan().IndexOfAny(_asciiByteValues);

        [Benchmark]
        public int IndexOfAny_Byte_AnyValue() => _byteBuffer.AsSpan().IndexOfAny(_anyByteValues);

        [Benchmark]
        public int IndexOfAny_Char_Ascii() => _charBuffer.AsSpan().IndexOfAny(_asciiCharValues);

        [Benchmark]
        public int IndexOfAny_Char_Probabilistic() => _charBuffer.AsSpan().IndexOfAny(_probabilisticCharValues);
    }
}
