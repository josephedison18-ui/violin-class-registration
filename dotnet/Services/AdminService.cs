using Microsoft.EntityFrameworkCore;
using ViolinClassAPI.Data;
using ViolinClassAPI.Models;

namespace ViolinClassAPI.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminService> _logger;

        public AdminService(ApplicationDbContext context, ILogger<AdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminUser> AuthenticateAsync(string username, string password)
        {
            try
            {
                var admin = await _context.AdminUsers
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (admin == null || !VerifyPassword(password, admin.Password))
                {
                    _logger.LogWarning($"Invalid login attempt for username: {username}");
                    return null;
                }

                _logger.LogInformation($"Admin logged in: {username}");
                return admin;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Authentication error: {ex.Message}");
                return null;
            }
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsResponse
            {
                TotalRegistrations = await _context.Registrations.CountAsync(),
                OnlineClasses = await _context.Registrations
                    .Where(r => r.ClassMode == "online")
                    .CountAsync(),
                OfflineClasses = await _context.Registrations
                    .Where(r => r.ClassMode == "offline")
                    .CountAsync(),
                PendingPayments = await _context.Registrations
                    .Where(r => r.PaymentStatus == "pending")
                    .CountAsync(),
                TotalPageVisits = await _context.PageVisitors.CountAsync()
            };

            return stats;
        }

        public async Task<IEnumerable<Registration>> SearchRegistrationsByPhoneAsync(string phone)
        {
            return await _context.Registrations
                .Where(r => r.Phone.Contains(phone))
                .ToListAsync();
        }

        public async Task<IEnumerable<Registration>> SearchRegistrationsByEmailAsync(string email)
        {
            return await _context.Registrations
                .Where(r => r.Email.Contains(email))
                .ToListAsync();
        }

        public async Task<IEnumerable<Registration>> SearchRegistrationsByNameAsync(string name)
        {
            return await _context.Registrations
                .Where(r => r.Name.Contains(name))
                .ToListAsync();
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            // Use BCrypt for production
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }
    }

    public class DashboardStatsResponse
    {
        public int TotalRegistrations { get; set; }
        public int OnlineClasses { get; set; }
        public int OfflineClasses { get; set; }
        public int PendingPayments { get; set; }
        public int TotalPageVisits { get; set; }
    }
}
