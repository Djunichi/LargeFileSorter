using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace Sorter;

/// <summary>
/// Main class
/// </summary>
internal class Program : IDisposable
{

    /// <summary>
    /// Size of reading buffer
    /// </summary>
    private readonly int BufferSize;
    
    /// <summary>
    /// Path to file
    /// </summary>
    private readonly string InputFile;
    
    /// <summary>
    /// Size of temp file
    /// </summary>
    private readonly int TempFileSize;
    
    /// <summary>
    /// Count of work threads depended on available cores
    /// </summary>
    private static readonly int WorkThreads = Environment.ProcessorCount;

    /// <summary>
    /// Reader queue
    /// </summary>
    private readonly Channel<List<(ReadOnlyMemory<char>, int)>> _reader;
    
    /// <summary>
    /// Writer queue
    /// </summary>
    private readonly Channel<List<(ReadOnlyMemory<char>, int)>> _writer;
    
    /// <summary>
    /// Records comparer
    /// </summary>
    private readonly Comparer _comparer;
    
    /// <summary>
    /// Temp files set
    /// </summary>
    private readonly List<string> _splitFiles;
    
    /// <summary>
    /// Timer to measure execution time
    /// </summary>
    private readonly Stopwatch _timer = new();


    /// <summary>
    /// Default constructor
    /// </summary>
    private Program()
    {
        var config = GetConfig();

        int.TryParse(config.GetSection("AppSettings:BufferSize").Value, out BufferSize);
        InputFile = config.GetSection("AppSettings:InputFile").Value;
        int.TryParse(config.GetSection("AppSettings:TempFileSize").Value, out TempFileSize);
        ;
        _comparer = new Comparer();

        _splitFiles = new();
        _reader = Channel.CreateBounded<List<(ReadOnlyMemory<char>, int)>>(1);
        _writer = Channel.CreateBounded<List<(ReadOnlyMemory<char>, int)>>(1);

    }
    
    /// <summary>
    /// Deletes created temp files
    /// </summary>
    public void Dispose()
    {
        _splitFiles.ForEach(File.Delete);
    }

    /// <summary>
    /// Split input file to temp files and sort records
    /// </summary>
    private async Task SplitAndSortInputFile()
    {
        _timer.Restart();

        SemaphoreSlim semaphore = new(1);

        var readerThreads = Enumerable.Range(0, WorkThreads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var chunk in _reader.Reader.ReadAllAsync())
                {
                    //Sort records before writing to temp file
                    chunk.Sort(_comparer);
                    await _writer.Writer.WriteAsync(chunk);
                }
            }))
            .ToArray();

        var writerThread = Task.Run(async () =>
        {
            await foreach (var chunk in _writer.Reader.ReadAllAsync())
            {
                await semaphore.WaitAsync();
                await WriteChunkToTempFile(chunk);
                semaphore.Release();
            }
        });

        using var fileReader = new StreamReader(InputFile, Encoding.UTF8, true, BufferSize);

        var chunkBuffer = new char[TempFileSize];
        var currentPosition = 0;
        while (true)
        {
            await semaphore.WaitAsync();
            int charsRead = await fileReader.ReadBlockAsync(chunkBuffer, currentPosition, TempFileSize - currentPosition);
            semaphore.Release();
            var endOfStream = fileReader.EndOfStream;
            var memory = chunkBuffer.AsMemory(0, currentPosition + charsRead);

            // Fill lines list to sort
            List<(ReadOnlyMemory<char>, int)> chunk = new();
            int lineIndex;
            while ((lineIndex = memory.Span.IndexOf(Environment.NewLine)) >= 0)
            {
                var line = memory[..lineIndex];
                chunk.Add((line, line.Span.IndexOf('.')));
                memory = memory[(lineIndex + Environment.NewLine.Length)..];
            }

            // If End of file add last line if not empty
            if (endOfStream && memory.Length > 0)
            {
                chunk.Add((memory, memory.Span.IndexOf('.')));
            }

            await _reader.Writer.WriteAsync(chunk);
            
            if (endOfStream) break;
            
            //Move the rest of buffer to new
            chunkBuffer = new char[TempFileSize];
            memory.CopyTo(chunkBuffer);
            currentPosition = memory.Length;
        }
        
        _reader.Writer.Complete();
        await Task.WhenAll(readerThreads);
        
        _writer.Writer.Complete();
        await writerThread;
        
        Console.WriteLine($"Splitting to temp files completed in {_timer.Elapsed}");
    }


    /// <summary>
    /// Merges all temp files to sorted result file
    /// </summary>
    private void MergeTempFilesToResult()
    {
        _timer.Restart();

        //Read all temp files and merge all enumerators
        var mergedLines = _splitFiles
            .Select(f => File.ReadLines(f).Select(s => (s.AsMemory(), s.IndexOf('.'))))
            .MergeEnumerators(_comparer);

        using var sortedFile = new StreamWriter(Path.ChangeExtension(InputFile, ".sorted" + Path.GetExtension(InputFile)), false, Encoding.UTF8, BufferSize);
        sortedFile.AutoFlush = false;
        foreach (var (line, _) in mergedLines)
        {
            sortedFile.WriteLine(line);
        }
        Console.WriteLine($"Merging temp files complete in {_timer.Elapsed}");
    }

    /// <summary>
    /// Writes part of input file to temp file
    /// </summary>
    /// <param name="chunk">Part of input file</param>
    private async Task WriteChunkToTempFile(List<(ReadOnlyMemory<char>, int)> chunk)
    {
        var tempFileName = Path.ChangeExtension(InputFile, $".part-{_splitFiles.Count}" + Path.GetExtension(InputFile));
        await using (var tempFile = new StreamWriter(tempFileName, false, Encoding.UTF8, BufferSize))
        {
            tempFile.AutoFlush = false;

            foreach (var (l, _) in chunk)
            {
                await tempFile.WriteLineAsync(l);
            }
            await tempFile.FlushAsync();
        }
        _splitFiles.Add(tempFileName);
    }

    /// <summary>
    /// Main method
    /// </summary>
    /// <returns></returns>
    private static async Task<int> Main()
    {

        using var app = new Program();
        await app.SplitAndSortInputFile();
        app.MergeTempFilesToResult();

        return 0;
    }

    private static IConfiguration GetConfig()
    {
        var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        return builder.Build();
    }
}