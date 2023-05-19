using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using SynEx.Helpers;
using TLH.IntegrationServices;

namespace TLH.ClassroomApi
{
    public static class ClassroomApiHelper
    {
        // Returns the user's selected item from a list.
        public static T GetUserSelection<T>(IList<T> itemList, string displayMessage)
        {
            Console.WriteLine();
            Console.WriteLine(displayMessage);

            foreach (var item in itemList)
            {
                if (item is Course course)
                {
                    Console.WriteLine($"{itemList.IndexOf(item) + 1}. {course.Name}");
                }
                else if (item is Student student)
                {
                    Console.WriteLine($"{itemList.IndexOf(item) + 1}. {student.Profile.Name.FullName}");
                }
            }

            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= itemList.Count)
                {
                    return itemList[selection - 1];
                }
                Console.WriteLine("Invalid selection. Please try again.");
            }
        }
        // Selects a classroom and returns its ID.
        public static async ValueTask<string> SelectClassroomAndGetId()
        {
            var request = GoogleApiService.ClassroomService.Courses.List();
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var response = await request.ExecuteAsync();
            var selectedCourse = GetUserSelection(response.Courses, "Select a classroom by entering its number:");

            if (string.IsNullOrWhiteSpace(selectedCourse.Id))
            {
                return string.Empty;
            }

            await FileDownloadService.DownloadAllFilesFromClassroom(selectedCourse.Id);
            Console.WriteLine("Press Enter to continue.");
            Console.ReadLine();

            return selectedCourse.Id;
        }
        // Returns the course with the specified ID.
        public static async ValueTask<Course> GetCourse(string courseId)
        {
            return await GoogleApiService.ClassroomService.Courses.Get(courseId).ExecuteAsync();
        }
        // Returns a list of all the courses.
        public static async ValueTask<IList<Course>> GetAllCourses()
        {
            var courses = new List<Course>();
            var request = GoogleApiService.ClassroomService.Courses.List();
            do
            {
                var response = await request.ExecuteAsync();
                courses.AddRange(response.Courses);
                request.PageToken = response.NextPageToken;
            } while (!string.IsNullOrWhiteSpace(request.PageToken));

            return courses;
        }
        // Returns the student with the specified user ID in the specified course.
        public static async ValueTask<Student> GetStudent(string courseId, string userId)
        {
            return await GoogleApiService.ClassroomService.Courses.Students
                .Get(courseId, userId)
                .ExecuteAsync();
        }
        // Returns a list of all the course work in the specified course.
        public static async ValueTask<IList<CourseWork>> ListCourseWork(string courseId)
        {
            var courseWorks = new List<CourseWork>();

            await ExceptionHelper.TryCatchAsync(async () =>
            {
                var request = GoogleApiService.ClassroomService.Courses.CourseWork.List(courseId);
                do
                {
                    var response = await request.ExecuteAsync();
                    courseWorks.AddRange(response.CourseWork);
                    request.PageToken = response.NextPageToken;
                } while (!string.IsNullOrWhiteSpace(request.PageToken));
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, "Error retrieving course works");
            });

            return courseWorks;
        }
        // Returns a list of all the student submissions for the specified course work in the specified course.
        public static async ValueTask<IList<StudentSubmission>> ListStudentSubmissions(string courseId, string courseWorkId)
        {
            var request = GoogleApiService.ClassroomService.Courses.CourseWork.StudentSubmissions
                .List(courseId, courseWorkId);
            var response = await request.ExecuteAsync();
            return response.StudentSubmissions;
        }
        // Prints the active students in the specified course.
        public static async ValueTask PrintActiveStudentsInClassroom(string courseId)
        {
            var activeStudents = await GetActiveStudents(courseId);

            Console.WriteLine($"Active students in classroom {courseId}:");
            foreach (var student in activeStudents)
            {
                Console.WriteLine(student.Profile.Name.FullName);
            }
        }
        // Returns a list of all the active students in the specified course.
        public static async ValueTask<IList<Student>> GetActiveStudents(string courseId)
        {
            var allStudents = new List<Student>();
            string? nextPageToken = null;

            do
            {
                var request = GoogleApiService.ClassroomService.Courses.Students.List(courseId);
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
            } while (!string.IsNullOrWhiteSpace(nextPageToken));

            return allStudents;
        }
        // Returns the name of the specified course.
        public static string GetCourseName(string courseId)
        {
            var course = GoogleApiService.ClassroomService.Courses.Get(courseId).Execute();
            return DirectoryUtil.SanitizeFolderName(course.Name);
        }
        // Returns the modified time of the specified file in Google Drive.
        public static async ValueTask<DateTime?> GetFileModifiedTimeFromGoogleDrive(string fileId)
        {
            if (GoogleApiService.DriveService == null)
            {
                Console.WriteLine("Error: Google Drive service is not initialized.");
                return null;
            }
            try
            {
                var request = GoogleApiService.DriveService.Files.Get(fileId);
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
        // Returns a dictionary of all the modified times of files in the specified directory on the us er's desktop.
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
                    fileModifiedTimes[fileName] = fileModifiedTime;
                }
            }

            return fileModifiedTimes;
        }
        // Returns a dictionary of all the modified times of files in the specified course in Google Drive.
        public static async ValueTask<Dictionary<string, DateTime?>> GetAllGoogleDriveFilesModifiedTime(string courseId)
        {
            var fileModifiedTimes = new Dictionary<string, DateTime?>();
            var courseWorks = await ListCourseWork(courseId);

            foreach (var courseWork in courseWorks)
            {
                var studentSubmissions = await ListStudentSubmissions(courseId, courseWork.Id);

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