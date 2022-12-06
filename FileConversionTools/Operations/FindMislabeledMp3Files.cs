namespace FileConversionTools.Operations;

[OperationDescription("[ffprobe] Find mislabeled mp3 files")]
public class FindMislabeledMp3Files : IOperation
{
    public const string ResultsFilePath = "missing-mp3-files.txt";
    public async Task RunAsync()
    {
        Console.WriteLine("ffprobe must be in PATH for this to work.");

        // Optionally wipe old data (from another session perhaps)
        if (File.Exists(ResultsFilePath) && UserInput.PoseBoolQuestion("Remove any old stored results?", true))
            File.Delete(ResultsFilePath);

        Console.WriteLine("Paste directory to check (includes subdirectories)");
        string baseDir = Console.ReadLine();
        while (!Directory.Exists(baseDir))
            Console.WriteLine("Invalid directory");

        // Find all files that think they're mp3s
        Console.WriteLine("Checking..");
        string[] mp3Files = Directory.GetFiles(baseDir, "*.mp3", SearchOption.AllDirectories);
        List<string> mislabeledFiles = new();

        // Acceptable bottleneck here. Optimize for larger dataset.
        foreach (string file in mp3Files)
            if (!await IsValidMp3(file))
                mislabeledFiles.Add(file);

        // Store results
        await File.AppendAllLinesAsync(ResultsFilePath, mislabeledFiles);
        Console.WriteLine($"Done. {mislabeledFiles.Count} of {mp3Files.Length} files were mislabeled. Added to {ResultsFilePath}");

    }

    async Task<bool> IsValidMp3(string path)
    {
        // run ffprobe to get file info
        var ffprobeResult = await Cli.Wrap("ffprobe")
            .WithArguments($"-v quiet -show_entries format=format_name -of default=noprint_wrappers=1:nokey=1 \"{path}\"")
            .ExecuteBufferedAsync();

        // return only if mp3 file. This is not the extention, it's the file's actual format.
        return ffprobeResult.StandardOutput.TrimEnd() == "mp3";
    }
}
