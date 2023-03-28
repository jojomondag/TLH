using Google.Apis.Download;
using Google.Apis.Drive.v3;
public static class DriveServiceExtensions
{
    public static void DownloadWithStatus(this FilesResource.GetRequest request, MemoryStream memoryStream)
    {
        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Console.WriteLine($"Downloading {request.FileId}: {progress.BytesDownloaded} bytes.");
                    break;
                case DownloadStatus.Completed:
                    Console.WriteLine($"Download complete: {request.FileId}");
                    memoryStream.Position = 0;
                    break;
                case DownloadStatus.Failed:
                    Console.WriteLine($"Download failed: {request.FileId}");
                    break;
            }
        };
        request.Download(memoryStream);
    }
    public static void DownloadWithStatus(this FilesResource.ExportRequest request, MemoryStream memoryStream)
    {
        request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Console.WriteLine($"Downloading {request.FileId}: {progress.BytesDownloaded} bytes.");
                    break;
                case DownloadStatus.Completed:
                    Console.WriteLine($"Download complete: {request.FileId}");
                    memoryStream.Position = 0;
                    break;
                case DownloadStatus.Failed:
                    Console.WriteLine($"Download failed: {request.FileId}");
                    break;
            }
        };
        request.Download(memoryStream);
    }
}