using Google.Apis.Classroom.v1;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TLH.IntegrationServices;

//This Class creates a Local Excel file with all the data Gathered from the folders crated on the Local Computer.
namespace TLH
{
    public static class StudentEvaluation
    {
        //TODO:Vi behöver kanske skapa en klass till. En klass för Bedömmande, och en annan för att skapa sätta upp filer och managa dessa filer på rätt sätt, visa data hämta extraherea data är olika saker. Kan fråga chat gpt om vad som skall göra vad och hur vi behöver dela upp det så att det känns logiskt.
        public static async Task LookForUserFolder()
        {
            // Get the current year
            string currentYear = DateTime.Now.Year.ToString();

            string StudentAssignmentsName = "StudentAssignments_" + currentYear + ".xlsx";

            // Get the current user's name
            string userName = Environment.UserName;

            // Get the path to the user's desktop folder    
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Combine the desktop path with the user's name to get the expected folder path
            string userFolderPath = Path.Combine(desktopPath, userName);

            // Check if the user folder exists
            if (Directory.Exists(userFolderPath))
            {
                Console.WriteLine($"User folder for {userName} found on desktop.");

                // Generate the student list Excel file
                GenerateStudentAssignment(userFolderPath, StudentAssignmentsName);

                // Create folder on Google Drive
                string googleDriveFolderName = userName + "TLHData";
                string folderId = await DriveService.CreateFolderInGoogleDrive(googleDriveFolderName);

                Console.WriteLine($"Google Drive Folder has been created with ID: {folderId}");

                // Create assignment history folder inside the main Google Drive folder
                string googleDriveAssignmentHistoryFolderName = "AssignmentHistory";
                string assignmentHistoryFolderId = await DriveService.CreateFolderInGoogleDrive(googleDriveAssignmentHistoryFolderName, folderId);

                Console.WriteLine($"Assignment History Folder has been created with ID: {assignmentHistoryFolderId}");

                // Upload the file to Google Drive
                var excelFilePath = Path.Combine(userFolderPath, StudentAssignmentsName);
                await DriveService.UploadFileToGoogleDrive(excelFilePath, StudentAssignmentsName, folderId);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"User folder for {userName} not found on desktop.");
            }
        }
        private static Dictionary<string, List<string>>? GetAllUniqueAssignmentNames()
        {
            var allAssignmentNamesByCourse = new Dictionary<string, List<string>>();

            // Retrieve a list of all active courses
            var request = GoogleApiService.ClassroomService.Courses.List();
            request.TeacherId = "me";
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var courses = request.Execute().Courses;

            // Loop through each course and retrieve its assignments
            foreach (var course in courses)
            {
                // Retrieve the list of assignments for the current course
                var request2 = GoogleApiService.ClassroomService.Courses.CourseWork.List(course.Id);
                request2.OrderBy = "dueDate asc";
                var response2 = request2.Execute();
                var assignments = response2.CourseWork;

                // Check if the assignments object is null
                if (assignments != null)
                {
                    // Create a list to store the unique assignment names for the current course
                    var uniqueAssignmentNames = new List<string>();

                    // Loop through each assignment and add its name to the list
                    foreach (var assignment in assignments)
                    {
                        uniqueAssignmentNames.Add(assignment.Title);
                    }

                    // Add the unique assignment names for the current course to the dictionary
                    allAssignmentNamesByCourse.Add($"{course.Name}_{course.Id}", uniqueAssignmentNames.Distinct().ToList());
                }
                else
                {
                    // Handle the case where there are no assignments for the current course
                    // or there was an error retrieving the assignments
                }
            }

            return allAssignmentNamesByCourse;
        }
        public static async Task<Dictionary<string, List<Tuple<bool, string, List<string>>>>?> ExtractStructuredTextFromAssignments(string courseId)
        {
            // Create an instance of the StudentTextExtractor class
            var studentTextExtractor = new StudentTextExtractor();

            // Call the ExtractTextFromStudentAssignmentsAsync method
            var extractedTextData = await studentTextExtractor.ExtractTextFromStudentAssignments(courseId);

            return extractedTextData ?? new Dictionary<string, List<Tuple<bool, string, List<string>>>>();
        }
        public static void GenerateStudentAssignment(string mainFolderPath, string FileName)
        {
            // Set the EPPlus license context to NonCommercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Create a new Excel package
            using var excelPackage = new ExcelPackage();
            // Get a list of all unique assignment names for all courses
            var allUniqueAssignmentNamesByCourse = GetAllUniqueAssignmentNames();

            if (allUniqueAssignmentNamesByCourse != null)
            {
                // Get a list of all class folders within the main folder
                string[] classFolders = Directory.GetDirectories(mainFolderPath);

                // Loop through each class folder
                foreach (string classFolder in classFolders)
                {
                    // Get the class name from the folder name
                    string className = Path.GetFileName(classFolder);

                    // Get a list of all unique assignment names for the current class
                    if (allUniqueAssignmentNamesByCourse.TryGetValue(className, out var allUniqueAssignmentNames))
                    {
                        // Get a list of all student folders within the class folder
                        string[] studentFolders = Directory.GetDirectories(classFolder);

                        // Add a new worksheet to the Excel package for the current class
                        var worksheet = excelPackage.Workbook.Worksheets.Add(className);

                        // Set the horizontal alignment for the entire worksheet
                        SetHorizontalAlignment(worksheet);

                        // Set the header row for the worksheet
                        SetHeaderRow(worksheet);

                        int column = 2;

                        // Add assignments to the worksheet
                        foreach (var assignmentName in allUniqueAssignmentNames)
                        {
                            string sanitizedAssignmentName = DirectoryUtil.SanitizeFolderName(assignmentName);
                            worksheet.Cells[1, column].Value = sanitizedAssignmentName;
                            column++;
                        }

                        // Add student data to the worksheet
                        AddStudentData(worksheet, studentFolders);

                        // Apply conditional formatting to the worksheet
                        SetConditionalFormatting(worksheet);

                        // Auto-fit column widths
                        worksheet.Cells.AutoFitColumns();
                    }
                    else
                    {
                        // Handle the case where the className key is not found in the dictionary
                    }
                }

                // Save the Excel file to disk
                FileInfo excelFile = new(Path.Combine(mainFolderPath, FileName));
                excelPackage.SaveAs(excelFile);
            }
            else
            {
                // Handle the case where allUniqueAssignmentNamesByCourse is null
            }
        }
        public static void GenerateStudentGradeFile(string mainFolderPath, string FileName)
        {
            // Set the EPPlus license context to NonCommercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Create a new Excel package
            using var excelPackage = new ExcelPackage();

            // Get a list of all class folders within the main folder
            string[] classFolders = Directory.GetDirectories(mainFolderPath);

            // Loop through each class folder
            foreach (string classFolder in classFolders)
            {
                // Get the class name from the folder name
                string className = Path.GetFileName(classFolder);

                // Get a list of all student folders within the class folder
                string[] studentFolders = Directory.GetDirectories(classFolder);

                // Add a new worksheet to the Excel package for the current class
                var worksheet = excelPackage.Workbook.Worksheets.Add(className);

                // Set the horizontal alignment for the entire worksheet
                SetHorizontalAlignment(worksheet);

                // Set the header row for the worksheet
                SetHeaderRow(worksheet);

                // Add student data to the worksheet
                AddStudentData(worksheet, studentFolders);

                // Auto-fit column widths
                worksheet.Cells.AutoFitColumns();
            }

            // Save the Excel file to disk
            FileInfo excelFile = new(Path.Combine(mainFolderPath, FileName));
            excelPackage.SaveAs(excelFile);
        }
        public static Dictionary<string, string> ReadStudentGrades(string mainFolderPath, string FileName)
        {
            var studentGrades = new Dictionary<string, string>();

            // Set the EPPlus license context to NonCommercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Open the grade Excel file
            FileInfo excelFile = new(Path.Combine(mainFolderPath, FileName));

            using var excelPackage = new ExcelPackage(excelFile);

            // Loop through each worksheet in the workbook
            foreach (var worksheet in excelPackage.Workbook.Worksheets)
            {
                // Loop through each row in the worksheet
                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    // Get the student name and grade from the worksheet
                    string studentName = worksheet.Cells[row, 1].Value?.ToString();
                    string grade = worksheet.Cells[row, 2].Value?.ToString();

                    // Add the student name and grade to the dictionary
                    studentGrades[studentName] = grade;
                }
            }

