using System.Collections.Concurrent;
using System.Threading;

namespace FileConversionTools.Operations;

[OperationDescription("[ffmpeg] Convert the mislabeled mp3 files")]
public class ConvertMislabeledMp3 : IOperation
{
    const string FailedConversionsPath = "failed-conversion-mislabeled-mp3.txt";
    public async Task RunAsync()
    {
        Console.WriteLine("ffmpeg must be in PATH for this to work.");
        Console.WriteLine("Backup is recommended since we'll be overwriting existing mislabeled mp3 files");
        Console.WriteLine();
        if (!File.Exists(FindMislabeledMp3Files.ResultsFilePath))
        {
            Console.WriteLine("Run FindMislabeledMp3Files first.");
            return;
        }

        // Load and validate entries
        Console.WriteLine("Loading files from " + FindMislabeledMp3Files.ResultsFilePath);
        bool missingConfirmNeeded = false;
        ConcurrentQueue<string> filesToConvert = new((await File.ReadAllLinesAsync(FindMislabeledMp3Files.ResultsFilePath))
            .Distinct()
            .Where(x =>
            {
                if (!string.IsNullOrEmpty(x) && File.Exists(x)) return true;
                Console.WriteLine($"File not found: {x}");
                return false;
            }));
        if (missingConfirmNeeded && UserInput.PoseBoolQuestion(
            "Didn't find all files. Would you like to convert the remaining?", defaultAnswer: false))
            return;

        // Prep temp dir
        string baseTempDir = Path.Combine(Path.GetTempPath(), "convert-mislabeled-mp3");
        if (Directory.Exists(baseTempDir))
            Directory.Delete(baseTempDir, true);

        // Multithread convert
        ConcurrentBag<string> failedConversions = new();
        Console.WriteLine("Starting conversion. This will take a while.");
        int maxThreadCount = 4;
        List<Task> threadPool = new();
        try
        {
            for (int i = 0; i < maxThreadCount; i++)
            {
                string threadTempDir = Path.Combine(baseTempDir, i.ToString());
                threadPool.Add(Mp3ConversionThread(filesToConvert, failedConversions, threadTempDir));
            }

            // Wait for completion and report done
            await Task.WhenAll(threadPool);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Died: " + ex.Message);
            throw;
        }
        finally
        {
            if (Directory.Exists(baseTempDir))
                Directory.Delete(baseTempDir, true);
        }

        File.WriteAllLines(FailedConversionsPath, failedConversions);
        Console.WriteLine($"Done! {failedConversions.Count} failed");
    }

    private async Task Mp3ConversionThread(
        ConcurrentQueue<string> filesToConvert,
        ConcurrentBag<string> failedConversions,
        string threadTempDir)
    {
        //ffmpeg -i oldfile.mp3 -acodec libmp3lame audio.mp3
        if (!Directory.Exists(threadTempDir))
            Directory.CreateDirectory(threadTempDir);
        int iter = 1;
        while (filesToConvert.TryDequeue(out string file))
        {
            string tempOutput = Path.Combine(threadTempDir, iter.ToString() + ".mp3");
            try
            {
                var result = await Cli.Wrap("ffmpeg")
                    .WithArguments($"-i \"{file}\" -acodec libmp3lame \"{tempOutput}\"")
                    .ExecuteBufferedAsync();

                // Report fail or overwrite original file
                // ffmpeg reports output as error, don't check it.
                if (File.Exists(tempOutput))
                    File.Copy(tempOutput, file, overwrite: true);
                else failedConversions.Add(file);
                iter++;
            }
            catch
            {
                failedConversions.Add(file);
            }
            finally
            {
                if (File.Exists(tempOutput))
                    File.Delete(tempOutput);
            }
        }
    }
}
