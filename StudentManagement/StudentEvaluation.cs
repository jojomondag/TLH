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
        public static void LookForUserFolder()
        {
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
                GenerateStudentAssignment(userFolderPath);
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
            var request = GoogleApiUtil.ClassroomService.Courses.List();
            request.TeacherId = "me";
            request.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var courses = request.Execute().Courses;

            // Loop through each course and retrieve its assignments
            foreach (var course in courses)
            {
                // Retrieve the list of assignments for the current course
                var request2 = GoogleApiUtil.ClassroomService.Courses.CourseWork.List(course.Id);
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

        public static void GenerateStudentAssignment(string mainFolderPath)
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
                FileInfo excelFile = new(Path.Combine(mainFolderPath, "StudentAssignments.xlsx"));
                excelPackage.SaveAs(excelFile);
            }
            else
            {
                // Handle the case where allUniqueAssignmentNamesByCourse is null
            }
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
                            else
                            {
                                // Set the cell to the right of the student name to red
                                using var range = worksheet.Cells[row, column];
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
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
    }
}