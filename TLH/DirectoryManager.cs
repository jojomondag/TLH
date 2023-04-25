using Google.Apis.Classroom.v1.Data;
using System.Text.RegularExpressions;
using TLH;

//This class Creates all folders on the Local Computer for a Teachers classroom, courses, assignments and students.
public class DirectoryManager
{
    public const int MaxPathLength = 260;
    public const int MinFileNameLength = 5;

    public static string ShortenPath(string path, int maxLength = MaxPathLength)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var fileName = Path.GetFileName(path);
        var directory = Path.GetDirectoryName(path);

        if (directory == null)
        {
            throw new InvalidOperationException("The directory in the provided path is null.");
        }

        if (path.Length <= maxLength)
        {
            return path;
        }

        int allowedLengthForName = maxLength - directory.Length - 1; // -1 for the path separator

        if (allowedLengthForName < MinFileNameLength)
        {
            throw new InvalidOperationException($"Path is too long, and shortening would result in a file name with less than {MinFileNameLength} characters.");
        }

        var shortenedFileName = ShortenFileName(fileName, allowedLengthForName);
        return Path.Combine(directory, shortenedFileName);
    }
    public static string ShortenFileName(string fileName, int allowedLength)
    {
        if (fileName.Length <= allowedLength)
        {
            return fileName;
        }

        var extension = Path.GetExtension(fileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var shortenedFileNameWithoutExtension = fileNameWithoutExtension.Substring(0, allowedLength - extension.Length);
        return $"{shortenedFileNameWithoutExtension}{extension}";
    }
    private static (string, bool) CreateDirectory(string parentDirectory, string folderName)
    {
        var directoryPath = Path.Combine(parentDirectory, SanitizeFolderName(folderName));

        bool isNewlyCreated = !Directory.Exists(directoryPath);
        Directory.CreateDirectory(directoryPath);

        return (directoryPath, isNewlyCreated);
    }
    public static string CreateUserDirectoryOnDesktop()
    {
        Program.userPathLocation = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var (userDirectory, _) = CreateDirectory(Program.userPathLocation, Environment.UserName);
        return userDirectory;
    }
    public static string CreateCourseDirectory(string courseId)
    {
        if (Program.userPathLocation == null)
        {
            // Handle the null case, for example, by returning an empty string
            return string.Empty;
        }

        var course = ClassroomApiHelper.GetCourse(courseId).Result;
        var courseName = DirectoryManager.SanitizeFolderName(course.Name);
        var courseDirectory = Path.Combine(Program.userPathLocation, $"{DirectoryManager.SanitizeFolderName(courseName)}_{courseId}");
        Directory.CreateDirectory(courseDirectory);

        return courseDirectory;
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