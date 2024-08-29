// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Runtime.CompilerServices;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class SearchValuesCharTests
    {
        [Params(32, 64, 128, 256, 1_000, 10_000)]
        public int Length;

        private SearchValues<char> _searchValues;
        private char[] _text;

        [Params(
            //"abcdefABCDEF0123456789",   // ASCII
            "abcdefABCDEF0123456789Ü"  // Mixed ASCII and non-ASCII
            //"ßäöüÄÖÜ"                   // Non-ASCII only
            )]
        public string Values;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private char CharNotInSet() => '\n';

        [GlobalSetup]
        public void Setup()
        {
            _searchValues = SearchValues.Create(Values);

            _text = new string(CharNotInSet(), Length).ToCharArray();
            _text[0] = char.MaxValue;
        }

        [Benchmark]
        public int IndexOfAny() => _text.AsSpan().IndexOfAny(_searchValues);
    }
}
