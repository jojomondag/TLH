using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace TLH
{
    public class ExcelGenerator
    {
        public ExcelGenerator()
        {
            // Set the EPPlus license context to NonCommercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public void GenerateStudentAssignment(string mainFolderPath, string FileName)
        {
            // Create a new Excel package
            using var excelPackage = new ExcelPackage();

            // Get a list of all unique assignment names for all courses
            var allUniqueAssignmentNamesByCourse =
                StudentEvaluation.GetAllUniqueAssignmentNames();

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
                        // The method AddStudentDataWithoutGrades is called here instead of AddStudentData
                        AddStudentDataWithoutGrades(worksheet, studentFolders);

                        // Apply conditional formatting to the worksheet
                        SetConditionalFormatting(worksheet);

                        // Auto-fit column widths
                        worksheet.Cells.AutoFitColumns();
                    }
                }

                // Save the Excel file to disk
                FileInfo excelFile = new(Path.Combine(mainFolderPath, FileName));
                excelPackage.SaveAs(excelFile);
            }
        }
        public void GenerateStudentGradeFile(string mainFolderPath, string FileName)
        {
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
        public Dictionary<string, string> ReadStudentGrades(string mainFolderPath, string FileName)
        {
            var studentGrades = new Dictionary<string, string>();

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
        private void SetHorizontalAlignment(ExcelWorksheet worksheet)
        {
            worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
        private void SetHeaderRow(ExcelWorksheet worksheet)
        {
            worksheet.Cells[1, 1].Value = "Student";
        }
        private void AddStudentData(ExcelWorksheet worksheet, string[] studentFolders)
        {
            int row = 2;

            int gradeColumn = worksheet.Dimension.End.Column + 1;
            if (worksheet.Cells[1, gradeColumn].Value == null)
            {
                worksheet.Cells[1, gradeColumn].Value = "Grade";
            }

            foreach (string studentFolder in studentFolders)
            {
                string studentName = Path.GetFileName(studentFolder);

                if (studentName != null)
                {
                    worksheet.Cells[row, 1].Value = studentName;

                    int column = 2;

                    while (worksheet.Cells[1, column].Value != null)
                    {
                        string assignmentName = worksheet.Cells[1, column].Value?.ToString() ?? string.Empty;
                        string studentAssignmentFolder = Path.Combine(studentFolder ?? string.Empty, assignmentName ?? string.Empty);

                        if (Directory.Exists(studentAssignmentFolder))
                        {
                            int count = Directory.EnumerateFileSystemEntries(studentAssignmentFolder, "*", SearchOption.AllDirectories).Count();
                            if (count > 0)
                            {
                                using var range = worksheet.Cells[row, column];
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                            }
                        }
                        else
                        {
                            using var range = worksheet.Cells[row, column];
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        }

                        column++;
                    }

                    // Set the student's grade only if the cell is currently empty
                    if (worksheet.Cells[row, gradeColumn].Value == null)
                    {
                        // TODO: Calculate the student's grade and assign it to the cell
                        // This is a placeholder, replace it with your actual grade calculation logic
                        string studentGrade = "A";
                        worksheet.Cells[row, gradeColumn].Value = studentGrade;
                    }

                    row++;
                }
            }
        }
        private void AddStudentDataWithoutGrades(ExcelWorksheet worksheet, string[] studentFolders)
        {
            int row = 2;

            foreach (string studentFolder in studentFolders)
            {
                string studentName = Path.GetFileName(studentFolder);

                if (studentName != null)
                {
                    worksheet.Cells[row, 1].Value = studentName;

                    int column = 2;

                    while (worksheet.Cells[1, column].Value != null)
                    {
                        string assignmentName = worksheet.Cells[1, column].Value?.ToString() ?? string.Empty;
                        string studentAssignmentFolder = Path.Combine(studentFolder ?? string.Empty, assignmentName ?? string.Empty);

                        if (Directory.Exists(studentAssignmentFolder))
                        {
                            int count = Directory.EnumerateFileSystemEntries(studentAssignmentFolder, "*", SearchOption.AllDirectories).Count();
                            if (count > 0)
                            {
                                using var range = worksheet.Cells[row, column];
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
                            }
                        }
                        else
                        {
                            using var range = worksheet.Cells[row, column];
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        }

                        column++;
                    }

                    row++;
                }
            }
        }
        private void SetConditionalFormatting(ExcelWorksheet worksheet)
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