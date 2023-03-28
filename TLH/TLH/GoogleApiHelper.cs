using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Classroom.v1;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace TLH
{
    public static class GoogleApiHelper
    {
        private static readonly string[] scopes = {
            ClassroomService.Scope.ClassroomCoursesReadonly,
            ClassroomService.Scope.ClassroomCourseworkMe,
            ClassroomService.Scope.ClassroomCourseworkStudents,
            ClassroomService.Scope.ClassroomRosters,
            ClassroomService.Scope.ClassroomProfileEmails,
            ClassroomService.Scope.ClassroomAnnouncementsReadonly,
            DriveService.Scope.Drive,
        };
        public static ClassroomService ClassroomService { get; private set; }
        public static DriveService DriveService { get; private set; }
        public static void InitializeGoogleServices()
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            if (credential == null)
            {
                Console.WriteLine("Failed to authenticate. Please log in with your Google account.");
                return;
            }

            // initialize Classroom and Drive services
            ClassroomService = new ClassroomService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationNameHere"
            });

            DriveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationNameHere"
            });
        }
        public static void RefreshAccessToken(UserCredential credential)
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
                credential = new UserCredential(new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = clientSecrets
                    }),
                    "user",
                    new TokenResponse { RefreshToken = credential.Token.RefreshToken }
                );
            }

            if (credential == null)
            {
                Console.WriteLine("Failed to refresh access token. Please log in with your Google account.");
                return;
            }

            // create new Classroom and Drive services with refreshed credential
            var newClassroomService = new ClassroomService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationNameHere"
            });

            var newDriveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationNameHere"
            });

            // assign new services to existing objects
            ClassroomService = newClassroomService;
            DriveService = newDriveService;
        }
    }
}