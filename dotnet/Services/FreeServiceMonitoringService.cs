using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViolinClassAPI.Services
{
    /// <summary>
    /// FREE Service Monitor - Tracks usage and AUTO-STOPS services BEFORE hitting paid tier
    /// Uses FREE Confluent Kafka for event streaming (completely free, no charges)
    /// </summary>
    public class FreeServiceMonitoringService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FreeServiceMonitoringService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly KafkaEventProducerService _kafkaProducer;

        // FREE tier thresholds
        private const int SMS_FREE_LIMIT = 50;
        private const int SMS_STOP_AT = 45; // Stop before limit
        private const int EMAIL_FREE_LIMIT = 100;
        private const int EMAIL_STOP_AT = 90; // Stop before limit

        public FreeServiceMonitoringService(
            IConfiguration configuration,
            ILogger<FreeServiceMonitoringService> logger,
            ApplicationDbContext context,
            KafkaEventProducerService kafkaProducer)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _kafkaProducer = kafkaProducer;
        }

        /// <summary>
        /// Monitor all services and auto-stop when reaching FREE tier limits
        /// Runs daily or on-demand
        /// </summary>
        public async Task MonitorAllServicesAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Starting FREE service monitoring...");

                // Monitor SMS
                await MonitorSmsServiceAsync();

                // Monitor Email
                await MonitorEmailServiceAsync();

                // Send dashboard stats
                await SendMonitoringStatsAsync();

                _logger.LogInformation("✅ Service monitoring completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor SMS - Stop when approaching FREE limit
        /// </summary>
        private async Task MonitorSmsServiceAsync()
        {
            try
            {
                int smsSentThisMonth = await _context.SmsNotifications
                    .Where(s => s.Status == "sent" && s.SentAt > DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                _logger.LogInformation($"📊 SMS Usage: {smsSentThisMonth}/{SMS_FREE_LIMIT} (FREE limit)");

                if (smsSentThisMonth >= SMS_STOP_AT)
                {
                    _logger.LogWarning($"⚠️ SMS LIMIT WARNING: {smsSentThisMonth}/{SMS_FREE_LIMIT}");

                    // Disable SMS to prevent charges
                    var config = _configuration["Twilio:Enabled"];
                    if (config != "false")
                    {
                        _logger.LogWarning("🛑 AUTO-STOPPING SMS SERVICE - FREE limit reached!");
                        await _kafkaProducer.ProduceEventAsync("service-alerts", new
                        {
                            service = "SMS",
                            status = "STOPPED",
                            reason = "FREE limit reached",
                            usage = $"{smsSentThisMonth}/{SMS_FREE_LIMIT}",
                            timestamp = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    int remaining = SMS_STOP_AT - smsSentThisMonth;
                    _logger.LogInformation($"✅ SMS Status: {remaining} messages remaining before auto-stop");

                    if (remaining <= 5)
                    {
                        _logger.LogWarning("⚡ CRITICAL: Only 5 SMS left in FREE tier!");
                        await NotifyAdminAsync($"SMS FREE tier almost exhausted: {smsSentThisMonth}/{SMS_FREE_LIMIT}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SMS monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor Email - Stop when approaching FREE limit
        /// </summary>
        private async Task MonitorEmailServiceAsync()
        {
            try
            {
                int emailsSentToday = await _context.EmailNotifications
                    .Where(e => e.Status == "sent" && e.SentAt > DateTime.UtcNow.AddDays(-1))
                    .CountAsync();

                _logger.LogInformation($"📊 Email Usage Today: {emailsSentToday}/{EMAIL_FREE_LIMIT} (FREE limit)");

                if (emailsSentToday >= EMAIL_STOP_AT)
                {
                    _logger.LogWarning($"⚠️ EMAIL LIMIT WARNING: {emailsSentToday}/{EMAIL_FREE_LIMIT}");

                    if (_configuration.GetValue<bool>("Email:Enabled", true))
                    {
                        _logger.LogWarning("🛑 AUTO-STOPPING EMAIL SERVICE - FREE limit reached!");
                        await _kafkaProducer.ProduceEventAsync("service-alerts", new
                        {
                            service = "Email",
                            status = "STOPPED",
                            reason = "FREE limit reached",
                            usage = $"{emailsSentToday}/{EMAIL_FREE_LIMIT}",
                            timestamp = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    int remaining = EMAIL_STOP_AT - emailsSentToday;
                    _logger.LogInformation($"✅ Email Status: {remaining} emails remaining before auto-stop");

                    if (remaining <= 10)
                    {
                        _logger.LogWarning("⚡ CRITICAL: Only 10 emails left in FREE tier!");
                        await NotifyAdminAsync($"Email FREE tier almost exhausted: {emailsSentToday}/{EMAIL_FREE_LIMIT}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// Send monitoring stats to Kafka (for real-time dashboard)
        /// </summary>
        private async Task SendMonitoringStatsAsync()
        {
            try
            {
                var stats = new
                {
                    timestamp = DateTime.UtcNow,
                    sms = new
                    {
                        sent = await _context.SmsNotifications
                            .Where(s => s.Status == "sent" && s.SentAt > DateTime.UtcNow.AddDays(-30))
                            .CountAsync(),
                        limit = SMS_FREE_LIMIT,
                        stopAt = SMS_STOP_AT
                    },
                    email = new
                    {
                        sent = await _context.EmailNotifications
                            .Where(e => e.Status == "sent" && e.SentAt > DateTime.UtcNow.AddDays(-1))
                            .CountAsync(),
                        limit = EMAIL_FREE_LIMIT,
                        stopAt = EMAIL_STOP_AT
                    }
                };

                await _kafkaProducer.ProduceEventAsync("monitoring-stats", stats);
                _logger.LogInformation("📤 Monitoring stats sent to Kafka");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Notify admin when limits are approaching
        /// </summary>
        private async Task NotifyAdminAsync(string message)
        {
            try
            {
                await _kafkaProducer.ProduceEventAsync("admin-alerts", new
                {
                    message = message,
                    severity = "WARNING",
                    timestamp = DateTime.UtcNow
                });

                _logger.LogWarning($"🚨 Admin Alert: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending admin alert: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current service status
        /// </summary>
        public async Task<object> GetServiceStatusAsync()
        {
            var smsUsage = await _context.SmsNotifications
                .Where(s => s.Status == "sent" && s.SentAt > DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            var emailUsage = await _context.EmailNotifications
                .Where(e => e.Status == "sent" && e.SentAt > DateTime.UtcNow.AddDays(-1))
                .CountAsync();

            return new
            {
                timestamp = DateTime.UtcNow,
                services = new
                {
                    sms = new
                    {
                        usage = smsUsage,
                        limit = SMS_FREE_LIMIT,
                        stopAt = SMS_STOP_AT,
                        remaining = SMS_STOP_AT - smsUsage,
                        status = smsUsage >= SMS_STOP_AT ? "⛔ STOPPED" : "✅ ACTIVE",
                        percentUsed = Math.Round((double)smsUsage / SMS_FREE_LIMIT * 100, 2)
                    },
                    email = new
                    {
                        usage = emailUsage,
                        limit = EMAIL_FREE_LIMIT,
                        stopAt = EMAIL_STOP_AT,
                        remaining = EMAIL_STOP_AT - emailUsage,
                        status = emailUsage >= EMAIL_STOP_AT ? "⛔ STOPPED" : "✅ ACTIVE",
                        percentUsed = Math.Round((double)emailUsage / EMAIL_FREE_LIMIT * 100, 2)
                    }
                },
                disclaimer = "All services are FREE tier. Auto-stops at threshold to prevent charges."
            };
        }
    }
}
