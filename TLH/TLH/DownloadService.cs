using Google.Apis.Classroom.v1.Data;
using Google.Apis.Download;

namespace TLH
{
    public class DownloadService
    {
        // TODO: Do check too see that update google classrooom files are not downloaded again. Maybe change metadate of file on desktop too be older then that in google classroom.
        //Public methods
        public static async Task DownloadAllFilesFromClassroom(string courseId)
        {
            //We have to add checks so download is not happening if files are already downloaded.
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
                // ...
                if (attachment?.DriveFile?.Id != null)
                {
                    var fileId = attachment.DriveFile.Id;
                    var fileName = attachment.DriveFile.Title;
                    Google.Apis.Drive.v3.Data.File? driveFile = null;

                    try
                    {
                        driveFile = GoogleApiHelper.DriveService.Files.Get(fileId).Execute();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching file information: {ex.Message}");
                        continue;
                    }

                    var mimeType = driveFile?.MimeType;
                    var (exportMimeType, fileExtension) = GetExportMimeTypeAndFileExtension(mimeType, fileName);

                    fileName = Path.GetFileNameWithoutExtension(fileName) + fileExtension;

                    var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
                    var filePath = Path.Combine(destinationDirectory, sanitizedFileName);

                    if (!File.Exists(filePath))
                    {
                        await DownloadFileFromGoogleDrive(fileId, fileName, destinationDirectory, exportMimeType);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {fileName} as it already exists for student: {student.Profile.Name.FullName}");
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

            // Missing code for downloading and saving the file
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

            // Save the downloaded file to the destination directory
            Console.WriteLine($"Saving file to: {filePath}");

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    stream.WriteTo(fileStream);
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
        //Private methods
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
                        Console.WriteLine($"Download failed for {fileName}");
                        break;
                    }
            }
        }
    }
}