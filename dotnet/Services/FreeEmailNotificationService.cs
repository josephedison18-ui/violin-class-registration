using SendGrid;
using SendGrid.Helpers.Mail;

namespace ViolinClassAPI.Services
{
    public class FreeEmailNotificationService
    {
        private readonly ILogger<FreeEmailNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        // FREE tier: SendGrid = 100 emails/day (completely free, no charges)
        // Fallback: Database logging (zero cost)
        private const int FREE_EMAIL_LIMIT = 100; // Per day

        public FreeEmailNotificationService(
            IConfiguration configuration, 
            ILogger<FreeEmailNotificationService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Send email ONLY if:
        /// 1. Email service is EXPLICITLY ENABLED by admin
        /// 2. Free tier limit not exceeded (100/day)
        /// 3. SendGrid API key is configured
        /// Otherwise: Save to database, NO CHARGES INCURRED
        /// </summary>
        public async Task<bool> SendThankYouEmailAsync(string toEmail, string studentName, string registrationId, string phone)
        {
            try
            {
                // DEFAULT: Email disabled to avoid any charges
                bool emailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);
                
                if (!emailEnabled)
                {
                    _logger.LogInformation($"📧 EMAIL SERVICE DISABLED - Saving to database: {toEmail}");
                    await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_disabled", "thank_you");
                    return true; // Success - saved for later
                }

                // Check SendGrid API Key
                string sendGridKey = _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(sendGridKey))
                {
                    _logger.LogWarning($"❌ SendGrid not configured - Saving to database: {toEmail}");
                    await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_config", "thank_you");
                    return true; // Success - will send when configured
                }

                // Check free limit (100/day)
                int emailsSentToday = await _context.EmailNotifications
                    .Where(e => e.SentAt > DateTime.UtcNow.AddDays(-1) && e.Status == "sent")
                    .CountAsync();

                if (emailsSentToday >= FREE_EMAIL_LIMIT)
                {
                    _logger.LogWarning($"⚠️ FREE EMAIL LIMIT REACHED (100/day) - Saving: {toEmail}");
                    await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_limit", "thank_you");
                    return true; // Queued for tomorrow
                }

                // Send via SendGrid (FREE tier - no charges)
                await SendViasendGridAsync(toEmail, studentName, registrationId, phone, "thank_you");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email Error: {ex.Message}. Saving to database to avoid charges.");
                await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_error", "thank_you");
                return true; // Save to DB, don't crash
            }
        }

        /// <summary>
        /// Send admin notification email
        /// Only if enabled and configured - otherwise save to database
        /// </summary>
        public async Task<bool> SendAdminNotificationAsync(string studentName, string phone, string classMode, string city, string email)
        {
            try
            {
                bool emailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);
                
                if (!emailEnabled)
                {
                    _logger.LogInformation($"📧 EMAIL SERVICE DISABLED - Queuing admin notification");
                    await SaveEmailToDatabase(email ?? "admin@violinclasses.com", studentName, $"Registration: {studentName}", "pending_disabled", "admin_alert");
                    return true;
                }

                string sendGridKey = _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(sendGridKey))
                {
                    _logger.LogWarning("SendGrid not configured - Queuing admin notification");
                    await SaveEmailToDatabase(email ?? "admin@violinclasses.com", studentName, $"Registration: {studentName}", "pending_config", "admin_alert");
                    return true;
                }

                int emailsSentToday = await _context.EmailNotifications
                    .Where(e => e.SentAt > DateTime.UtcNow.AddDays(-1) && e.Status == "sent")
                    .CountAsync();

                if (emailsSentToday >= FREE_EMAIL_LIMIT)
                {
                    await SaveEmailToDatabase(email ?? "admin@violinclasses.com", studentName, $"Registration: {studentName}", "pending_limit", "admin_alert");
                    return true;
                }

