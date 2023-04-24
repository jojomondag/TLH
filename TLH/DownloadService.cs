using Google.Apis.Classroom.v1.Data;
using Google.Apis.Download;

namespace TLH
{
    public class DownloadService
    {
        public static async Task DownloadAllFilesFromClassroom(string courseId)
        {
            // We have to add checks so download is not happening if files are already downloaded.
            string courseDirectory = DirectoryManager.CreateCourseDirectory(courseId);

            var courseWorkList = await ClassroomApiHelper.ListCourseWork(courseId);
            foreach (var courseWork in courseWorkList)
            {
                await DownloadCourseWorkFiles(courseId, courseWork, courseDirectory);
            }
        }
        public static async Task DownloadCourseWorkFiles(string courseId, CourseWork courseWork, string courseDirectory)
        {
            var studentSubmissions = await ClassroomApiHelper.ListStudentSubmissions(courseId, courseWork.Id);
            foreach (var submission in studentSubmissions)
            {
                var student = await ClassroomApiHelper.GetStudent(courseId, submission.UserId);
                var studentDirectory = Path.Combine(courseDirectory, DirectoryManager.SanitizeFolderName(student.Profile.Name.FullName));
                Directory.CreateDirectory(studentDirectory);

                if (submission.AssignmentSubmission?.Attachments != null && submission.AssignmentSubmission.Attachments.Count > 0)
                {
                    var assignmentDirectory = Path.Combine(studentDirectory, DirectoryManager.SanitizeFolderName(courseWork.Title));
                    Directory.CreateDirectory(assignmentDirectory);

                    await DownloadAttachmentsForSubmission(submission, assignmentDirectory, student);
                }
            }
        }
        public static async Task DownloadAttachmentsForSubmission(StudentSubmission submission, string destinationDirectory, Student student)
        {
            if (submission.AssignmentSubmission?.Attachments == null || submission.AssignmentSubmission.Attachments.Count == 0)
            {
                return;
            }

            if (GoogleApiHelper.DriveService == null)
            {
                Console.WriteLine("Error: Google Drive service is not initialized.");
                return;
            }

            foreach (var attachment in submission.AssignmentSubmission.Attachments)
            {
                if (attachment?.DriveFile?.Id != null)
                {
                    // Handle Google Drive file attachments
                    var fileId = attachment.DriveFile.Id;
                    var fileName = attachment.DriveFile.Title;

                    // Get the mimeType of the file
                    var mimeType = await GetFileMimeTypeFromGoogleDrive(fileId);

                    await DownloadFileFromGoogleDrive(fileId, fileName, destinationDirectory, mimeType);
                }
                else if (attachment?.Link != null)
                {
                    // Handle link attachments
                    var link = attachment.Link.Url;
                    var linkFileName = "link_" + student.UserId + ".txt";
                    var linkFilePath = Path.Combine(destinationDirectory, linkFileName);

                    try
                    {
                        using (StreamWriter writer = new StreamWriter(linkFilePath, true))
                        {
                            await writer.WriteLineAsync(link);
                        }
                        Console.WriteLine($"Saved link for student {student.Profile.Name.FullName} to: {linkFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving link for student {student.Profile.Name.FullName}: {ex.Message}");
                    }
                }
            }
        }
        public static async Task DownloadFileFromGoogleDrive(string fileId, string fileName, string destinationDirectory, string? mimeType = null)
        {
            var (exportMimeType, fileExtension) = GetExportMimeTypeAndFileExtension(mimeType, fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName) + fileExtension;

            var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
            var filePath = Path.Combine(destinationDirectory, sanitizedFileName);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                var localFileModifiedTime = File.GetLastWriteTimeUtc(filePath);
                var googleDriveFileModifiedTime = await GetFileModifiedTimeFromGoogleDrive(fileId);

                Console.WriteLine($"Local file {fileName} modified time: {localFileModifiedTime}");
                // Add this line to print the Google Classroom file modified time in the correct format
                Console.WriteLine($"Google Classroom file {fileName} modified time: {googleDriveFileModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");

                // Check if the Google Classroom file is newer than the local file
                if (googleDriveFileModifiedTime.HasValue && googleDriveFileModifiedTime.Value <= localFileModifiedTime)
                {
                    Console.WriteLine($"File {fileName} already exists and is up to date. Skipping download.");
                    return;
                }
            }
            // Download the file
            var stream = new MemoryStream();

