// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    public class SearchValuesCharTests
    {
        [Params(32, 64, 128, 256, 1_000, 10_000)]
        public int Length;

        private static readonly SearchValues<char> s_searchValues = SearchValues.Create("ßäöüÄÖÜ");
        private char[] _text;

        [GlobalSetup]
        public void Setup()
        {
            _text = new string('\n', Length).ToCharArray();
        }

        [Benchmark]
        public int IndexOfAny() => _text.AsSpan().IndexOfAny(s_searchValues);
    }
}
