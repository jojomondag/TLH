using TLH.ClassroomApi;
using TLH.StudentManagement;
using TLH.IntegrationServices;

namespace TLH
{
    public static class MainProgram
    {
        public static string? userPathLocation;

        private static async Task Main(string[] args)
        {
            userPathLocation = DirectoryUtil.CreateUserDirectoryOnDesktop();
            GoogleApiUtil.InitializeGoogleServices();
            await Start();
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

                switch (key)
                {
                    case ConsoleKey.D1:
                        await ClassroomApiHelper.SelectClassroomAndGetId();
                        break;

                    case ConsoleKey.D2:
                        StudentEvaluation.LookForUserFolder();
                        break;

                    case ConsoleKey.D3:
                        StudentTextExtractor ste = new StudentTextExtractor();
                        await ste.ExtractAndPrintTextData();

                        break;

                    default:
                        Console.WriteLine("Invalid input. Please try again.");
                        break;
                }
            }
        }
    }
}