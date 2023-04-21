<<<<<<< HEAD
=======
using Google;
using Google.Apis.Auth.OAuth2;
>>>>>>> parent of e421557 (Teacher download files)
using Google.Apis.Classroom.v1.Data;

namespace TLH
{
    public class DownloadService
    {
<<<<<<< HEAD
        public static async Task DownloadAllFilesFromClassroom(string courseId)
        {
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
                if (attachment?.DriveFile?.Id == null)
                {
                    // New condition to check for attachment.Link
                    if (attachment?.Link != null)
                    {
                        string linkUrl = attachment.Link.Url;
                        string linkFileName = "Link_" + linkUrl.GetHashCode() + ".txt"; // Use the link's hashcode to create a unique file name

                        string filePath = Path.Combine(destinationDirectory, linkFileName);

                        // Check if the file already exists, if not, create and save the link
                        if (!File.Exists(filePath))
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            using (var streamWriter = new StreamWriter(fileStream))
                            {
                                await streamWriter.WriteAsync(linkUrl);
                            }

                            Console.WriteLine($"Saved link: {linkUrl} as {linkFileName} for student: {student.Profile.Name.FullName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Attachment or DriveFile is null, or the Id is missing for student: {student.Profile.Name.FullName}");
                    }
                    continue;
                }

                Google.Apis.Drive.v3.Data.File? driveFile = null;
                try
                {
                    driveFile = GoogleApiHelper.DriveService.Files.Get(attachment.DriveFile.Id).Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching file information: {ex.Message}");
                    continue;
                }

                var fileId = attachment.DriveFile.Id;
                var fileName = attachment.DriveFile.Title;
                var mimeType = driveFile?.MimeType;

                await DownloadFileFromGoogleDrive(fileId, fileName, destinationDirectory, mimeType);
            }
        }

