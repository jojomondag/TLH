using Google.Apis.Classroom.v1.Data;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;
using TLH.IntegrationServices;

namespace TLH.ClassroomApi
{
    public class FileDownloadService
    {
        private static bool ShouldDownloadFile(string fileId, string fileName, Dictionary<string, DateTime?> googleDriveFilesModifiedTime, Dictionary<string, DateTime?> desktopFilesModifiedTime)
        {
            var googleDriveFileModifiedTime = googleDriveFilesModifiedTime.GetValueOrDefault(fileId);
            var desktopFileModifiedTime = desktopFilesModifiedTime.GetValueOrDefault(fileName);

            var isDesktopFileUpToDate = desktopFileModifiedTime.HasValue && googleDriveFileModifiedTime.HasValue && desktopFileModifiedTime.Value >= googleDriveFileModifiedTime.Value;

            return !isDesktopFileUpToDate;
        }
        public static async Task DownloadAllFilesFromClassroom(string courseId)
        {
            await MessageHelper.SaveMessageAsync("Starting DownloadAllFilesFromClassroom...");
            string courseDirectory = await DirectoryUtil.CreateCourseDirectory(courseId.Trim());
            await MessageHelper.SaveMessageAsync("Course directory created...");

            // Start the tasks concurrently
            await MessageHelper.SaveMessageAsync("Starting tasks concurrently...");
            var googleDriveFilesModifiedTimeTask = ClassroomApiHelper.GetAllGoogleDriveFilesModifiedTime(courseId).AsTask();
            var desktopFilesModifiedTimeTask = Task.Run(() => ClassroomApiHelper.GetAllDesktopFilesModifiedTime(courseDirectory));
            var courseWorkListTask = ClassroomApiHelper.ListCourseWork(courseId).AsTask();

            // Wait for all tasks to complete
            await MessageHelper.SaveMessageAsync("Waiting for all tasks to complete...");
            await Task.WhenAll(googleDriveFilesModifiedTimeTask, desktopFilesModifiedTimeTask, courseWorkListTask).ConfigureAwait(false);

            // Extract the results
            await MessageHelper.SaveMessageAsync("Extracting the results...");
            var googleDriveFilesModifiedTime = googleDriveFilesModifiedTimeTask.Result;
            var desktopFilesModifiedTime = desktopFilesModifiedTimeTask.Result;
            var courseWorkList = courseWorkListTask.Result;

            var googleDriveFilesModifiedTimeNullable = googleDriveFilesModifiedTime.ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value);
            var desktopFilesModifiedTimeNullable = desktopFilesModifiedTime.ToDictionary(keyValue => keyValue.Key, keyValue => (DateTime?)keyValue.Value);

