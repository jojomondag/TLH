using Google.Apis.Classroom.v1.Data;
using System.Text.RegularExpressions;
using TLH;
public class DirectoryManager
{
    public static string CreateDirectory(string parentDirectory, string folderName)
    {
        var sanitizedFolderName = SanitizeFolderName(folderName);
        var directoryPath = Path.Combine(parentDirectory, sanitizedFolderName);
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }
    public static string CreateStudentDirectoryOnDesktop()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var userDirectory = CreateDirectory(desktopPath, Environment.UserName);
        return userDirectory;
    }
    public static string GetCourseName(string courseId)
    {
        var course = GoogleApiHelper.ClassroomService.Courses.Get(courseId).Execute();
        return SanitizeFolderName(course.Name);
    }
    public static string CreateStudentDirectory(string courseDirectory, Student student)
    {
        var studentName = SanitizeFolderName(student.Profile.Name.FullName);
        var studentDirectory = CreateDirectory(courseDirectory, studentName);
        Console.WriteLine($"Created directory for student: {studentName}");
        return studentDirectory;
    }
    public static string CreateAssignmentDirectory(string studentDirectory, CourseWork courseWork)
    {
        var assignmentName = SanitizeFolderName(courseWork.Title);
        var assignmentDirectory = CreateDirectory(studentDirectory, assignmentName);
        Console.WriteLine($"Created directory for assignment: {assignmentName}");
        return assignmentDirectory;
    }
    public static void DeleteEmptyFolders(string directory)
    {
        foreach (var dir in Directory.GetDirectories(directory))
        {
            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0 && dir != directory)
            {
                Directory.Delete(dir);
            }
            else
            {
                DeleteEmptyFolders(dir);
            }
        }
    }
    public static string SanitizeFolderName(string folderName)
    {
        var invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var regex = new Regex($"[{Regex.Escape(invalidChars)}]");
        return regex.Replace(folderName, "_");
    }
}