                await SendViaendGridAdminAsync(studentName, phone, classMode, city);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Admin email error: {ex.Message}");
                await SaveEmailToDatabase(email ?? "admin@violinclasses.com", studentName, $"Registration: {studentName}", "pending_error", "admin_alert");
                return true;
            }
        }

        /// <summary>
        /// Save email to database - completely free, no charges
        /// Admin can manually send or enable service later
        /// </summary>
        private async Task SaveEmailToDatabase(string toEmail, string subject, string registrationId, string status, string type)
        {
            try
            {
                var emailLog = new EmailNotification
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    RegistrationId = registrationId,
                    Status = status, // pending_disabled, pending_config, pending_limit, sent, pending_error
                    EmailType = type, // thank_you, admin_alert
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EmailNotifications.Add(emailLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Email saved to database (0 cost): {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save email: {ex.Message}");
            }
        }

        /// <summary>
        /// Actually send email via SendGrid (only called if enabled)
        /// </summary>
        private async Task SendViaendGridAsync(string toEmail, string studentName, string registrationId, string phone, string type)
        {
            try
            {
                string sendGridKey = _configuration["SendGrid:ApiKey"];
                var client = new SendGridClient(sendGridKey);

                var from = new EmailAddress("noreply@violinclasses.com", "Violin Classes");
                var to = new EmailAddress(toEmail, studentName);
                var subject = $"🎻 Thank You for Registering - ID: {registrationId}";

                string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 25px; border-radius: 10px; text-align: center; margin-bottom: 20px; }}
        .content {{ color: #333; line-height: 1.6; }}
        .info-box {{ background: #f0f0f0; padding: 15px; border-left: 4px solid #667eea; margin: 20px 0; }}
        .contact {{ background: #f9f9f9; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ background: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎻 Welcome to Violin Classes!</h1>
        </div>
        
        <div class='content'>
            <p>Dear <strong>{studentName}</strong>,</p>
            
            <p>Thank you for registering with our Violin Classes! We're excited to teach you.</p>
            
            <div class='info-box'>
                <h3>Your Registration Details:</h3>
                <p><strong>Registration ID:</strong> {registrationId}</p>
                <p><strong>Phone:</strong> {phone}</p>
                <p><strong>Registration Date:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
            </div>
            
            <p>Our administrator will contact you within 24 hours with:</p>
            <ul>
                <li>Class schedule and timings</li>
                <li>Payment details</li>
                <li>Class location/WhatsApp group link</li>
            </ul>
            
            <div class='contact'>
                <h3>📞 Contact Information</h3>
                <p><strong>Name:</strong> L. Joseph Edison Rathinaraj</p>
                <p><strong>Phone:</strong> +91 9499035574</p>
                <p><strong>WhatsApp:</strong> +91 9499035574</p>
                <p><strong>Email:</strong> josephedison18@gmail.com</p>
                <p><strong>Location:</strong> Perambur, Chennai</p>
            </div>
            
            <p>If you have any questions, please feel free to contact us directly.</p>
            
            <p>Best regards,<br><strong>Violin Classes Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>&copy; 2024 Violin Classes. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this address.</p>
        </div>
    </div>
</body>
</html>
";

                var msg = new SendGridMessage()
                {
                    From = from,
                    Subject = subject,
                    HtmlContent = htmlContent
                };
                msg.AddTo(to);

                var response = await client.SendEmailAsync(msg);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    await SaveEmailToDatabase(toEmail, studentName, registrationId, "sent", type);
                    _logger.LogInformation($"✅ Email sent via SendGrid (FREE): {toEmail}");
                }
                else
                {
                    await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_error", type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendGrid error: {ex.Message}");
                await SaveEmailToDatabase(toEmail, studentName, registrationId, "pending_error", type);
            }
        }

        private async Task SendViaendGridAdminAsync(string studentName, string phone, string classMode, string city)
        {
            try
            {
                string sendGridKey = _configuration["SendGrid:ApiKey"];
                string adminEmail = _configuration["Admin:Email"];
                
                var client = new SendGridClient(sendGridKey);
                var from = new EmailAddress("noreply@violinclasses.com", "Violin Classes Admin");
                var to = new EmailAddress(adminEmail);
                var subject = $"🎻 New Registration: {studentName} from {city}";

                string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        table {{ border-collapse: collapse; width: 100%; margin: 15px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background: #667eea; color: white; }}
        tr:nth-child(even) {{ background: #f9f9f9; }}
    </style>
</head>
<body>
    <h2>✅ New Student Registration Alert</h2>
    <table>
        <tr><th>Field</th><th>Value</th></tr>
        <tr><td>Student Name</td><td>{studentName}</td></tr>
        <tr><td>Phone</td><td>{phone}</td></tr>
        <tr><td>Class Mode</td><td>{classMode}</td></tr>
        <tr><td>City</td><td>{city}</td></tr>
        <tr><td>Registration Time</td><td>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</td></tr>
    </table>
    <p><strong>⚡ Action Required:</strong> Contact student within 24 hours</p>
</body>
</html>
";

                var msg = new SendGridMessage()
                {
                    From = from,
                    Subject = subject,
                    HtmlContent = htmlContent
                };
                msg.AddTo(to);

                await client.SendEmailAsync(msg);
                _logger.LogInformation($"✅ Admin email sent (FREE): {adminEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Admin SendGrid error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all pending emails (not yet sent)
        /// Admin can view and manually send if needed
        /// </summary>
        public async Task<IEnumerable<object>> GetPendingEmailsAsync()
        {
            return await _context.EmailNotifications
                .Where(e => e.Status.StartsWith("pending"))
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.ToEmail,
                    e.Subject,
                    e.Status,
                    e.EmailType,
                    e.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Get email statistics
        /// </summary>
        public async Task<object> GetEmailStatsAsync()
        {
            var stats = new
            {
                FreeLimit = FREE_EMAIL_LIMIT,
                SentToday = await _context.EmailNotifications
                    .Where(e => e.Status == "sent" && e.SentAt > DateTime.UtcNow.AddDays(-1))
                    .CountAsync(),
                PendingDisabled = await _context.EmailNotifications
                    .Where(e => e.Status == "pending_disabled")
                    .CountAsync(),
                PendingConfig = await _context.EmailNotifications
                    .Where(e => e.Status == "pending_config")
                    .CountAsync(),
                PendingLimit = await _context.EmailNotifications
                    .Where(e => e.Status == "pending_limit")
                    .CountAsync(),
                Failed = await _context.EmailNotifications
                    .Where(e => e.Status == "pending_error")
                    .CountAsync()
            };
            return stats;
        }
    }

    /// <summary>
    /// Email Notification entity for logging
    /// </summary>
    public class EmailNotification
    {
        public int Id { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string RegistrationId { get; set; }
        public string Status { get; set; } // pending_disabled, pending_config, pending_limit, sent, pending_error
        public string EmailType { get; set; } // thank_you, admin_alert
        public DateTime SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
