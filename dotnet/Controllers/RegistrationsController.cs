using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViolinClassAPI.Data;
using ViolinClassAPI.Models;
using ViolinClassAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViolinClassAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SmsNotificationService _smsService;
        private readonly ILogger<RegistrationsController> _logger;

        public RegistrationsController(
            ApplicationDbContext context, 
            SmsNotificationService smsService,
            ILogger<RegistrationsController> logger)
        {
            _context = context;
            _smsService = smsService;
            _logger = logger;
        }

        // GET: api/registrations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrations()
        {
            return await _context.Registrations.ToListAsync();
        }

        // GET: api/registrations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Registration>> GetRegistration(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);

            if (registration == null)
            {
                return NotFound(new { success = false, error = "Registration not found" });
            }

            return registration;
        }

        // POST: api/registrations
        [HttpPost]
        public async Task<ActionResult<RegistrationResponse>> PostRegistration(CreateRegistrationRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Phone))
                {
                    return BadRequest(new { success = false, error = "Name and phone are required" });
                }

                // Check if phone already registered
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.Phone == request.Phone);

                if (existingRegistration != null)
                {
                    return BadRequest(new { success = false, error = "Phone number already registered" });
                }

                var registration = new Registration
                {
                    Name = request.Name,
                    Age = request.Age,
                    Gender = request.Gender,
                    Email = request.Email,
                    Phone = request.Phone,
                    City = request.City,
                    Country = request.Country,
                    ClassMode = request.ClassMode, // online or offline
                    Location = request.ClassMode == "offline" ? "Perambur, Chennai" : "Online (WhatsApp)",
                    WhatsappOptIn = request.WhatsappOptIn,
                    Message = request.Message,
                    RegistrationDate = DateTime.UtcNow,
                    PaymentStatus = "pending",
                    Status = "active",
                    VisitorId = Guid.NewGuid().ToString()
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                // Send Thank You SMS to registering person
                string thankYouMessage = $"Thank you for registering with Violin Classes! Your Registration ID: {registration.Id}. Contact: L. Joseph Edison Rathinaraj - +91 9499035574. Payment Link: [GPay Link]";
                await _smsService.SendSmsAsync(registration.Phone, thankYouMessage);

                // Send Admin Notification
                string adminMessage = $"New Registration: {registration.Name} - Phone: {request.Phone} - Class Mode: {request.ClassMode}. Payment Status: Pending.";
                await _smsService.SendSmsAsync("+919499035574", adminMessage); // Admin phone

                _logger.LogInformation($"New registration created: {registration.Name}");

                return CreatedAtAction("GetRegistration", new { id = registration.Id },
                    new RegistrationResponse
                    {
                        Success = true,
                        Message = "Registration successful! Check WhatsApp for updates.",
                        RegistrationId = registration.Id,
                        Name = registration.Name,
                        ClassMode = registration.ClassMode,
                        Location = registration.Location,
                        GpayLink = "https://pay.google.com/qr/YOUR_QR_CODE"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Server error during registration" });
            }
        }

        // PUT: api/registrations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRegistration(int id, UpdateRegistrationRequest request)
        {
            var registration = await _context.Registrations.FindAsync(id);

            if (registration == null)
            {
                return NotFound(new { success = false, error = "Registration not found" });
            }

            registration.PaymentStatus = request.PaymentStatus ?? registration.PaymentStatus;
            registration.Status = request.Status ?? registration.Status;

            _context.Registrations.Update(registration);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Registration updated successfully", data = registration });
        }

        // DELETE: api/registrations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegistration(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);

            if (registration == null)
            {
                return NotFound(new { success = false, error = "Registration not found" });
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Registration deleted successfully" });
        }

        // GET: api/registrations/search
        [HttpGet("search/byphone")]
        public async Task<ActionResult<IEnumerable<Registration>>> SearchByPhone([FromQuery] string phone)
        {
            var registrations = await _context.Registrations
                .Where(r => r.Phone.Contains(phone))
                .ToListAsync();

            return Ok(new { success = true, count = registrations.Count, data = registrations });
        }

        // Track page visitors
        [HttpPost("track-visitor")]
        public async Task<IActionResult> TrackVisitor([FromBody] VisitorTrackRequest request)
        {
            var pageVisit = new PageVisitor
            {
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            _context.PageVisitors.Add(pageVisit);
            await _context.SaveChangesAsync();

            // Send notification to admin
            string visitMessage = $"New page visit from IP: {pageVisit.IpAddress}";
            await _smsService.SendSmsAsync("+919499035574", visitMessage);

            return Ok(new { success = true });
        }
    }

    // Request Models
    public class CreateRegistrationRequest
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ClassMode { get; set; } // online or offline
        public bool WhatsappOptIn { get; set; }
        public string Message { get; set; }
    }

    public class UpdateRegistrationRequest
    {
        public string PaymentStatus { get; set; }
        public string Status { get; set; }
    }

    public class VisitorTrackRequest
    {
        public string Source { get; set; }
    }

    // Response Model
    public class RegistrationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RegistrationId { get; set; }
        public string Name { get; set; }
        public string ClassMode { get; set; }
        public string Location { get; set; }
        public string GpayLink { get; set; }
    }
}
