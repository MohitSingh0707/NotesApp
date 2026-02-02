using NotesApp.Application.Interfaces.Emails;
using NotesApp.Application.Interfaces.Notifications;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace NotesApp.Infrastructure.BackgroundJobs
{
    public class ReminderJob
    {
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IUserRepository _userRepository;

        public ReminderJob(
            IEmailService emailService,
            INotificationService notificationService,
            IPushNotificationService pushNotificationService,
            IUserRepository userRepository)
        {
            _emailService = emailService;
            _notificationService = notificationService;
            _pushNotificationService = pushNotificationService;
            _userRepository = userRepository;
        }

        // ✅ SINGLE, CORRECT METHOD
        public async Task SendReminderAsync(
            Guid userId,
            string email,
            Guid noteId,
            string noteTitle,
            ReminderType type)
        {
            // SYSTEM / LOCAL TIME (email display only)
            var now = DateTime.Now;

            // 1️⃣ IN-APP NOTIFICATION (DB)
            if (type.HasFlag(ReminderType.InApp))
            {
                try
                {
                    await _notificationService.CreateAsync(
                        userId,
                        "Reminder ⏰",
                        $"You have a reminder for one of your notes: {noteTitle}",
                        "Reminder",
                        noteId,
                        noteTitle
                    );
                }
                catch (Exception ex)
                {
                    // ❌ In-app notification fail ho sakta hai, job nahi rukni chahiye
                    Console.WriteLine("In-app notification failed: " + ex.Message);
                }
            }

            // 2️⃣ PUSH NOTIFICATION (APP CLOSED ALSO)
            if (type.HasFlag(ReminderType.Push))
            {
                try
                {
                    await _pushNotificationService.SendAsync(
                        userId,
                        "Reminder ⏰",
                        $"Reminder for note: {noteTitle}"
                    );
                }
                catch (Exception ex)
                {
                    // ❌ Firebase / Push fail ho sakta hai
                    Console.WriteLine("Push notification failed: " + ex.Message);
                }
            }

            // 3️⃣ EMAIL (🔥 Premium Industry Template)
            if (type.HasFlag(ReminderType.Email))
            {
                try
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    var name = user?.FirstName ?? "there";

                    var emailBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reminder - NotesApp</title>
</head>
<body style='margin:0; padding:0; background-color:#f8fafc; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f8fafc; padding:40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:24px; overflow:hidden; box-shadow:0 15px 35px rgba(0,0,0,0.05); border: 1px solid #e2e8f0;'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); padding:40px; text-align:center;'>
                            <div style='background:rgba(255,255,255,0.2); width:60px; height:60px; border-radius:18px; line-height:60px; margin:0 auto 20px; font-size:30px;'>⏰</div>
                            <h1 style='margin:0; color:#ffffff; font-size:28px; font-weight:800; letter-spacing:-0.5px;'>NotesApp Reminder</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:45px; color:#1e293b;'>
                            <h2 style='margin:0 0 15px; font-size:20px; font-weight:700; color:#0f172a;'>Hi {name} 👋,</h2>
                            <p style='margin:0 0 25px; font-size:16px; line-height:1.6; color:#475569;'>
                                This is a friendly nudge for the reminder you set in your notes.
                            </p>
                            
                            <div style='background:#f1f5f9; border-left:4px solid #4f46e5; border-radius:8px; padding:25px; margin-bottom:30px;'>
                                <p style='margin:0 0 10px; font-size:13px; font-weight:700; color:#6366f1; text-transform:uppercase; letter-spacing:1px;'>Note Title</p>
                                <p style='margin:0; font-size:18px; font-weight:600; color:#0f172a;'>""{noteTitle}""</p>
                                <div style='margin-top:20px; padding-top:15px; border-top:1px solid #e2e8f0;'>
                                    <p style='margin:0; font-size:14px; color:#64748b;'>
                                        📅 <strong>Scheduled for:</strong><br />
                                        {now:dddd, dd MMM yyyy • hh:mm tt}
                                    </p>
                                </div>
                            </div>

                            <p style='margin:0; font-size:15px; line-height:1.6; color:#475569;'>
                                Stay organized and keep smashing your goals! 🚀
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background:#f8fafc; padding:30px; text-align:center; border-top:1px solid #f1f5f9;'>
                            <p style='margin:0; font-size:12px; color:#94a3b8; line-height:1.5;'>
                                &copy; {now.Year} NotesApp. All rights reserved.<br/>
                                You received this because you set a reminder on your NotesApp account.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                    await _emailService.SendAsync(
                        email,
                        "⏰ Reminder from NotesApp",
                        emailBody
                    );
                }
                catch (Exception ex)
                {
                    // ❌ Agar email bhi fail ho jaye, to log milna chahiye
                    Console.WriteLine("Email sending failed: " + ex.Message);
                }
            }
        }
    }
}
