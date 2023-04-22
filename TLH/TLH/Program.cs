using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;

namespace TLH
{
    public static class Program
    {
        public static string? userPathLocation;

        private static async Task Main(string[] args)
        {
            userPathLocation = DirectoryManager.CreateUserDirectoryOnDesktop();
            await Start(); // Use await here
            GoogleApiHelper.InitializeGoogleServices();
        }

        public static async Task Start()
        {
            Console.WriteLine("Welcome to the Classroom File Downloader!");
            Console.WriteLine("Press Escape to exit the program at any time.");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Press 1 to select a classroom and download files.");
                Console.WriteLine("Press 2 to evaluate all student's");
                Console.WriteLine("Press 3 to grade a course.");
                Console.WriteLine("Press Escape to exit.");
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Escape)
                {
                    break;
                }

                var courseId = "";

                switch (key)
                {
                    case ConsoleKey.D1:
                        courseId = SelectClassroomAndGetId();

                        if (!string.IsNullOrEmpty(courseId))
                        {
                            await DownloadService.DownloadAllFilesFromClassroom(courseId); // Use await here
                            Console.WriteLine("Press Enter to continue.");
                            Console.ReadLine();
                        }
                        break;

                    case ConsoleKey.D2:
                        //have too fix connection too excel
                        StudentEvaluation.LookForUserFolder();
                        break;

                    case ConsoleKey.D3:
                        // ...

                        courseId = SelectClassroomAndGetId();
                        if (!string.IsNullOrEmpty(courseId))
                        {
                            var allStudentExtractedText = await StudentEvaluation.GetAllUniqueExtractedText(courseId);

                            // Rest of the code
                        }
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

        public static string GetCourseName(string courseId)
        {
            var course = GoogleApiHelper.ClassroomService.Courses.Get(courseId).Execute();
            return DirectoryManager.SanitizeFolderName(course.Name);
        }
    }
}