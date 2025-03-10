using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleMaintenanceTracker.Context;
using VehicleMaintenanceTracker.DTos;
using VehicleMaintenanceTracker.Modules;

namespace VehicleMaintenanceTracker.Controllers
{
    [Route("api/vehicles")]
    [ApiController]
    [Authorize] 
    public class VehicleController : ControllerBase
    {
        private readonly VMSDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public VehicleController(VMSDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor; 

        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] VehicleDto vehicleDto)
        {
            if (vehicleDto == null)
            {
                return BadRequest(new { message = "Invalid vehicle data." });
            }

            int userId = (int)GetUserIdFromToken();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authorized." });
            }

            var vehicle = new Vehicle
            {
                UserId = userId,
                VehicleType = vehicleDto.VehicleType,
                LicensePlateNumber = vehicleDto.LicensePlateNumber,
                RegistrationDate = vehicleDto.RegistrationDate,
                ManufactureYear = vehicleDto.ManufactureYear,
                Status = "Active" 
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Vehicle added successfully",
                vehicle = new
                {
                    id = vehicle.VehicleId,
                    type = vehicle.VehicleType,
                    licensePlate = vehicle.LicensePlateNumber
                }
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetUserVehicles()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid token" });

            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId.Value)
                .Select(v => new
                {
                    _id = v.VehicleId,
                    type = v.VehicleType,
                    licensePlate = v.LicensePlateNumber,
                    registrationDate = v.RegistrationDate,
                    year = v.ManufactureYear,
                    status = v.Status
                })
                .ToListAsync();

            return Ok(vehicles);
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return null; 
            }

            if (int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null; 
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetAllVehiclesForAdmin()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.User) 
                .Select(v => new
                {
                    _id = v.VehicleId,
                    type = v.VehicleType,
                    licensePlate = v.LicensePlateNumber,
                    registrationDate = v.RegistrationDate,
                    year = v.ManufactureYear,
                    status = v.Status,
                    owner = v.User.Username 
                })
                .ToListAsync();

            return Ok(vehicles);
        }

    }
}
    



