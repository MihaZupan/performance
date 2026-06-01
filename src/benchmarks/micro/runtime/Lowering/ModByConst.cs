// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Lowering
{
    /// <summary>
    /// Exercises the JIT lowering of <c>uint % const</c> when the constant fits in [3, INT_MAX].
    /// On 64-bit targets this lowers to Daniel Lemire's "FastMod" sequence (two 64-bit multiplies)
    /// instead of the general magic-mul / shift / sub sequence.
    /// </summary>
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class ModByConst
    {
        private const int ArrayLength = 1024;

        private uint[] _values;
        private byte[] _bytes;
        private Adler32 _adler;

        [GlobalSetup]
        public void Setup()
        {
            _values = new uint[ArrayLength];
            _bytes = new byte[ArrayLength];
            var rng = new Random(42);
            var buf = new byte[4];
            for (int i = 0; i < _values.Length; i++)
            {
                rng.NextBytes(buf);
                _values[i] = BitConverter.ToUInt32(buf, 0);
            }
            rng.NextBytes(_bytes);
            _adler = new Adler32();
        }

        // Real-world consumer of `% 65521`: System.IO.Hashing.Adler32.
        [Benchmark]
        public void Adler32Append()
        {
            _adler.Append(_bytes);
            _adler.Reset();
        }

        // Small divisor (most common in date/time math: % 7, % 12, % 24, % 60).
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_7()        => Sum_7(_values);

        // Medium divisor.
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_100()      => Sum_100(_values);

        // The Adler32 divisor — largest prime below 2^16. The canonical FastMod use case.
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_65521()    => Sum_65521(_values);

        // Larger prime, near the middle of the FastMod range.
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_1000003()  => Sum_1000003(_values);

        // Upper edge of the FastMod range.
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_IntMax()   => Sum_IntMax(_values);

        // Control: divisor > INT_MAX still uses the existing magic-mod path.
        // Included to confirm there is no regression for the unchanged code path.
        [Benchmark(OperationsPerInvoke = ArrayLength)]
        public uint Mod_BigDivisor() => Sum_BigDivisor(_values);

        // Per-divisor [NoInlining] helpers so the divisor reaches the JIT as a compile-time constant.

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_7(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 7u;
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_100(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 100u;
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_65521(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 65521u;
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_1000003(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 1000003u;
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_IntMax(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 2147483647u; // INT_MAX
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint Sum_BigDivisor(uint[] v)
        {
            uint s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i] % 0x80000001u; // > INT_MAX
            return s;
        }
    }
}
