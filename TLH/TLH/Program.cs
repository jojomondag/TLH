using Google.Apis.Classroom.v1;
using System.Text.RegularExpressions;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Download;
using Google.Apis.Auth.OAuth2;

namespace TLH
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Classroom File Downloader!");
            Console.WriteLine("Press Escape to exit the program at any time.");
            Console.WriteLine();

            GoogleApiHelper.InitializeGoogleServices();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Press 1 to select a classroom and download files.");
                Console.WriteLine("Press 2 to evaluate student folders.");
                Console.WriteLine("Press Escape to exit.");
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Escape)
                {
                    break;
                }

                switch (key)
                {
                    case ConsoleKey.D1:
                        var courseId = SelectClassroomAndGetId();
                        DownloadAllFilesFromClassroom(courseId);
                        break;

                    case ConsoleKey.D2:
                        StudentEvaluation.LookForUserFolder();

                        break;

                    default:
                        Console.WriteLine("Invalid input. Please try again.");
                        break;
                }
            }
        }
        public static string SelectClassroomAndGetId()
        {
            var request = GoogleApiHelper.ClassroomService.Courses.List();
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var response = request.Execute();
            Console.WriteLine();
            Console.WriteLine("Select a classroom by entering its number:");
            for (int i = 0; i < response.Courses.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {response.Courses[i].Name}");
            }

            int selection;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out selection) && selection > 0 && selection <= response.Courses.Count)
                {
                    return response.Courses[selection - 1].Id;
                }
                Console.WriteLine("Invalid selection. Please enter a valid classroom number.");
            }
        }
        public static string GetStudentsFromClassroom(string courseId)
        {
            var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
            var response = request.Execute();

            Console.WriteLine("Select a student by entering their number:");
            for (int i = 0; i < response.Students.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {response.Students[i].Profile.Name.FullName}");
            }

            int selection;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out selection) && selection > 0 && selection <= response.Students.Count)
                {
                    return response.Students[selection - 1].Profile.Id;
                }
                Console.WriteLine("Invalid selection. Please enter a valid student number.");
            }

        }
        public static string GetEmailFromStudent(string studentId)
        {
            var request = GoogleApiHelper.ClassroomService.UserProfiles.Get(studentId);
            var response = request.Execute();
            return response.EmailAddress;
        }
        public static void DownloadAllFilesFromClassroom(string courseId)
        {
            var userDirectory = CreateStudentDirectoryOnDesktop();
            var students = GetActiveStudents(courseId);
            var courseName = GetCourseName(courseId);

            var courseDirectory = Path.Combine(userDirectory, SanitizeFolderName(courseName));
            Directory.CreateDirectory(courseDirectory);

            Parallel.ForEach(students, student =>
            {
                var studentDirectory = CreateStudentDirectory(courseDirectory, student);

                var courseWorkRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.List(courseId);
                var courseWorkResponse = courseWorkRequest.Execute();

                foreach (var courseWork in courseWorkResponse.CourseWork)
                {
                    if (courseWork.Assignment == null)
                    {
                        continue;
                    }

                    DownloadCourseWorkFiles(courseId, courseWork, student, studentDirectory);
                }
            });

            DeleteEmptyFolders(courseDirectory);
        }
        public static void PrintActiveStudentsInClassroom()
        {
            var courseId = SelectClassroomAndGetId();
            var activeStudents = GetActiveStudents(courseId);

            Console.WriteLine($"Active students in classroom {courseId}:");
            foreach (var student in activeStudents)
            {
                Console.WriteLine(student.Profile.Name.FullName);
            }
        }

        private static IList<Student> GetActiveStudents(string courseId)
        {
            var allStudents = new List<Student>();
            string nextPageToken = null;

            do
            {
                var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
                request.PageSize = 100;
                request.PageToken = nextPageToken;
                var response = request.Execute();
                allStudents.AddRange(response.Students);

                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);
            int counter = 0;
            // Print all students before filtering
            Console.WriteLine("All students from the API:");
            foreach (var student in allStudents)
            {
                counter++;
                Console.WriteLine(counter + " " + student.Profile.Name.FullName);
            }

            return allStudents;
        }
        private static string CreateStudentDirectoryOnDesktop()
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var userDirectory = Path.Combine(desktopPath, Environment.UserName);
            Directory.CreateDirectory(userDirectory);
            return userDirectory;
        }
        private static string GetCourseName(string courseId)
        {
            var course = GoogleApiHelper.ClassroomService.Courses.Get(courseId).Execute();
            return SanitizeFolderName(course.Name);
        }
        private static string CreateStudentDirectory(string courseDirectory, Student student)
        {
            var studentName = SanitizeFolderName(student.Profile.Name.FullName);
            var studentDirectory = Path.Combine(courseDirectory, studentName);
            Directory.CreateDirectory(studentDirectory);
            Console.WriteLine($"Created directory for student: {studentName}");
            return studentDirectory;
        }
        private static void DownloadStudentFiles(string courseId, Student student, string studentDirectory)
        {
            var courseWorkRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.List(courseId);
            var courseWorkResponse = courseWorkRequest.Execute();

            foreach (var courseWork in courseWorkResponse.CourseWork)
            {
                if (courseWork.Assignment == null)
                {
                    continue;
                }

                DownloadCourseWorkFiles(courseId, courseWork, student, studentDirectory);
            }
        }
        private static void DownloadCourseWorkFiles(string courseId, CourseWork courseWork, Student student, string studentDirectory)
        {
            if (courseWork.Assignment == null)
            {
                return;
            }

            string assignmentDirectory = CreateAssignmentDirectory(studentDirectory, courseWork);

            try
            {
                var submissionRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.StudentSubmissions.List(courseId, courseWork.Id);
                submissionRequest.UserId = student.UserId;
                var submissionResponse = submissionRequest.Execute();

                foreach (var submission in submissionResponse.StudentSubmissions)
                {
                    DownloadAttachments(assignmentDirectory, submission);
                }
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nCourseId: {courseId}\nCourseWorkId: {courseWork.Id}\nStudentId: {student.UserId}");
            }
        }
        private static string CreateAssignmentDirectory(string studentDirectory, CourseWork courseWork)
        {
            var assignmentName = SanitizeFolderName(courseWork.Title);
            var assignmentDirectory = Path.Combine(studentDirectory, assignmentName);
            Directory.CreateDirectory(assignmentDirectory);
            Console.WriteLine($"Created directory for assignment: {assignmentName}");
            return assignmentDirectory;
        }
        private static void DownloadAttachments(string studentDirectory, StudentSubmission submission)
        {
            var attachments = submission.AssignmentSubmission?.Attachments;

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    Google.Apis.Drive.v3.Data.File driveFile = null;

                    try
                    {
                        if (attachment.DriveFile != null)
                        {
                            UserCredential credential = GoogleApiHelper.ClassroomService.HttpClientInitializer as UserCredential;
                            if (credential.Token.IsExpired(credential.Flow.Clock))
                            {
                                GoogleApiHelper.RefreshAccessToken(credential);
                            }

                            driveFile = GoogleApiHelper.DriveService.Files.Get(attachment.DriveFile.Id).Execute();
                        }
                        else
                        {
                            Console.WriteLine("DriveFile object is null.");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error retrieving the DriveFile: " + ex.Message);
                        continue;
                    }

                    var fileName = driveFile.Name;
                    var filePath = Path.Combine(studentDirectory, fileName);

                    if (File.Exists(filePath))
                    {
                        // Get the file's modified time from Google Drive
                        var driveFileModifiedTime = driveFile.ModifiedTime;

                        // Get the local file's modified time
                        var localFileModifiedTime = File.GetLastWriteTime(filePath);

                        // Check if the local file is older than the Drive file
                        if (localFileModifiedTime < driveFileModifiedTime)
                        {
                            // Download the updated file
                            DownloadAttachment(studentDirectory, attachment);

                            // Remove the old file
                            File.Delete(filePath);
                        }
                    }
                    else
                    {
                        // Download the file if it doesn't exist
                        DownloadAttachment(studentDirectory, attachment);
                    }
                }
            }
        }
        private static void DownloadAttachment(string studentDirectory, Attachment attachment)
        {
            if (attachment == null || attachment.DriveFile == null)
            {
                Console.WriteLine("Error: Attachment is null or does not have a Drive file.");
                return;
            }

            var driveFile = GoogleApiHelper.DriveService.Files.Get(attachment.DriveFile.Id).Execute();
            // ...
        }
        private static void HandleDownloadProgress(IDownloadProgress progress, MemoryStream stream, string filePath)
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Console.WriteLine(progress.BytesDownloaded);
                    break;
                case DownloadStatus.Completed:
                    Console.WriteLine("Download complete.");
                    SaveFile(stream, filePath);
                    break;
                case DownloadStatus.Failed:
                    Console.WriteLine("Download failed.");
                    break;
            }
        }
        private static void SaveFile(MemoryStream stream, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath);
            var sanitizedFileName = SanitizeFolderName(fileName);
            var sanitizedFilePath = Path.Combine(directory, sanitizedFileName);

            using (var fileStream = new FileStream(sanitizedFilePath, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fileStream);
            }
        }
        //Helper Function's
        public static void DeleteEmptyFolders(string directory)
        {
            foreach (var dir in Directory.GetDirectories(directory))
            {
                //Delete all folders that contains now files or folders never delete the first folder in the directory folder
                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0 && dir != directory)
                {
                    Directory.Delete(dir);
                }
                else
                {
                    DeleteEmptyFolders(dir);
                }
            }
        }
        public static string SanitizeFolderName(string folderName)
        {
            var invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var regex = new Regex($"[{Regex.Escape(invalidChars)}]");
            return regex.Replace(folderName, "_");
        }
    }
}