using Xceed.Words.NET;
using System.Xml;

namespace TLH
{
    internal class StudentTextExtractor
    {
        public StudentTextExtractor()
        {
            fileHandlers = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { ".cs", ExtractTextFromTxt },
                { ".txt", ExtractTextFromTxt },
                { ".java", ExtractTextFromTxt },
                { ".js", ExtractTextFromTxt },
                { ".py", ExtractTextFromTxt },
                { ".cpp", ExtractTextFromTxt },
                { ".docx", ExtractTextFromDocx }
            };
        }
        private Dictionary<string, Func<string, string>> fileHandlers;
        public Dictionary<string, List<Tuple<bool, string, List<string>>>> ExtractTextFromStudentAssignments(string courseId)
        {
            var userDirectory = Program.userPathLocation;
            var courseName = Program.GetCourseName(courseId);
            var courseFolderPath = Path.Combine(userDirectory, $"{DirectoryManager.SanitizeFolderName(courseName)}_{courseId}");

            if (!Directory.Exists(courseFolderPath))
            {
                Console.WriteLine("The specified course folder does not exist on the Desktop.");
                return null;
            }

            var students = Program.GetActiveStudents(courseId);
            var extractedTextData = new Dictionary<string, List<Tuple<bool, string, List<string>>>>();

            foreach (var student in students)
            {
                var studentName = DirectoryManager.SanitizeFolderName(student.Profile.Name.FullName);
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
                    return handler(filePath);
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
                        string validAssignmentName = RemoveInvalidXmlChars(tuple.Item2);
                        Xceed.Document.NET.Paragraph headingPara = document.InsertParagraph(validAssignmentName);
                        headingPara.StyleName = "Heading1";

                        // Create a paragraph for each line in the extracted text list
                        foreach (var line in tuple.Item3)
                        {
                            string validLine = RemoveInvalidXmlChars(line);
                            document.InsertParagraph(validLine);
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