        public static async Task DownloadFileFromGoogleDrive(string fileId, string fileName, string destinationDirectory, string? mimeType = null)
        {
=======
        public static void DownloadCourseWorkFiles(string courseId, CourseWork courseWork, Student student, string studentDirectory)
        {
            if (courseWork.Assignment == null)
            {
                return;
            }
            try
            {
                var submissionRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.StudentSubmissions.List(courseId, courseWork.Id);
                submissionRequest.UserId = student.UserId;
                var submissionResponse = submissionRequest.Execute();

                // Check if there are any attachments in the submissions
                var hasAttachments = submissionResponse.StudentSubmissions.Any(submission =>
                    submission.AssignmentSubmission?.Attachments != null && submission.AssignmentSubmission.Attachments.Count > 0);

                if (hasAttachments)
                {
                    foreach (var submission in submissionResponse.StudentSubmissions)
                    {
                        DownloadAttachments(studentDirectory, submission, student); // Pass the student object
                    }
                }
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nCourseId: {courseId}\nCourseWorkId: {courseWork.Id}\nStudentId: {student.UserId}");
            }
        }
        public static async void DownloadAttachments(string studentDirectory, StudentSubmission submission, Student student)
        {
            var attachments = submission.AssignmentSubmission?.Attachments;

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    Google.Apis.Drive.v3.Data.File? driveFile = null;

                    try
                    {
                        if (attachment.DriveFile != null && !string.IsNullOrEmpty(attachment.DriveFile.Id))
                        {
                            UserCredential? credential = GoogleApiHelper.ClassroomService.HttpClientInitializer as UserCredential;
                            if (credential?.Token?.IsExpired(credential?.Flow?.Clock) == true)
                            {
                                GoogleApiHelper.RefreshAccessToken(credential);
                            }

                            driveFile = GoogleApiHelper.DriveService.Files.Get(attachment.DriveFile.Id).Execute();
                        }
                        else if (attachment.Link != null)
                        {
                            string linkUrl = attachment.Link.Url;
                            string fileName = "Link_" + linkUrl.GetHashCode() + ".txt"; // Use the link's hashcode to create a unique file name

                            string filePath = Path.Combine(studentDirectory, fileName);

                            // Check if the file already exists, if not, create and save the link
                            if (!File.Exists(filePath))
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                                using (var streamWriter = new StreamWriter(fileStream))
                                {
                                    await streamWriter.WriteAsync(linkUrl);
                                }

                                Console.WriteLine($"Saved link: {linkUrl} as {fileName}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"DriveFile object is null or DriveFile.Id is empty for student: {student.Profile.Name.FullName}, Attachment: {attachment.DriveFile?.Id ?? "null"}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error retrieving the attachment: " + ex.Message);
                        Console.ReadLine();

                        continue;
                    }

                    // Check if the attachment is a Google Drive folder, and skip it if it is
                    if (driveFile?.MimeType == "application/vnd.google-apps.folder")
                    {
                        Console.WriteLine($"Skipping folder for student: {student.Profile.Name.FullName}, Attachment: {driveFile.Name}");
                        continue;
                    }

                    DownloadAttachment(studentDirectory, driveFile, student, attachment.DriveFile?.Id);
                }
            }
        }
        public static void DownloadAttachment(string studentDirectory, Google.Apis.Drive.v3.Data.File driveFile, Student student, string attachmentId)
        {
            if (driveFile == null)
            {
                Console.WriteLine($"Error: Drive file is null for student {student.Profile.Name.FullName}, Attachment: {attachmentId ?? "null"}"); return;
            }

            var fileId = driveFile.Id;
            var mimeType = driveFile.MimeType;
>>>>>>> parent of e421557 (Teacher download files)
            var exportMimeType = "";
            var fileExtension = "";

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
                fileExtension = Path.GetExtension(driveFile.Name);
            }

            var fileName = Path.GetFileNameWithoutExtension(driveFile.Name) + fileExtension;
            var filePath = Path.Combine(studentDirectory, DirectoryManager.SanitizeFolderName(fileName));

<<<<<<< HEAD
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
            var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
            var filePath = Path.Combine(destinationDirectory, sanitizedFileName);
            var shortenedFilePath = DirectoryManager.ShortenPath(filePath);

            Console.WriteLine($"Saving file to: {shortenedFilePath}");

            try
            {
                using (var fileStream = new FileStream(shortenedFilePath, FileMode.Create, FileAccess.Write))
=======
            using (var memoryStream = new MemoryStream())
            {
                if (!string.IsNullOrEmpty(exportMimeType))
                {
                    GoogleApiHelper.DriveService.Files.Export(fileId, exportMimeType)
                        .DownloadWithStatus(memoryStream);
                }
                else
>>>>>>> parent of e421557 (Teacher download files)
                {
                    GoogleApiHelper.DriveService.Files.Get(fileId)
                        .DownloadWithStatus(memoryStream);
                }
<<<<<<< HEAD
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
=======

                SaveFile(memoryStream, filePath);
            }
        }
        public static void SaveFile(MemoryStream stream, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath)?.TrimEnd(); // Trim trailing spaces from the directory
            var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
            var sanitizedFilePath = Path.Combine(directory ?? string.Empty, sanitizedFileName ?? string.Empty);

            // Shorten the file path if it's too long
            sanitizedFilePath = DirectoryManager.ShortenPath(sanitizedFilePath);

            // Get the parent directory of the sanitizedFilePath
            var parentDirectory = Path.GetDirectoryName(sanitizedFilePath);

            // Create the directory if it doesn't exist
            if (parentDirectory != null && !Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            using (var fileStream = new FileStream(sanitizedFilePath, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fileStream);
            }
        }
        public static void DownloadAllFilesFromClassroom(string courseId)
        {
            Console.WriteLine($"Attempting to download files from classroom with ID: {courseId}");

            var userDirectory = Program.userPathLocation;
            var students = Program.GetActiveStudents(courseId);
            var courseName = Program.GetCourseName(courseId);

            var courseDirectory = Path.Combine(userDirectory, $"{DirectoryManager.SanitizeFolderName(courseName)}_{courseId}");
            Directory.CreateDirectory(courseDirectory);

            var courseWorksRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.List(courseId);
            var courseWorksResponse = courseWorksRequest.Execute();
            var courseWorks = courseWorksResponse?.CourseWork?.Where(cw => cw.Assignment != null).ToList();

            if (courseWorks != null && courseWorks.Count > 0)
            {
                foreach (var student in students)
                {
                    Console.WriteLine($"Processing student: {student.UserId}");

                    var studentDirectory = DirectoryManager.CreateStudentDirectory(courseDirectory, student);

                    foreach (var courseWork in courseWorks)
                    {
                        Console.WriteLine($"Processing course work: {courseWork.Id}");

                        var submissionRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.StudentSubmissions.List(courseId, courseWork.Id);
                        submissionRequest.UserId = student.UserId;

                        try
                        {
                            var submissionResponse = submissionRequest.Execute();

                            if (submissionResponse != null && submissionResponse.StudentSubmissions != null)
                            {
                                foreach (var submission in submissionResponse.StudentSubmissions)
                                {
                                    var attachments = submission.AssignmentSubmission?.Attachments;

                                    if (attachments != null && attachments.Count > 0)
                                    {
                                        var assignmentDirectory = DirectoryManager.CreateAssignmentDirectory(studentDirectory, courseWork, courseId);
                                        DownloadAttachments(assignmentDirectory, submission, student);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"No submissions found for student {student.UserId} and course work {courseWork.Id}");
                            }
                        }
                        catch (GoogleApiException ex)
                        {
                            Console.WriteLine($"Google API Error while executing submissionRequest: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No course works found.");
>>>>>>> parent of e421557 (Teacher download files)
            }
        }
    }
}