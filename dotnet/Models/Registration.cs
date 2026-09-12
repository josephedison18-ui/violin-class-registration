namespace ViolinClassAPI.Models
{
    public class Registration
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ClassMode { get; set; } // online or offline
        public string Location { get; set; } // e.g., "Perambur, Chennai" or "Online (WhatsApp)"
        public bool WhatsappOptIn { get; set; }
        public string Message { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PaymentStatus { get; set; } // pending, completed
        public string Status { get; set; } // active, inactive
        public string VisitorId { get; set; }
    }

    public class PageVisitor
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class AdminUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SmsNotification
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } // sent, pending, failed
        public string NotificationType { get; set; } // registration, payment, reminder
    }
}
