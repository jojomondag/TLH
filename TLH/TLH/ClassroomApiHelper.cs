using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;

namespace TLH
{
    public static class ClassroomApiHelper
    {
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
    }
}