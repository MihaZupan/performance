// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections;

[BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
public class StringDictionaryHashing
{
    private const int BatchSize = 256;
    private Dictionary<string, int> _dictionary = null!;
    private Dictionary<string, int> _missingDictionary = null!;
    private string[] _found = null!;
    private string[] _missing = null!;

    [Params(0, 1, 2, 3, 4, 8, 9, 10, 18, 32, 47, 48, 121, 1000, -1)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        _dictionary = new(BatchSize);
        _found = new string[BatchSize];
        _missing = new string[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            int length = Length < 0 ? random.Next(1, 65) : Length;
            char[] chars = new char[length];
            for (int j = 0; j < length; j++)
            {
                chars[j] = (char)random.Next('a', 'z' + 1);
            }

            if (length != 0)
            {
                chars[length - 1] = (char)(0x100 + i);
            }

            _dictionary[new string(chars)] = i;
            _found[i] = new string(chars);
            if (length != 0)
            {
                chars[length - 1] = (char)(0x400 + i);
            }
            _missing[i] = new string(chars);
        }

        _missingDictionary = Length == 0 ? new() { ["nonempty"] = 0 } : _dictionary;
        random.Shuffle(_found);
        random.Shuffle(_missing);
    }

    // Vary keys within an invocation so mixed lengths exercise branch prediction.
    [Benchmark(OperationsPerInvoke = BatchSize * 2)]
    public int LookupAndUpdate()
    {
        int result = 0;
        foreach (string key in _found)
        {
            int value = _dictionary[key];
            _dictionary[key] = value;
            result += value;
        }
        return result;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int Missing()
    {
        int result = 0;
        foreach (string key in _missing)
        {
            result += _missingDictionary.ContainsKey(key) ? 1 : 0;
        }
        return result;
    }
}

[BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
public class StringDictionaryHashingCorpus
{
    private string[] _queries = null!;

    [Params(2, 3)]
    public int Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string text = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "libraries", "Common", "StringDictionaryHashing.txt"));
        _queries = Enumerable.Range(0, text.Length - Width + 1)
            .Select(i => text.Substring(i, Width)).ToArray();
        new Random(42).Shuffle(_queries);
    }

    [Benchmark]
    public Dictionary<string, int> Count()
    {
        Dictionary<string, int> dictionary = new();
        foreach (string query in _queries)
        {
            dictionary.TryGetValue(query, out int count);
            dictionary[query] = count + 1;
        }
        return dictionary;
    }
}
#endif
