using Google.Apis.Classroom.v1.Data;
using Google.Apis.Download;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace TLH
    {
    public class DownloadService
    {
        public static async Task DownloadAllFilesFromClassroom(string courseId)
        {
            string courseDirectory = DirectoryManager.CreateCourseDirectory(courseId.Trim());

            var googleDriveFilesModifiedTime = await ClassroomApiHelper.GetAllGoogleDriveFilesModifiedTime(courseId);
            var desktopFilesModifiedTime = ClassroomApiHelper.GetAllDesktopFilesModifiedTime(courseDirectory);

            // Update the type of the dictionary to match the expected parameter type
            var googleDriveFilesModifiedTimeNullable = googleDriveFilesModifiedTime.ToDictionary(kv => kv.Key, kv => (DateTime?)kv.Value);
            var desktopFilesModifiedTimeNullable = desktopFilesModifiedTime.ToDictionary(kv => kv.Key, kv => (DateTime?)kv.Value);

            var courseWorkList = await ClassroomApiHelper.ListCourseWork(courseId);
            foreach (var courseWork in courseWorkList)
            {
                await DownloadCourseWorkFiles(courseId, courseWork, courseDirectory, googleDriveFilesModifiedTimeNullable, desktopFilesModifiedTimeNullable);
            }
        }
        public static async Task DownloadCourseWorkFiles(string courseId, CourseWork courseWork, string courseDirectory, Dictionary<string, DateTime?> googleDriveFilesModifiedTime, Dictionary<string, DateTime?> desktopFilesModifiedTime)
        {
            var studentSubmissions = await ClassroomApiHelper.ListStudentSubmissions(courseId, courseWork.Id);
            var downloadOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 }; // Change this number to limit the number of parallel downloads

            foreach (var submission in studentSubmissions)
            {
                var student = await ClassroomApiHelper.GetStudent(courseId, submission.UserId);
                var studentDirectory = Path.Combine(courseDirectory, DirectoryManager.SanitizeFolderName(student.Profile.Name.FullName));
                Directory.CreateDirectory(studentDirectory);

                if (submission.AssignmentSubmission?.Attachments != null && submission.AssignmentSubmission.Attachments.Count > 0)
                {
                    var assignmentDirectory = Path.Combine(studentDirectory, DirectoryManager.SanitizeFolderName(courseWork.Title));
                    Directory.CreateDirectory(assignmentDirectory);

                    await DownloadAttachmentsForSubmission(submission, assignmentDirectory, student, googleDriveFilesModifiedTime, desktopFilesModifiedTime);
                }
            }
        }
        public static async Task DownloadAttachmentsForSubmission(StudentSubmission submission, string destinationDirectory, Student student, Dictionary<string, DateTime?> googleDriveFilesModifiedTime, Dictionary<string, DateTime?> desktopFilesModifiedTime)
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

            var tasks = submission.AssignmentSubmission.Attachments.Select(async attachment =>
            {
                if (attachment?.DriveFile?.Id != null)
                {
                    // Handle Google Drive file attachments
                    var fileId = attachment.DriveFile.Id;
                    var fileName = attachment.DriveFile.Title;

                    // Get the file object from Google Drive
                    var file = await GetFileFromGoogleDrive(fileId);

                    // Get the modified time of the Google Classroom file and the local file
                    var googleDriveFileModifiedTime = await ClassroomApiHelper.GetFileModifiedTimeFromGoogleDrive(fileId);
                    
                    var desktopFileModifiedTime = File.GetLastWriteTimeUtc(Path.Combine(destinationDirectory, fileName));

                    var googleDriveFile = await GetFileFromGoogleDrive(fileId);

                    if (googleDriveFile == null)
                    {
                        Console.WriteLine($"Error: File with ID {fileId} not found.");
                    }
                    else
                    {
                        await DownloadFileFromGoogleDrive(fileId, fileName, destinationDirectory, googleDriveFile, googleDriveFileModifiedTime, desktopFileModifiedTime);
                    }
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
            });

            await Task.WhenAll(tasks);
        }
        public static async Task DownloadFileFromGoogleDrive(string fileId, string fileName, string destinationDirectory, GoogleDriveFile file, DateTime? googleDriveFileModifiedTime, DateTime? desktopFileModifiedTime)
        {
            if (file == null)
            {
                Console.WriteLine($"Error: File with ID {fileId} not found.");
                return;
            }

            string mimeType = file.MimeType;

            var (exportMimeType, fileExtension) = GetExportMimeTypeAndFileExtension(mimeType, fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName) + fileExtension;

            var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
            var filePath = Path.Combine(destinationDirectory, sanitizedFileName);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Compare the Google Classroom file modified time with the local file modified time
                if (googleDriveFileModifiedTime.HasValue && desktopFileModifiedTime.HasValue && googleDriveFileModifiedTime.Value <= desktopFileModifiedTime.Value)
                {
                    Console.WriteLine($"File {fileName} already exists and is up to date. Skipping download.");
                    return;
                }
            }

            // Download the file using HttpClient
            string downloadUrl = "";
            if (!string.IsNullOrEmpty(exportMimeType))
            {
                downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}/export?mimeType={Uri.EscapeDataString(exportMimeType)}";
            }
            else
            {
                downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GoogleApiHelper.Credential?.Token.AccessToken ?? "");

                using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    // Save the downloaded file to the destination directory
                    Console.WriteLine($"Saving file to: {filePath}");

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fileStream);
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

            switch (mimeType)
            {
                case "application/vnd.google-apps.document":
                    exportMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileExtension = ".docx";
                    break;
                case "application/vnd.google-apps.spreadsheet":
                    exportMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileExtension = ".xlsx";
                    break;
                case "application/vnd.google-apps.presentation":
                    exportMimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    fileExtension = ".pptx";
                    break;
                default:
                    fileExtension = Path.GetExtension(fileName);
                    break;
            }

            return (exportMimeType, fileExtension);
        }
        private static void HandleDownloadProgress(IDownloadProgress progress, string fileName, bool isSkipped)
        {
            if (progress.Status == DownloadStatus.Failed)
            {
                Console.WriteLine($"Download failed for {fileName}: {progress.Exception?.Message}");
            }
            else if (isSkipped)
            {
                Console.WriteLine($"File {fileName} already exists and is up to date. Skipping download.");
            }
        }
        public static async Task<GoogleDriveFile?> GetFileFromGoogleDrive(string fileId)
        {
            if (GoogleApiHelper.DriveService == null)
            {
                Console.WriteLine("Error: Google Drive service is not initialized.");
                return null;
            }

            try
            {
                var request = GoogleApiHelper.DriveService.Files.Get(fileId);
                request.Fields = "mimeType, modifiedTime, name"; // Add this line to specify the fields you want to retrieve
                var file = await request.ExecuteAsync();

                if (file == null)
                {
                    Console.WriteLine($"Error: File with ID {fileId} not found.");
                    return null;
                }

                return file;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file with ID {fileId}: {ex.Message}");
                return null;
            }
        }
    }
}