            if (!string.IsNullOrEmpty(exportMimeType))
            {
                var exportRequest = GoogleApiHelper.DriveService.Files.Export(fileId, exportMimeType);
                exportRequest.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    HandleDownloadProgress(progress, fileName);
                };
                await exportRequest.DownloadAsync(stream);
            }
            else
            {
                var getRequest = GoogleApiHelper.DriveService.Files.Get(fileId);
                getRequest.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    HandleDownloadProgress(progress, fileName);
                };
                await getRequest.DownloadAsync(stream);
            }

            // Reset the stream position to the beginning
            stream.Seek(0, SeekOrigin.Begin);

            // Save the downloaded file to the destination directory
            Console.WriteLine($"Saving file to: {filePath}");

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nCould not save the file: {fileName}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nCould not save the file: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nCould not save the file: {fileName}");
            }
        }
        public static async Task<DateTime?> GetFileModifiedTimeFromGoogleDrive(string fileId)
        {
            if (GoogleApiHelper.DriveService == null)
            {
                Console.WriteLine("Error: Google Drive service is not initialized.");
                return null;
            }

            try
            {
                var request = GoogleApiHelper.DriveService.Files.Get(fileId);
                request.Fields = "modifiedTime, name"; // Add this line to specify the fields you want to retrieve
                var file = await request.ExecuteAsync();

                if (file == null)
                {
                    Console.WriteLine($"Error: File with ID {fileId} not found.");
                    return null;
                }

                Console.WriteLine($"File {file.Name} modified time: {file.ModifiedTime?.ToUniversalTime()}");

                return file.ModifiedTime?.ToUniversalTime(); // Convert the modified time to UTC
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving modified time for file ID {fileId}: {ex.Message}");
                return null;
            }
        }
        public static async Task<string?> GetFileMimeTypeFromGoogleDrive(string fileId)
        {
            if (GoogleApiHelper.DriveService == null)
            {
                Console.WriteLine("Error: Google Drive service is not initialized.");
                return null;
            }

            try
            {
                var file = await GoogleApiHelper.DriveService.Files.Get(fileId).ExecuteAsync();
                return file.MimeType;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving mimeType for file ID {fileId}: {ex.Message}");
                return null;
            }
        }
        private static (string, string) GetExportMimeTypeAndFileExtension(string? mimeType, string fileName)
        {
            string exportMimeType = "";
            string fileExtension = "";

            if (mimeType == "application/vnd.google-apps.document")
            {
                exportMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                fileExtension = ".docx";
            }
            else if (mimeType == "application/vnd.google-apps.spreadsheet")
            {
                exportMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = ".xlsx";
            }
            else if (mimeType == "application/vnd.google-apps.presentation")
            {
                exportMimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                fileExtension = ".pptx";
            }
            else
            {
                fileExtension = Path.GetExtension(fileName);
            }

            return (exportMimeType, fileExtension);
        }
        private static void HandleDownloadProgress(IDownloadProgress progress, string fileName)
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    {
                        Console.WriteLine($"Downloading {fileName} ({progress.BytesDownloaded} bytes downloaded)");
                        break;
                    }
                case DownloadStatus.Completed:
                    {
                        Console.WriteLine($"Downloaded {fileName}");
                        break;
                    }
                case DownloadStatus.Failed:
                    {
                        Console.WriteLine($"Download failed for {fileName}: {progress.Exception?.Message}");
                        break;
                    }
            }
        }
    }
}