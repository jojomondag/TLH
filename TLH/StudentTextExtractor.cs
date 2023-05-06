using System.Xml;
using Xceed.Words.NET;

namespace TLH
{
    internal class StudentTextExtractor
    {
        public StudentTextExtractor()
        {
            fileHandlers = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { ".docx", ExtractTextFromDocx }
            };

            var textBasedExtensions = new List<string> { ".cs", ".txt", ".java", ".js", ".py", ".cpp" };

            foreach (var ext in textBasedExtensions)
            {
                fileHandlers[ext] = ExtractTextFromTxt;
            }
        }

        private Dictionary<string, Func<string, string>> fileHandlers;

        public async Task ExtractAndPrintTextData()
        {
            var allStudentExtractedText = await ExtractTextFromStudentAssignments(await ClassroomApiHelper.SelectClassroomAndGetId());

            if (allStudentExtractedText != null)
            {
                foreach (var student in allStudentExtractedText)
                {
                    Console.WriteLine(student.Key);
                    foreach (var assignment in student.Value)
                    {
                        Console.WriteLine(assignment.Item1);

                        // Join the text strings and print them as a whole
                        string wholeText = string.Join(Environment.NewLine, assignment.Item2);
                        Console.WriteLine(wholeText);
                    }
                }
            }
        }

        public async Task<Dictionary<string, List<Tuple<bool, string, List<string>>>>?> ExtractTextFromStudentAssignments(string courseId)
        {
            var userDirectory = MainProgram.userPathLocation;
            var courseName = ClassroomApiHelper.GetCourseName(courseId);

            if (userDirectory == null || courseName == null)
            {
                Console.WriteLine("Error: User directory or course name is null.");
                return null;
            }
            var courseFolderPath = Path.Combine(userDirectory, $"{DirectoryUtil.SanitizeFolderName(courseName)}_{courseId}");

            if (!Directory.Exists(courseFolderPath))
            {
                Console.WriteLine("The specified course folder does not exist on the Desktop.");
                return null;
            }

            var students = await ClassroomApiHelper.GetActiveStudents(courseId);
            var extractedTextData = new Dictionary<string, List<Tuple<bool, string, List<string>>>>();

            foreach (var student in students)
            {
                var studentName = DirectoryUtil.SanitizeFolderName(student.Profile.Name.FullName);
                var studentFolderPath = Path.Combine(courseFolderPath, studentName);
                var outputText = new List<Tuple<bool, string, List<string>>>();

                foreach (var assignmentFolder in Directory.GetDirectories(studentFolderPath))
                {
                    var assignmentName = Path.GetFileName(assignmentFolder);
                    var extractedTextList = new List<string>();

                    foreach (var filePath in Directory.GetFiles(assignmentFolder))
                    {
                        var extractedText = ExtractTextFromFile(filePath);
                        if (!string.IsNullOrEmpty(extractedText))
                        {
                            extractedTextList.Add(extractedText);
                        }
                    }

                    outputText.Add(new Tuple<bool, string, List<string>>(true, assignmentName, extractedTextList));
                }

                var outputFileName = $"{studentName}_ExtractedText.docx";
                var outputFileFullPath = Path.Combine(studentFolderPath, outputFileName);
                SaveTextToWordFile(outputText, outputFileFullPath);

                extractedTextData.Add(studentName, outputText);
            }
            return extractedTextData;
        }

        private string ExtractTextFromFile(string filePath)
        {
            try
            {
                string fileExtension = Path.GetExtension(filePath);

                if (fileHandlers.TryGetValue(fileExtension, out var handler))
                {
                    string extractedText = handler(filePath);
                    return RemoveInvalidXmlChars(extractedText);
                }
                else
                {
                    Console.WriteLine($"Unsupported file format '{fileExtension}' for file '{filePath}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing the file '{filePath}': {ex.Message}");
            }

            return string.Empty;
        }

        private string ExtractTextFromTxt(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        private string ExtractTextFromDocx(string filePath)
        {
            using (DocX document = DocX.Load(filePath))
            {
                return document.Text;
            }
        }

        private void SaveTextToWordFile(List<Tuple<bool, string, List<string>>> textData, string filePath)
        {
            // Create a new document.
            using (DocX document = DocX.Create(filePath))
            {
                // Iterate through each tuple in the text data
                foreach (var tuple in textData)
                {
                    if (tuple.Item1)
                    {
                        // Create a Heading 1 paragraph for the assignment name
                        Xceed.Document.NET.Paragraph headingPara = document.InsertParagraph(tuple.Item2);
                        headingPara.StyleId = "Heading1";

                        // Create a paragraph for each line in the extracted text list
                        foreach (var line in tuple.Item3)
                        {
                            document.InsertParagraph(line);
                        }
                    }
                }

                // Save the document.
                document.Save();
            }

            Console.WriteLine("Document created successfully.");
        }

        private string RemoveInvalidXmlChars(string input)
        {
            var validXmlChars = input.Where(ch => XmlConvert.IsXmlChar(ch)).ToArray();
            return new string(validXmlChars);
        }
    }
}