            if (courseWorkList.Count > 0)
            {
                await MessageHelper.SaveMessageAsync("Starting to download course work files...");
                List<Task> tasks = new List<Task>();
                foreach (var courseWork in courseWorkList)
                {
                    tasks.Add(DownloadCourseWorkFiles(courseId, courseWork, courseDirectory, googleDriveFilesModifiedTimeNullable, desktopFilesModifiedTimeNullable));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            await MessageHelper.SaveMessageAsync("DownloadAllFilesFromClassroom completed.");
        }
        public static Dictionary<string, Student> studentCache = new Dictionary<string, Student>();
        public static async Task DownloadCourseWorkFiles(string courseId, CourseWork courseWork, string courseDirectory, Dictionary<string, DateTime?> googleDriveFilesModifiedTime, Dictionary<string, DateTime?> desktopFilesModifiedTime)
        {
            var studentSubmissions = await ClassroomApiHelper.ListStudentSubmissions(courseId, courseWork.Id).ConfigureAwait(false);

            var submissionProcessingTasks = studentSubmissions.Select(async submission =>
            {
                Student student;
                if (!studentCache.ContainsKey(submission.UserId))
                {
                    student = await ClassroomApiHelper.GetStudent(courseId, submission.UserId).ConfigureAwait(false);
                    studentCache[submission.UserId] = student; // cache the student data
                }
                else
                {
                    student = studentCache[submission.UserId]; // get student data from the cache
                }

                var studentDirectory = Path.Combine(courseDirectory, DirectoryUtil.SanitizeFolderName(student.Profile.Name.FullName));
                Directory.CreateDirectory(studentDirectory);

                if (submission.AssignmentSubmission?.Attachments?.Count > 0)
                {
                    var assignmentDirectory = Path.Combine(studentDirectory, DirectoryUtil.SanitizeFolderName(courseWork.Title));
                    Directory.CreateDirectory(assignmentDirectory);

                    await DownloadAttachmentsForSubmission(submission, assignmentDirectory, student, googleDriveFilesModifiedTime, desktopFilesModifiedTime).ConfigureAwait(false);
                }
            });

            await Task.WhenAll(submissionProcessingTasks).ConfigureAwait(false);
        }
        public static async Task DownloadAttachmentsForSubmission(StudentSubmission submission, string destinationDirectory, Student student, Dictionary<string, DateTime?> googleDriveFilesModifiedTime, Dictionary<string, DateTime?> desktopFilesModifiedTime)
        {
            await ExceptionHelper.TryCatchAsync(async () =>
            {
                if (submission.AssignmentSubmission?.Attachments == null || submission.AssignmentSubmission.Attachments.Count == 0)
                {
                    return;
                }
                if (GoogleApiService.DriveService == null)
                {
                    await ExceptionHelper.TryCatchAsync(() => MessageHelper.SaveErrorAsync("Error: Google Drive service is not initialized."));
                    return;
                }

                var tasks = submission.AssignmentSubmission.Attachments.Select(attachment =>
                {
                    return Task.Run(async () =>
                    {
                        if (attachment?.DriveFile?.Id != null)
                        {
                            // Handle Google Drive file attachments
                            var fileId = attachment.DriveFile.Id;
                            var fileName = attachment.DriveFile.Title;

                            // Check if the file should be downloaded
                            if (ShouldDownloadFile(fileId, fileName, googleDriveFilesModifiedTime, desktopFilesModifiedTime))
                            {
                                var googleDriveFile = await GetFileFromGoogleDrive(fileId).ConfigureAwait(false);

                                if (googleDriveFile == null)
                                {
                                    await ExceptionHelper.TryCatchAsync(() => MessageHelper.SaveErrorAsync($"Error: File with ID {fileId} not found."));
                                }
                                else
                                {
                                    await DownloadFileFromGoogleDrive(fileId, fileName, destinationDirectory, googleDriveFile).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                await MessageHelper.SaveMessageAsync($"File {fileName} already exists and is up to date. Skipping download.");
                            }
                        }
                        else if (attachment?.Link != null)
                        {
                            // Handle link attachments
                            var link = attachment.Link.Url;
                            var linkFileName = "link_" + student.UserId + ".txt";
                            var linkFilePath = Path.Combine(destinationDirectory, linkFileName);

                            await ExceptionHelper.TryCatchAsync(async () =>
                            {
                                using (StreamWriter writer = new StreamWriter(linkFilePath, true))
                                {
                                    await writer.WriteLineAsync(link).ConfigureAwait(false);
                                }
                                await MessageHelper.SaveMessageAsync($"Saved link for student {student.Profile.Name.FullName} to: {linkFilePath}");
                            }, ex =>
                            {
                                ExceptionHelper.HandleException(ex, $"Error saving link for student {student.Profile.Name.FullName}");
                            });
                        }
                    });
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }, ex =>
            {
                MessageHelper.SaveError($"Error downloading attachments for submission: {ex.Message}");
            });
        }
        public static async Task DownloadFileFromGoogleDrive(string fileId, string fileName, string destinationDirectory, GoogleDriveFile file)
        {
            await ExceptionHelper.TryCatchAsync(async () =>
            {
                if (file == null)
                {
                    await MessageHelper.SaveErrorAsync($"Error: File with ID {fileId} not found.");
                    return;
                }

                string mimeType = file.MimeType;

                var (exportMimeType, fileExtension) = GetExportMimeTypeAndFileExtension(mimeType, fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName) + fileExtension;

                var sanitizedFileName = DirectoryUtil.SanitizeFolderName(fileName);
                var filePath = Path.Combine(destinationDirectory, sanitizedFileName);

                // Download the file using HttpClient
                string downloadUrl;
                if (!string.IsNullOrEmpty(exportMimeType))
                {
                    downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}/export?mimeType={Uri.EscapeDataString(exportMimeType)}";
                }
                else
                {
                    downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GoogleApiService.Credential?.Token.AccessToken ?? "");

                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Save the downloaded file to the destination directory
                await MessageHelper.SaveMessageAsync($"Saving file to: {filePath}");

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fileStream);
            }, ex => ExceptionHelper.HandleException(ex, $"Error downloading file {fileName}"));
        }
        public static async Task<string?> GetFileMimeTypeFromGoogleDrive(string fileId)
        {
            return await ExceptionHelper.TryCatchAsync(async () =>
            {
                if (GoogleApiService.DriveService == null)
                {
                    ExceptionHelper.HandleException(new Exception("Error: Google Drive service is not initialized."));
                    return null;
                }

                var file = await GoogleApiService.DriveService.Files.Get(fileId).ExecuteAsync();
                return file.MimeType;
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, $"Error retrieving mimeType for file ID {fileId}");
            });
        }
        private static (string, string) GetExportMimeTypeAndFileExtension(string? mimeType, string fileName)
        {
            string exportMimeType = "";
            string fileExtension;

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
        public static async Task<GoogleDriveFile?> GetFileFromGoogleDrive(string fileId)
        {
            return await ExceptionHelper.TryCatchAsync(async () =>
            {
                if (GoogleApiService.DriveService == null)
                {
                    ExceptionHelper.HandleException(new Exception("Error: Google Drive service is not initialized."));
                    return null;
                }

                var request = GoogleApiService.DriveService.Files.Get(fileId);
                request.Fields = "mimeType, modifiedTime, name";
                var file = await request.ExecuteAsync();

                if (file == null)
                {
                    ExceptionHelper.HandleException(new Exception($"Error: File with ID {fileId} not found."));
                    return null;
                }

                return file;
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, $"Error retrieving file with ID {fileId}");
            });
        }
    }
}