            return studentGrades;
        }

        private static void SetHorizontalAlignment(ExcelWorksheet worksheet)
        {
            worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
        private static void SetHeaderRow(ExcelWorksheet worksheet)
        {
            worksheet.Cells[1, 1].Value = "Student";
        }
        private static void AddStudentData(ExcelWorksheet worksheet, string[] studentFolders)
        {
            // Initialize a row counter
            int row = 2;

            // Add the grade column in the first row (header row)
            // Ensure this is done only once by checking if the header of the grade column is null
            int gradeColumn = worksheet.Dimension.End.Column + 1;
            if (worksheet.Cells[1, gradeColumn].Value == null)
            {
                worksheet.Cells[1, gradeColumn].Value = "Grade";
            }

            // Loop through each student folder
            foreach (string studentFolder in studentFolders)
            {
                // Get the student name from the folder name
                string studentName = Path.GetFileName(studentFolder);

                if (studentName != null)
                {
                    // Add the student name to the worksheet
                    worksheet.Cells[row, 1].Value = studentName;

                    // Loop through each assignment column
                    int column = 2;

                    while (worksheet.Cells[1, column].Value != null)
                    {
                        string assignmentName = worksheet.Cells[1, column].Value?.ToString() ?? string.Empty;
                        string studentAssignmentFolder = Path.Combine(studentFolder ?? string.Empty, assignmentName ?? string.Empty);

                        // Check if the student has files in the assignment folder
                        if (Directory.Exists(studentAssignmentFolder))
                        {
                            int count = Directory.EnumerateFileSystemEntries(studentAssignmentFolder, "*", SearchOption.AllDirectories).Count();
                            if (count > 0)
                            {
                                // Set the cell to the right of the student name to green
                                using var range = worksheet.Cells[row, column];
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                            }
                        }
                        else
                        {
                            // Set the cell to the right of the student name to yellow (no folder found)
                            using var range = worksheet.Cells[row, column];
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        }

                        column++;
                    }

                    // Add grade data for the student here. This could be a variable or a calculated value
                    worksheet.Cells[row, gradeColumn].Value = "A"; // replace "A" with the actual grade

                    // Increment the row counter
                    row++;
                }
            }
        }
        private static void SetConditionalFormatting(ExcelWorksheet worksheet)
        {
            var conditionalFormatting = worksheet.ConditionalFormatting.AddExpression(worksheet.Cells[2, 2, worksheet.Dimension.Rows, 2]);
            conditionalFormatting.Formula = $"COUNTIF(B2:B{worksheet.Dimension.Rows},\">0\")>0";
            conditionalFormatting.Style.Fill.PatternType = ExcelFillStyle.Solid;
            conditionalFormatting.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
            conditionalFormatting.Style.Fill.PatternColor.SetColor(System.Drawing.Color.Red);
            conditionalFormatting.Style.Font.Color.SetColor(System.Drawing.Color.White);
            conditionalFormatting.Style.Font.Bold = true;
        }
        private static void AddStudentGrades(ExcelWorksheet worksheet, Dictionary<string, string> studentGrades)
        {
            // Get the next available column
            int column = worksheet.Dimension.Columns + 1;

            // Loop through each row in the worksheet
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                // Get the student name from the worksheet
                string studentName = worksheet.Cells[row, 1].Value?.ToString();

                // Get the grade for the student from the dictionary
                if (studentGrades.TryGetValue(studentName, out string grade))
                {
                    // Add the grade to the worksheet
                    worksheet.Cells[row, column].Value = grade;
                }
            }
        }
        public static void UpdateStudentGrades(string mainFolderPath, string FileName, Dictionary<string, string> updatedGrades)
        {
            // Set the EPPlus license context to NonCommercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Open the grade Excel file
            FileInfo excelFile = new(Path.Combine(mainFolderPath, FileName));

            using var excelPackage = new ExcelPackage(excelFile);

            // Loop through each worksheet in the workbook
            foreach (var worksheet in excelPackage.Workbook.Worksheets)
            {
                // Loop through each row in the worksheet
                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    // Get the student name from the worksheet
                    string studentName = worksheet.Cells[row, 1].Value?.ToString();

                    // Get the updated grade for the student from the dictionary
                    if (updatedGrades.TryGetValue(studentName, out string updatedGrade))
                    {
                        // Update the grade in the worksheet
                        worksheet.Cells[row, 2].Value = updatedGrade;
                    }
                }
            }

            // Save the changes to the Excel file
            excelPackage.Save();
        }

    }
}