using Google.Apis.Classroom.v1.Data;

namespace TLH
{
    public static class ClassroomApiHelper
    {
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

        public static async Task<Student> GetStudent(string courseId, string userId)
        {
            return await GoogleApiHelper.ClassroomService.Courses.Students.Get(courseId, userId).ExecuteAsync();
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
    }
}