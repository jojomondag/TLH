using Google.Apis.Download;
using Google.Apis.Drive.v3;
using TLH.IntegrationServices;
public static class DriveService
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
    public static async Task UploadFileToGoogleDrive(string pathToFile, string fileName, string parentFolderId, bool overwrite)
    {
        await ExceptionHelper.TryCatchAsync(async () =>
        {
            // If overwrite is true, find and delete any existing file with the same name
            if (overwrite)
            {
                var requestList = GoogleApiService.DriveService.Files.List();
                requestList.Q = $"name='{fileName}' and '{parentFolderId}' in parents";
                var fileList = await requestList.ExecuteAsync();
                if (fileList.Files.Count > 0)
                {
                    foreach (var duplicateFile in fileList.Files)
                    {
                        var requestDelete = GoogleApiService.DriveService.Files.Delete(duplicateFile.Id);
                        await requestDelete.ExecuteAsync();
                    }
                }
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Parents = new List<string>
            {
                parentFolderId
            }
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = new System.IO.FileStream(pathToFile, System.IO.FileMode.Open))
            {
                request = GoogleApiService.DriveService.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
                request.Fields = "id";
                await request.UploadAsync();
            }

            var file = request.ResponseBody;
            await MessageHelper.SaveMessageAsync($"File ID: {file.Id}");

        }, async (ex) =>
        {
            await ExceptionHelper.HandleExceptionAsync(ex, "Failed to upload the file to Google Drive");
        });
    }
    public static async Task<string> CreateFolderInGoogleDrive(string folderName, string? parentFolderId = null)
    {
        // First, search for the folder by name and parent folder ID (if provided)
        var request = GoogleApiService.DriveService.Files.List();
        request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
        if (parentFolderId != null)
        {
            request.Q += $" and '{parentFolderId}' in parents";
        }
        var files = await request.ExecuteAsync();

        // If the folder already exists, return its ID
        if (files.Files.Any())
        {
            Console.WriteLine($"Folder '{folderName}' already exists.");
            return files.Files.First().Id;
        }

        // If the folder doesn't exist, create it
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = parentFolderId != null ? new List<string> { parentFolderId } : null
        };

        var createRequest = GoogleApiService.DriveService.Files.Create(fileMetadata);
        createRequest.Fields = "id";
        var folder = await createRequest.ExecuteAsync();

        Console.WriteLine($"Folder ID: {folder.Id}");
        return folder.Id;
    }
    public static async Task<bool> CheckIfFileExists(string fileName, string parentFolderId)
    {
        var request = GoogleApiService.DriveService.Files.List();
        request.Q = $"name='{fileName}' and '{parentFolderId}' in parents and trashed=false";
        request.Fields = "files(id, name)";

        var files = await request.ExecuteAsync();
        return files.Files.Any();
    }
}