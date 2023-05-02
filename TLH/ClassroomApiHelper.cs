using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace TLH
{
    public static class ClassroomApiHelper
    {
        public static async Task<string> GetAccessTokenAsync()
        {
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = "YOUR_CLIENT_ID",
                    ClientSecret = "YOUR_CLIENT_SECRET",
                },
                new[] { ClassroomService.Scope.ClassroomCoursesReadonly, DriveService.Scope.Drive },
                "user",
                CancellationToken.None,
                new FileDataStore("TLH.TokenCache"));

            return credential.Token.AccessToken;
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
        public static async Task<string> SelectClassroomAndGetId()
        {
            var request = GoogleApiHelper.ClassroomService.Courses.List();
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var response = request.Execute();
            var selectedCourse = GetUserSelection<Course>(response.Courses, "Select a classroom by entering its number:");

            if (!string.IsNullOrEmpty(selectedCourse.Id))
            {
                await DownloadService.DownloadAllFilesFromClassroom(selectedCourse.Id);
                Console.WriteLine("Press Enter to continue.");
                Console.ReadLine();
            }

            return selectedCourse.Id;
        }
        public static async Task<Course> GetCourse(string courseId)
        {
            return await GoogleApiHelper.ClassroomService.Courses.Get(courseId).ExecuteAsync();
        }
        public static async Task<List<Course>> GetAllCourses()
        {
            var courses = new List<Course>();
            var request = GoogleApiHelper.ClassroomService.Courses.List();
            do
            {
                var response = await request.ExecuteAsync();
                courses.AddRange(response.Courses);
                request.PageToken = response.NextPageToken;
            } while (!string.IsNullOrEmpty(request.PageToken));

            return courses;
        }
        public static async Task<Student> GetStudent(string courseId, string userId)
        {
            return await GoogleApiHelper.ClassroomService.Courses.Students.Get(courseId, userId).ExecuteAsync();
        }
        public static async Task<List<CourseWork>> ListCourseWork(string courseId)
        {
            var courseWorks = new List<CourseWork>();
            var request = GoogleApiHelper.ClassroomService.Courses.CourseWork.List(courseId);
            do
            {
                var response = await request.ExecuteAsync();
                courseWorks.AddRange(response.CourseWork);
                request.PageToken = response.NextPageToken;
            } while (!string.IsNullOrEmpty(request.PageToken));

            return courseWorks;
        }
        public static async Task<IList<StudentSubmission>> ListStudentSubmissions(string courseId, string courseWorkId)
        {
            var request = GoogleApiHelper.ClassroomService.Courses.CourseWork.StudentSubmissions.List(courseId, courseWorkId);
            var response = await request.ExecuteAsync();
            return response.StudentSubmissions;
        }
        public static async Task PrintActiveStudentsInClassroom(string courseId)
        {
            var activeStudents = await GetActiveStudents(courseId);

            Console.WriteLine($"Active students in classroom {courseId}:");
            foreach (var student in activeStudents)
            {
                Console.WriteLine(student.Profile.Name.FullName);
            }
        }
        public static async Task<IList<Student>> GetActiveStudents(string courseId)
        {
            var allStudents = new List<Student>();
            string? nextPageToken = null;

            do
            {
                var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
                request.PageSize = 100;
                request.PageToken = nextPageToken;
                var response = await request.ExecuteAsync();

                try
                {
                    if (response.Students != null)
                    {
                        allStudents.AddRange(response.Students);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred while getting students for classroom {courseId}: {ex.Message}");
                }

                nextPageToken = response.NextPageToken;
            } while (nextPageToken != null);

            return allStudents;
        }
        public static string GetCourseName(string courseId)
        {
            var course = GoogleApiHelper.ClassroomService.Courses.Get(courseId).Execute();
            return DirectoryManager.SanitizeFolderName(course.Name);
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

                return file.ModifiedTime?.ToUniversalTime(); // Convert the modified time to UTC
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving modified time for file ID {fileId}: {ex.Message}");
                return null;
            }
        }
        public static Dictionary<string, DateTime> GetAllDesktopFilesModifiedTime(string rootDirectory)
        {
            var fileModifiedTimes = new Dictionary<string, DateTime>();

            foreach (var file in Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                DateTime fileModifiedTime = File.GetLastWriteTimeUtc(file);

                if (!fileModifiedTimes.ContainsKey(fileName))
                {
                    fileModifiedTimes.Add(fileName, fileModifiedTime);
                }
                else
                {
                    // Key already exists, update the value
                    fileModifiedTimes[fileName] = fileModifiedTime;
                }
            }

            return fileModifiedTimes;
        }
        public static async Task<Dictionary<string, DateTime?>> GetAllGoogleDriveFilesModifiedTime(string courseId)
        {
            Dictionary<string, DateTime?> fileModifiedTimes = new Dictionary<string, DateTime?>();
            var courseWorkList = await ClassroomApiHelper.ListCourseWork(courseId);

            foreach (var courseWork in courseWorkList)
            {
                var studentSubmissions = await ClassroomApiHelper.ListStudentSubmissions(courseId, courseWork.Id);

                foreach (var submission in studentSubmissions)
                {
                    if (submission.AssignmentSubmission?.Attachments != null && submission.AssignmentSubmission.Attachments.Count > 0)
                    {
                        foreach (var attachment in submission.AssignmentSubmission.Attachments)
                        {
                            if (attachment?.DriveFile?.Id != null)
                            {
                                var fileId = attachment.DriveFile.Id;
                                var fileName = attachment.DriveFile.Title;

                                var fileModifiedTime = await GetFileModifiedTimeFromGoogleDrive(fileId);
                                if (!fileModifiedTimes.ContainsKey(fileName))
                                {
                                    fileModifiedTimes.Add(fileName, fileModifiedTime);
                                }
                                else
                                {
                                    // Key already exists, update the value
                                    fileModifiedTimes[fileName] = fileModifiedTime;
                                }
                            }
                        }
                    }
                }
            }

            return fileModifiedTimes;
        }

    }
}