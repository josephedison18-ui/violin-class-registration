using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ViolinClassAPI.Services
{
    public class SmsNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsNotificationService> _logger;

        public SmsNotificationService(IConfiguration configuration, ILogger<SmsNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Initialize Twilio
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];

            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
            {
                TwilioClient.Init(accountSid, authToken);
            }
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string messageBody)
        {
            try
            {
                // Format phone number (ensure it starts with country code)
                string formattedPhone = FormatPhoneNumber(toPhoneNumber);

                var fromPhoneNumber = _configuration["Twilio:PhoneNumber"];

                // Send SMS using Twilio
                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber(fromPhoneNumber),
                    to: new PhoneNumber(formattedPhone)
                );

                _logger.LogInformation($"SMS sent successfully to {formattedPhone}. SID: {message.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending SMS to {toPhoneNumber}: {ex.Message}");
                return false;
            }
        }

        private string FormatPhoneNumber(string phone)
        {
            // Remove all non-digit characters
            string cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");

            // If it's an Indian number (10 digits), add country code
            if (cleanPhone.Length == 10)
            {
                return "+91" + cleanPhone;
            }

            // If it already has country code, add + if not present
            if (!phone.StartsWith("+"))
            {
                return "+" + cleanPhone;
            }

            return phone;
        }
    }
}
