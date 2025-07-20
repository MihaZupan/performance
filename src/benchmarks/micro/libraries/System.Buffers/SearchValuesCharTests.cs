// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class SearchValuesCharTests
    {
        private const int InputLength = 256;

        private SearchValues<char> _searchValues;
        private char[] _text;
        private char[] _textExcept;

        [Params(
            "abcdefABCDEF0123456789",   // ASCII
            "abcdefABCDEF0123456789Ü",  // Mixed ASCII and non-ASCII
            "ßäöüÄÖÜ"                   // Non-ASCII only
            )]
        public string Values;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private char CharNotInSet() => '\n';

        [GlobalSetup]
        public void Setup()
        {
            _searchValues = SearchValues.Create(Values);

            char charInSet = Values[0];

            _text = new string(CharNotInSet(), InputLength).ToCharArray();
            _textExcept = new string(charInSet, InputLength).ToCharArray();

            _text[InputLength / 2] = charInSet;
            _textExcept[InputLength / 2] = CharNotInSet();
        }

        [Benchmark]
        public bool Contains() => _searchValues.Contains(CharNotInSet());

        [Benchmark]
        public bool ContainsAny() => _text.AsSpan().ContainsAny(_searchValues);

        [Benchmark]
        public int IndexOfAny() => _text.AsSpan().IndexOfAny(_searchValues);

        [Benchmark]
        [MemoryRandomization]
        public int IndexOfAnyExcept() => _textExcept.AsSpan().IndexOfAnyExcept(_searchValues);

        [Benchmark]
        public int LastIndexOfAny() => _text.AsSpan().LastIndexOfAny(_searchValues);

        [Benchmark]
        public int LastIndexOfAnyExcept() => _textExcept.AsSpan().LastIndexOfAnyExcept(_searchValues);
    }

#if NET9_0_OR_GREATER
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class SearchValuesStringTests_SingleString
    {
        private static readonly SearchValues<string> s_values = SearchValues.Create([Needle], StringComparison.Ordinal);
        private static readonly SearchValues<string> s_valuesIC = SearchValues.Create([Needle], StringComparison.OrdinalIgnoreCase);
        private static readonly string s_text_noMatches = new('a', Length);
        private static readonly string s_text_falsePositives = string.Concat(Enumerable.Repeat("Sherlock Holm_s", Length / Needle.Length));
        private static readonly string s_text_earlyMatch = $"{Needle}{new string('a', 100)}";

        public const int Length = 100_000;
        public const string Needle = "Sherlock Holmes";

        [Benchmark] public void Throughput() => s_text_noMatches.AsSpan().Contains(Needle, StringComparison.Ordinal);
        [Benchmark] public void SV_Throughput() => s_text_noMatches.AsSpan().ContainsAny(s_values);
        [Benchmark] public void SV_ThroughputIC() => s_text_noMatches.AsSpan().ContainsAny(s_valuesIC);

        [Benchmark] public void FalsePositives() => s_text_falsePositives.AsSpan().Contains(Needle, StringComparison.Ordinal);
        [Benchmark] public void SV_FalsePositives() => s_text_falsePositives.AsSpan().ContainsAny(s_values);
        [Benchmark] public void SV_FalsePositivesIC() => s_text_falsePositives.AsSpan().ContainsAny(s_valuesIC);

        [Benchmark] public void SV_EarlyMatch() => s_text_earlyMatch.AsSpan().ContainsAny(s_values);
        [Benchmark] public void SV_EarlyMatchIC() => s_text_earlyMatch.AsSpan().ContainsAny(s_valuesIC);
    }
#endif
}
