using FirebaseAdmin.Messaging;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Infrastructure.Push;
using System;
using System.Linq;
using System.Threading.Tasks;
using NotesApp.Application.Interfaces.Common;

namespace NotesApp.Infrastructure.Push
{
    public class FcmPushNotificationService : IPushNotificationService
    {
        private readonly IDeviceTokenRepository _deviceTokenRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public FcmPushNotificationService(
            IDeviceTokenRepository deviceTokenRepository,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _deviceTokenRepository = deviceTokenRepository;
            _configuration = configuration;

            // 🔥 Firebase init once
            FirebaseInitializer.Initialize(_configuration);
        }

        public async Task SendAsync(Guid userId, string title, string body)
        {
            var tokens = await _deviceTokenRepository
                .GetByUserAsync(userId);

            if (!tokens.Any())
            {
                Console.WriteLine($"📱 FCM: No tokens found for user {userId}. Skipping push.");
                return;
            }

            Console.WriteLine($"📱 FCM: Found {tokens.Count()} tokens for user {userId}. Sending multicast...");

            var message = new MulticastMessage
            {
                Tokens = tokens.Select(t => t.Token).ToList(),

                // ✅ EXPLICIT FIREBASE NOTIFICATION
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                }
            };

            try
            {
                Console.WriteLine($"🚀 FCM: Attempting multicast send to {message.Tokens.Count} tokens...");
                
                // 🔥 Switch from SendMulticastAsync (deprecated Batch API) to SendEachForMulticastAsync
                var response = await FirebaseMessaging
                    .DefaultInstance
                    .SendEachForMulticastAsync(message);
                
                Console.WriteLine($"✅ FCM: Multicast send completed.");
                Console.WriteLine($"📊 Result -> Success: {response.SuccessCount}, Failures: {response.FailureCount}");
                
                if (response.FailureCount > 0)
                {
                    for (var i = 0; i < response.Responses.Count; i++)
                    {
                        var res = response.Responses[i];
                        if (!res.IsSuccess)
                        {
                            // Log the failed token index or token if needed, and the exception
                            var token = message.Tokens[i];
                            Console.WriteLine($"⚠️ FCM Individual Failure (Token: {token.Substring(0, 10)}...): {res.Exception?.Message ?? "Unknown error"} (Code: {res.Exception?.MessagingErrorCode})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FCM: CRITICAL failure during multicast send: {ex.Message}");
                Console.WriteLine($"📉 StackTrace: {ex.StackTrace}");
            }
        }
    }
}
