using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Classroom.v1;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace TLH.IntegrationServices
{
    public static class GoogleApiService
    {
        public static UserCredential? Credential { get; private set; }

        private static readonly string[] scopes = {
            ClassroomService.Scope.ClassroomCoursesReadonly,
            ClassroomService.Scope.ClassroomCourseworkMe,
            ClassroomService.Scope.ClassroomCourseworkStudents,
            ClassroomService.Scope.ClassroomRosters,
            ClassroomService.Scope.ClassroomProfileEmails,
            ClassroomService.Scope.ClassroomAnnouncementsReadonly,
            ClassroomService.Scope.ClassroomCourseworkStudentsReadonly,
            Google.Apis.Drive.v3.DriveService.Scope.Drive,
        };

        public static ClassroomService ClassroomService { get; private set; } = new ClassroomService(new BaseClientService.Initializer());
        public static Google.Apis.Drive.v3.DriveService DriveService { get; private set; } = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer());

        /// <summary>
        /// Initializes the Google Classroom and Drive services using the credentials stored in credentials.json and token.json.
        /// </summary>
        public static void InitializeGoogleServices()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
                Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // initialize Classroom and Drive services
            ClassroomService = new ClassroomService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = $"{nameof(InitializeGoogleServices)} - {nameof(ClassroomService)}"
            });

            DriveService = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = $"{nameof(InitializeGoogleServices)} - {nameof(DriveService)}"
            });
        }

        public static void RefreshAccessToken(UserCredential credential)
        {
            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var newCredential = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets
                }),
                "user",
                new TokenResponse { RefreshToken = credential.Token.RefreshToken }
            );
            // Revoke access token of old credential to prevent unauthorized access
            var revokeTask = newCredential.RevokeTokenAsync(CancellationToken.None);
            revokeTask.Wait();
            if (revokeTask.Result)
            {
                // Assign new credential to class property
                Credential = newCredential;

                // Create new Classroom and Drive services with refreshed credential
                ClassroomService = new ClassroomService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = "TeachersLittleHelper"
                });
                DriveService = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = "TeachersLittleHelper"
                });
            }
            else
            {
                Console.WriteLine("Failed to revoke access token. Please log in with your Google account.");
            }
        }
    }
}