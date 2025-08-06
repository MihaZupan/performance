// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public class Gzip : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
    }

    public class Deflate : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
    }

#if NET6_0_OR_GREATER // API introduced in .NET 6
    public class ZLib : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZLibStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZLibStream(stream, level);
    }

    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class ZLibBench
    {
        private Barrier _barrier = new Barrier(Environment.ProcessorCount - 1);

        [Benchmark]
        public void Test()
        {
            Task.WaitAll(Enumerable
                .Range(0, _barrier.ParticipantCount)
                .Select(_ => Task.Run(() =>
                {
                    _barrier.SignalAndWait();
                    for (int length = 1; length < 1000; length++)
                    {

                        byte[] buffer = new byte[length];
                        Random.Shared.NextBytes(buffer);
                        using (var z = new ZLibStream(new MemoryStream(), CompressionMode.Compress))
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                z.Write(buffer);
                            }
                        }
                    }
                })).ToArray());
        }
    }
#endif

    // Brotli has a dedicated file with more benchmarks

    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public abstract class CompressionStreamPerfTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);

        [ParamsSource(nameof(UncompressedTestFileNames))]
        public string file { get; set; }

        [Params(CompressionLevel.Optimal, CompressionLevel.Fastest)] // we don't test the performance of CompressionLevel.NoCompression on purpose
        public CompressionLevel level { get; set; }

        protected CompressedFile CompressedFile;

        [GlobalSetup]
        public void Setup() => CompressedFile = new CompressedFile(file, level, CreateStream); // this logic is quite expensive, needs to be a part of Setup

        public IEnumerable<string> UncompressedTestFileNames()
        {
            yield return "TestDocument.pdf"; // 199 KB small test document with repeated paragraph, PDF are common
            yield return "alice29.txt"; // 145 KB, copy of "ALICE'S ADVENTURES IN WONDERLAND" book, an example of text file
            yield return "sum"; // 37.3 KB, some binary content, an example of binary file
        }

        [Benchmark]
        public void Compress()
        {
            CompressedFile.CompressedDataStream.Position = 0; // all benchmarks invocation reuse the same stream, we set Postion to 0 to start at the beginning

            var compressor = CreateStream(CompressedFile.CompressedDataStream, level);
            compressor.Write(CompressedFile.UncompressedData, 0, CompressedFile.UncompressedData.Length);
        }

        [Benchmark]
        public int Decompress()
        {
            CompressedFile.CompressedDataStream.Position = 0;

            var compressor = CreateStream(CompressedFile.CompressedDataStream, CompressionMode.Decompress);

            byte[] buffer = CompressedFile.UncompressedData;

            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = compressor.Read(buffer, totalRead, buffer.Length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }

            return totalRead;
        }
    }
}
