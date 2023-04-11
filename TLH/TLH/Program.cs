using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using TLH;

namespace TLH
{
    class Program
    {
        static void Main(string[] args)
        {
            GoogleApiHelper.InitializeGoogleServices();
            /*
            var openAi = new OpenAi();
            var message = openAi.ConnectAsync().GetAwaiter().GetResult();
            Console.WriteLine(message);
            */
            //WebScraper.StartScraping();
            Start();
        }
        public static void Start()
        {
            Console.WriteLine("Welcome to the Classroom File Downloader!");
            Console.WriteLine("Press Escape to exit the program at any time.");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Press 1 to select a classroom and download files.");
                Console.WriteLine("Press 2 to evaluate student folders.");
                Console.WriteLine("Press 3 to grade a student.");
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
                        DownloadService.DownloadAllFilesFromClassroom(courseId);
                        break;

                    case ConsoleKey.D2:
                        StudentEvaluation.LookForUserFolder();
                        break;
                    case ConsoleKey.D3:

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

                nextPageToken = response?.NextPageToken;
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
        public static bool HasCourseWorkAttachments(string courseId, string courseWorkId, string studentUserId)
        {
            try
            {
                var submissionRequest = GoogleApiHelper.ClassroomService.Courses.CourseWork.StudentSubmissions.List(courseId, courseWorkId);
                submissionRequest.UserId = studentUserId;
                var submissionResponse = submissionRequest.Execute();

                // Check if there are any attachments in the submissions
                var hasAttachments = submissionResponse.StudentSubmissions.Any(submission =>
                    submission.AssignmentSubmission?.Attachments != null && submission.AssignmentSubmission.Attachments.Count > 0);

                return hasAttachments;
            }
            catch (Exception ex)
            {
                // Log the error and studentUserId
                Console.WriteLine($"Error occurred while checking for attachments for student {studentUserId}: {ex.Message}");
                return false;
            }
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
        public static IList<Student> GetActiveStudents(string courseId)
        {
            var allStudents = new List<Student>();
            string? nextPageToken = null;

            do
            {
                var request = GoogleApiHelper.ClassroomService.Courses.Students.List(courseId);
                request.PageSize = 100;
                request.PageToken = nextPageToken;
                var response = request.Execute();

                //allStudents.AddRange can be null we need too add exception handeling 
                try
                {
                    allStudents.AddRange(response.Students);
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