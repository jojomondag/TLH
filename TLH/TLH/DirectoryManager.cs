using Google.Apis.Classroom.v1.Data;
using System.Text.RegularExpressions;
using TLH;
public class DirectoryManager
{
    public static (string, bool) CreateDirectory(string parentDirectory, string folderName)
    {
        var sanitizedFolderName = SanitizeFolderName(folderName);
        var directoryPath = Path.Combine(parentDirectory, sanitizedFolderName);

        bool isNewlyCreated = !Directory.Exists(directoryPath);
        Directory.CreateDirectory(directoryPath);

        return (directoryPath, isNewlyCreated);
    }
    public static string CreateStudentDirectoryOnDesktop()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var (userDirectory, _) = CreateDirectory(desktopPath, Environment.UserName);
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
        var (studentDirectory, isNewlyCreated) = CreateDirectory(courseDirectory, studentName);

        if (isNewlyCreated)
        {
            Console.WriteLine($"Created directory for student: {studentName}");
        }

        return studentDirectory;
    }
    public static string CreateAssignmentDirectory(string studentDirectory, CourseWork courseWork, string courseId)
    {
        var assignmentName = SanitizeFolderName(courseWork.Title);
        var assignmentDirectory = Path.Combine(studentDirectory, $"{assignmentName}");
        Directory.CreateDirectory(assignmentDirectory);

        return assignmentDirectory;
    }
    public static string SanitizeFolderName(string folderName)
    {
        var invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var regex = new Regex($"[{Regex.Escape(invalidChars)}]");
        return regex.Replace(folderName, "_");
    }
}