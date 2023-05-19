using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using System.Collections.Concurrent;
using TLH.IntegrationServices;
using TLH.Services;

namespace TLH.ClassroomApi
{
    public static class ClassroomApiHelper
    {
        public static async Task<T> GetUserSelection<T>(IList<T> itemList, string displayMessage)
        {
            await MessageHelper.SaveMessageAsync("\n" + displayMessage);

            foreach (var item in itemList)
            {
                if (item is Course course)
                {
                    await MessageHelper.SaveMessageAsync($"{itemList.IndexOf(item) + 1}. {course.Name}");
                }
                else if (item is Student student)
                {
                    await MessageHelper.SaveMessageAsync($"{itemList.IndexOf(item) + 1}. {student.Profile.Name.FullName}");
                }
            }

            while (true)
            {
                if (int.TryParse(await MessageHelper.GetInputAsync(), out int selection) && selection > 0 && selection <= itemList.Count)
                {
                    return itemList[selection - 1];
                }
                await MessageHelper.SaveMessageAsync("Invalid selection. Please try again.");
            }
        }

        // Selects a classroom and returns its ID.
        public static async ValueTask<string> SelectClassroomAndGetId()
        {
            var request = GoogleApiService.ClassroomService.Courses.List();
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var response = await request.ExecuteAsync();
            var selectedCourse = await GetUserSelection(response.Courses, "Select a classroom by entering its number:");

            if (string.IsNullOrWhiteSpace(selectedCourse.Id))
            {
                return string.Empty;
            }

            await FileDownloadService.DownloadAllFilesFromClassroom(selectedCourse.Id);
            await MessageHelper.SaveMessageAsync("Press Enter to continue.");
            await MessageHelper.GetInputAsync();

            return selectedCourse.Id;
        }


        // Returns the course with the specified ID.
        private static CacheService<Course> _courseCacheService = new CacheService<Course>();
        // Returns the user's selected item from a list.
        public static async Task<Course?> GetCourse(string courseId)
        {
            return await ExceptionHelper.TryCatchAsync(async () =>
            {
                // Try to get the course from the cache
                var course = _courseCacheService.Get(courseId);

                // If the course is not in the cache, fetch it and add it to the cache
                if (course == null)
                {
                    course = await GoogleApiService.ClassroomService.Courses.Get(courseId).ExecuteAsync().ConfigureAwait(false);
                    _courseCacheService.Add(courseId, course);
                }

                return course;
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, $"Error getting course with ID: {courseId}");
            });
        }
        private static CacheService<IList<Course>> _allCoursesCacheService = new CacheService<IList<Course>>();
        public static async ValueTask<IList<Course>?> GetAllCourses()
        {
            return await ExceptionHelper.TryCatchAsync(async () =>
            {
                // Try to get the courses from the cache
                var courses = _allCoursesCacheService.Get("all");

                // If the courses are not in the cache, fetch them and add them to the cache
                if (courses == null)
                {
                    var courseList = new List<Course>();  // change this line
                    var request = GoogleApiService.ClassroomService.Courses.List();
                    do
                    {
                        var response = await request.ExecuteAsync();
                        courseList.AddRange(response.Courses);  // use List<Course>.AddRange method
                        request.PageToken = response.NextPageToken;
                    } while (!string.IsNullOrWhiteSpace(request.PageToken));

                    courses = courseList;  // assign back to courses
                    _allCoursesCacheService.Add("all", courses);
                }

                return courses;
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, "Error getting all courses");
            });
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

            MessageHelper.SaveMessage($"Active students in classroom {courseId}:");
            foreach (var student in activeStudents)
            {
                MessageHelper.SaveMessage(student.Profile.Name.FullName);
            }
        }
        // Returns a list of all the active students in the specified course.
        private static CacheService<IList<Student>> _allStudentsCacheService = new CacheService<IList<Student>>();
        public static async ValueTask<IList<Student>> GetActiveStudents(string courseId)
        {
            return await ExceptionHelper.TryCatchAsync(async () =>
            {
                // Try to get the students from the cache
                var students = _allStudentsCacheService.Get(courseId);

                // If the students are not in the cache, fetch them and add them to the cache
                if (students == null)
                {
                    var allStudents = new List<Student>();
                    string? nextPageToken = null;

                    do
                    {
                        var request = GoogleApiService.ClassroomService.Courses.Students.List(courseId);
                        request.PageSize = 100;
                        request.PageToken = nextPageToken;
                        var response = await request.ExecuteAsync();

                        if (response.Students != null)
                        {
                            allStudents.AddRange(response.Students);
                        }

                        nextPageToken = response.NextPageToken;
                    } while (!string.IsNullOrWhiteSpace(nextPageToken));

                    students = allStudents;
                    _allStudentsCacheService.Add(courseId, students);
                }

                return students;
            }, ex =>
            {
                ExceptionHelper.HandleException(ex, $"Error getting active students for course with ID: {courseId}");
                return new List<Student>(); // return an empty list in case of an exception
            });
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
            try
            {
                if (GoogleApiService.DriveService == null)
                {
                    await MessageHelper.SaveErrorAsync("Error: Google Drive service is not initialized.");
                    return null;
                }

                var request = GoogleApiService.DriveService.Files.Get(fileId);
                request.Fields = "modifiedTime, name";
                var file = await request.ExecuteAsync();

                if (file == null)
                {
                    await MessageHelper.SaveErrorAsync($"Error: File with ID {fileId} not found.");
                    return null;
                }

                return file.ModifiedTime?.ToUniversalTime();
            }
            catch (Exception ex)
            {
                await MessageHelper.SaveErrorAsync($"Error retrieving modified time for file ID {fileId}: {ex.Message}");
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
                    fileModifiedTimes[fileName] = fileModifiedTime;
                }
            }

            return fileModifiedTimes;
        }
        // Returns a dictionary of all the modified times of files in the specified course in Google Drive.
        public static async ValueTask<Dictionary<string, DateTime?>> GetAllGoogleDriveFilesModifiedTime(string courseId)
        {
            var fileModifiedTimes = new ConcurrentDictionary<string, DateTime?>();
            var courseWorks = await ListCourseWork(courseId);

            List<Task> tasks = new List<Task>();

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

                                tasks.Add(Task.Run(async () =>
                                {
                                    var fileModifiedTime = await GetFileModifiedTimeFromGoogleDrive(fileId);
                                    fileModifiedTimes.AddOrUpdate(fileName, fileModifiedTime, (key, oldValue) => fileModifiedTime);
                                }));
                            }
                        }
                    }
                }
            }
            // wait for all tasks to complete
            await Task.WhenAll(tasks);

            return new Dictionary<string, DateTime?>(fileModifiedTimes);
        }
    }
}