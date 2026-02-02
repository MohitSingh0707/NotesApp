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
                return;

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

            await FirebaseMessaging
                .DefaultInstance
                .SendMulticastAsync(message);
        }
    }
}
