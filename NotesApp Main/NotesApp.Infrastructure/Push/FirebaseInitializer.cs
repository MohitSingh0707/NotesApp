using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace NotesApp.Infrastructure.Push
{
    public static class FirebaseInitializer
    {
        public static void Initialize(IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance != null)
                return;

            var firebaseConfigPath = configuration["Firebase:ServiceAccountPath"];

            if (string.IsNullOrWhiteSpace(firebaseConfigPath))
            {
                // throw new FileNotFoundException("Firebase service account path not configured.");
                System.Console.WriteLine("⚠️ Firebase service account path not configured. Push notifications will be disabled.");
                return;
            }

            using var stream = new FileStream(
                firebaseConfigPath,
                FileMode.Open,
                FileAccess.Read);

            var credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential
            });
        }
    }
}
