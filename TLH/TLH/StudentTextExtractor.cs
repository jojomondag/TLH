using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Generic;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TLH
{
    internal class StudentTextExtractor
    {
        public void ExtractTextFromStudentAssignments(string courseId)
        {
            var userDirectory = Program.userPathLocation;
            var courseName = Program.GetCourseName(courseId);
            var courseFolderPath = Path.Combine(userDirectory, $"{DirectoryManager.SanitizeFolderName(courseName)}_{courseId}");

            if (!Directory.Exists(courseFolderPath))
            {
                Console.WriteLine("The specified course folder does not exist on the Desktop.");
                return;
            }

            var students = Program.GetActiveStudents(courseId);

            foreach (var student in students)
            {
                var studentName = DirectoryManager.SanitizeFolderName(student.Profile.Name.FullName);
                var studentFolderPath = Path.Combine(courseFolderPath, studentName);
                var outputText = new StringBuilder();

                foreach (var assignmentFolder in Directory.GetDirectories(studentFolderPath))
                {
                    var assignmentName = Path.GetFileName(assignmentFolder);
                    outputText.AppendLine($"{assignmentName}");

                    foreach (var filePath in Directory.GetFiles(assignmentFolder))
                    {
                        var extractedText = ExtractTextFromFile(filePath);
                        if (!string.IsNullOrEmpty(extractedText))
                        {
                            outputText.AppendLine(extractedText);
                        }
                    }
                }

                // Save the extracted text to a Word file
                var outputFileName = $"{studentName}_ExtractedText.docx";
                var outputFileFullPath = Path.Combine(studentFolderPath, outputFileName);
                SaveTextToWordFile(outputText.ToString(), outputFileFullPath);
            }
        }
        private string ExtractTextFromFile(string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return File.ReadAllText(filePath);
            }

            return string.Empty;
        }
        private void SaveTextToWordFile(string text, string filePath)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Add style definitions part and include Heading 1 style
                AddStyleDefinitionsPart(mainPart);

                // Split text by lines
                string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Create a paragraph for each line
                foreach (string line in lines)
                {
                    if (line.StartsWith("===== ") && line.EndsWith(" ====="))
                    {
                        // Create a Heading 1 paragraph for the assignment name
                        string assignmentName = line.Substring(6, line.Length - 12);
                        Paragraph headingPara = body.AppendChild(new Paragraph());
                        Run headingRun = headingPara.AppendChild(new Run());
                        headingRun.AppendChild(new Text(assignmentName));

                        // Apply Heading 1 style
                        ParagraphProperties headingParaProps = headingPara.AppendChild(new ParagraphProperties());
                        headingParaProps.ParagraphStyleId = new ParagraphStyleId() { Val = "Heading1" };
                    }
                    else
                    {
                        // Create a paragraph for the normal line
                        Paragraph para = body.AppendChild(new Paragraph());
                        Run run = para.AppendChild(new Run());
                        run.AppendChild(new Text(line));
                    }
                }

                mainPart.Document.Save();
            }
        }
        private void AddStyleDefinitionsPart(MainDocumentPart mainPart)
        {
            StyleDefinitionsPart styleDefinitionsPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            Styles styles = new Styles();

            // Create the built-in Heading 1 style
            Style heading1Style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1",
                CustomStyle = true
            };

            // Set the Heading 1 style properties
            StyleRunProperties heading1RunProps = new StyleRunProperties();
            RunFonts runFonts = new RunFonts { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" };
            heading1RunProps.Append(runFonts);
            heading1RunProps.Append(new Bold());
            heading1RunProps.Append(new FontSize() { Val = "28" }); // 14pt font size
            heading1RunProps.Append(new Color { Val = "365F91" }); // Set a custom color
            heading1Style.Append(heading1RunProps);

            styles.Append(heading1Style);

            // Add the styles to the style definitions part
            styleDefinitionsPart.Styles = styles;
        }
    }
}
