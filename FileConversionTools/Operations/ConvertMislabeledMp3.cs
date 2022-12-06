namespace FileConversionTools.Operations;

[OperationDescription("[ffmpeg] Convert the mislabeled mp3 files")]
public class ConvertMislabeledMp3 : IOperation
{
    public async Task RunAsync()
    {
        Console.WriteLine("ffmpeg must be in PATH for this to work.");
        //ffmpeg -i oldfile.mp3 -acodec libmp3lame audio.mp3
    }
}
