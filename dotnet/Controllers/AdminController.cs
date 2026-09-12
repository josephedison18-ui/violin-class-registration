using Microsoft.AspNetCore.Mvc;
using ViolinClassAPI.Services;

namespace ViolinClassAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // Admin Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            try
            {
                var admin = await _adminService.AuthenticateAsync(request.Username, request.Password);

                if (admin == null)
                {
                    return Unauthorized(new { success = false, error = "Invalid credentials" });
                }

                // Generate JWT token or session
                string sessionId = Guid.NewGuid().ToString();

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    sessionId = sessionId,
                    admin = new
                    {
                        username = admin.Username,
                        email = admin.Email,
                        phone = admin.Phone,
                        whatsapp = admin.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error" });
            }
        }

        // Get Dashboard Stats
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();

                return Ok(new
                {
                    success = true,
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dashboard error: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error" });
            }
        }

        // Search Registrations by Phone
        [HttpGet("search/phone")]
        public async Task<IActionResult> SearchByPhone([FromQuery] string phone)
        {
            try
            {
                var registrations = await _adminService.SearchRegistrationsByPhoneAsync(phone);

                return Ok(new
                {
                    success = true,
                    count = registrations.Count(),
                    data = registrations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Search error: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error" });
            }
        }

        // Search Registrations by Email
        [HttpGet("search/email")]
        public async Task<IActionResult> SearchByEmail([FromQuery] string email)
        {
            try
            {
                var registrations = await _adminService.SearchRegistrationsByEmailAsync(email);

                return Ok(new
                {
                    success = true,
                    count = registrations.Count(),
                    data = registrations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Search error: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error" });
            }
        }

        // Search Registrations by Name
        [HttpGet("search/name")]
        public async Task<IActionResult> SearchByName([FromQuery] string name)
        {
            try
            {
                var registrations = await _adminService.SearchRegistrationsByNameAsync(name);

                return Ok(new
                {
                    success = true,
                    count = registrations.Count(),
                    data = registrations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Search error: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error" });
            }
        }
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
