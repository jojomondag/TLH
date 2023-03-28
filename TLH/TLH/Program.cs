﻿using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
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
        public static T GetUserSelection<T>(IList<T> items, string displayMessage)
        {
            Console.WriteLine();
            Console.WriteLine(displayMessage);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is Course course)
                {
                    Console.WriteLine($"{i + 1}. {course.Name}");
                }
                else if (items[i] is Student student)
                {
                    Console.WriteLine($"{i + 1}. {student.Profile.Name.FullName}");
                }
            }

            int selection;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out selection) && selection > 0 && selection <= items.Count)
                {
                    return items[selection - 1];
                }
                Console.WriteLine("Invalid selection. Please try again.");
            }
        }
        public static string SelectClassroomAndGetId()
        {
            var request = GoogleApiHelper.ClassroomService.Courses.List();
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var response = request.Execute();
            var selectedCourse = GetUserSelection<Course>(response.Courses, "Select a classroom by entering its number:");
            return selectedCourse.Id;
        }
        public static string GetStudentsFromClassroom(string courseId)
        {
            var allStudents = new List<Student>();
            string? nextPageToken = null;

            do
            {
                var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
                request.PageSize = 100;
                request.PageToken = nextPageToken;
                var response = request.Execute();
                allStudents.AddRange(response.Students);

                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);

            var selectedStudent = GetUserSelection<Student>(allStudents, "Select a student by entering their number:");
            return selectedStudent.Profile.Id;
        }
        public static string GetEmailFromStudent(string studentId)
        {
            var request = GoogleApiHelper.ClassroomService.UserProfiles.Get(studentId);
            var response = request.Execute();
            return response.EmailAddress;
        }
        public static void DownloadAllFilesFromClassroom(string courseId)
        {
            var userDirectory = DirectoryManager.CreateStudentDirectoryOnDesktop();
            var students = GetActiveStudents(courseId);
            var courseName = GetCourseName(courseId);

            var courseDirectory = Path.Combine(userDirectory, DirectoryManager.SanitizeFolderName(courseName));
            Directory.CreateDirectory(courseDirectory);

            Parallel.ForEach(students, student =>
            {
                var studentDirectory = DirectoryManager.CreateStudentDirectory(courseDirectory, student);

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

            DirectoryManager.DeleteEmptyFolders(courseDirectory);
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
            string? nextPageToken = null;

            do
            {
                var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
                request.PageSize = 100;
                request.PageToken = nextPageToken;
                var response = request.Execute();
                allStudents.AddRange(response.Students);

                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);

            return allStudents;
        }
        private static string GetCourseName(string courseId)
        {
            var course = GoogleApiHelper.ClassroomService.Courses.Get(courseId).Execute();
            return DirectoryManager.SanitizeFolderName(course.Name);
        }
        private static void DownloadCourseWorkFiles(string courseId, CourseWork courseWork, Student student, string studentDirectory)
        {
            if (courseWork.Assignment == null)
            {
                return;
            }

            string assignmentDirectory = DirectoryManager.CreateAssignmentDirectory(studentDirectory, courseWork);

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
        private static void DownloadAttachments(string studentDirectory, StudentSubmission submission)
        {
            var attachments = submission.AssignmentSubmission?.Attachments;

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    Google.Apis.Drive.v3.Data.File? driveFile = null;

                    try
                    {
                        if (attachment.DriveFile != null)
                        {
                            UserCredential? credential = GoogleApiHelper.ClassroomService.HttpClientInitializer as UserCredential;
                            if (credential?.Token?.IsExpired(credential?.Flow?.Clock) == true)
                            {
                                if (credential != null)
                                {
                                    GoogleApiHelper.RefreshAccessToken(credential);
                                }
                                else
                                {
                                    // Handle the case when the credential is null, if necessary.
                                }
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

                    DownloadAttachment(studentDirectory, driveFile);
                }
            }
        }
        private static void DownloadAttachment(string studentDirectory, Google.Apis.Drive.v3.Data.File driveFile)
        {
            if (driveFile == null)
            {
                Console.WriteLine("Error: Drive file is null.");
                return;
            }

            var fileId = driveFile.Id;
            var mimeType = driveFile.MimeType;
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

            using (var memoryStream = new MemoryStream())
            {
                if (!string.IsNullOrEmpty(exportMimeType))
                {
                    GoogleApiHelper.DriveService.Files.Export(fileId, exportMimeType)
                        .DownloadWithStatus(memoryStream);
                }
                else
                {
                    GoogleApiHelper.DriveService.Files.Get(fileId)
                        .DownloadWithStatus(memoryStream);
                }

                SaveFile(memoryStream, filePath);
            }
        }
        private static void SaveFile(MemoryStream stream, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath);
            var sanitizedFileName = DirectoryManager.SanitizeFolderName(fileName);
            var sanitizedFilePath = Path.Combine(directory ?? string.Empty, sanitizedFileName ?? string.Empty);

            using (var fileStream = new FileStream(sanitizedFilePath, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fileStream);
            }
        }
    }
}