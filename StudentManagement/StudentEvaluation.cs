using Google.Apis.Classroom.v1;
using TLH.IntegrationServices;

namespace TLH
{
    public static class StudentEvaluation
    {
        public static async Task LookForUserFolder()
        {
            string currentYear = DateTime.Now.Year.ToString();
            string StudentAssignmentsName = "StudentAssignments_" + currentYear + ".xlsx";
            string StudentCourseGradeFileName = "StudentCourseGrade_" + currentYear + ".xlsx";

            string userName = Environment.UserName;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string userFolderPath = Path.Combine(desktopPath, userName);

            if (Directory.Exists(userFolderPath))
            {
                Console.WriteLine($"User folder for {userName} found on desktop.");

                // Creating an instance of ExcelGenerator
                var excelGenerator = new ExcelGenerator();
                excelGenerator.GenerateStudentAssignment(userFolderPath, StudentAssignmentsName);

                string googleDriveFolderName = userName + "TLHData";
                string folderId = await DriveService.CreateFolderInGoogleDrive(googleDriveFolderName);

                Console.WriteLine($"Google Drive Folder has been created with ID: {folderId}");

                string googleDriveAssignmentHistoryFolderName = "AssignmentHistory";
                string assignmentHistoryFolderId = await DriveService.CreateFolderInGoogleDrive(googleDriveAssignmentHistoryFolderName, folderId);

                Console.WriteLine($"Assignment History Folder has been created with ID: {assignmentHistoryFolderId}");

                var excelFilePath = Path.Combine(userFolderPath, StudentAssignmentsName);
                await DriveService.UploadFileToGoogleDrive(excelFilePath, StudentAssignmentsName, folderId, overwrite: true);

                // Upload a backup copy to the AssignmentHistory folder
                var backupName = StudentAssignmentsName.Replace(".xlsx", $"_Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                await DriveService.UploadFileToGoogleDrive(excelFilePath, backupName, assignmentHistoryFolderId, overwrite: false);

                // Check if the StudentGradeFile exists
                if (!File.Exists(Path.Combine(userFolderPath, StudentCourseGradeFileName)))
                {
                    excelGenerator.GenerateStudentGradeFile(userFolderPath, StudentCourseGradeFileName);
                    var gradeExcelFilePath = Path.Combine(userFolderPath, StudentCourseGradeFileName);

                    // Check if a file with the same name exists in Google Drive
                    bool fileExists = await DriveService.CheckIfFileExists(StudentCourseGradeFileName, folderId);
                    if (!fileExists)
                    {
                        await DriveService.UploadFileToGoogleDrive(gradeExcelFilePath, StudentCourseGradeFileName, folderId, overwrite: false);
                    }

                    // Regardless of whether the file exists in TLHData, create and upload a backup copy to the AssignmentHistory folder
                    var backupGradeFileName = StudentCourseGradeFileName.Replace(".xlsx", $"_Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                    await DriveService.UploadFileToGoogleDrive(gradeExcelFilePath, backupGradeFileName, assignmentHistoryFolderId, overwrite: false);
                }
                else
                {
                    Console.WriteLine($"The StudentGradeFile already exists, skipping file creation.");
                    var gradeExcelFilePath = Path.Combine(userFolderPath, StudentCourseGradeFileName);
                    var backupGradeFileName = StudentCourseGradeFileName.Replace(".xlsx", $"_Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                    await DriveService.UploadFileToGoogleDrive(gradeExcelFilePath, backupGradeFileName, assignmentHistoryFolderId, overwrite: false);
                }
            }
            else
            {
                Console.WriteLine($"User folder for {userName} not found on desktop.");
            }
        }
        public static Dictionary<string, List<string>>? GetAllUniqueAssignmentNames()
        {
            var allAssignmentNamesByCourse = new Dictionary<string, List<string>>();

            var request = GoogleApiService.ClassroomService.Courses.List();
            request.TeacherId = "me";
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var courses = request.Execute().Courses;

            foreach (var course in courses)
            {
                var request2 = GoogleApiService.ClassroomService.Courses.CourseWork.List(course.Id);
                request2.OrderBy = "dueDate asc";
                var response2 = request2.Execute();
                var assignments = response2.CourseWork;

                if (assignments != null)
                {
                    var uniqueAssignmentNames = new List<string>();

                    foreach (var assignment in assignments)
                    {
                        uniqueAssignmentNames.Add(assignment.Title);
                    }

                    allAssignmentNamesByCourse.Add($"{course.Name}_{course.Id}", uniqueAssignmentNames.Distinct().ToList());
                }
            }

            return allAssignmentNamesByCourse;
        }
        public static async Task<Dictionary<string, List<Tuple<bool, string, List<string>>>>?> ExtractStructuredTextFromAssignments(string courseId)
        {
            var studentTextExtractor = new StudentTextExtractor();
            var extractedTextData = await studentTextExtractor.ExtractTextFromStudentAssignments(courseId);

            return extractedTextData ?? new Dictionary<string, List<Tuple<bool, string, List<string>>>>();
        }
    }
}