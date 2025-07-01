using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.DTOs;
using Mind_Mend.Models;
using Mind_Mend.Models.Users;

namespace Mind_Mend.Controllers
{
    public class AdminController : ControllerBase
    {

        private readonly MindMendDbContext _context;
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id.ToString());
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRoleDto userRoleDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Only allow role change if current role is Patient
            if (user.Role != Roles.Patient)
            {
                return BadRequest("Role change is only allowed from 'Patient'.");
            }
            // TODO: 
            // Only allow changing to Doctor or Therapist
            if (userRoleDto.Role.RoleId != Roles.Doctor && userRoleDto.Role.RoleId != Roles.Therapist)
            {
                return BadRequest("Invalid role. Only 'Doctor' or 'Therapist' are allowed.");
            }

            user.Role = userRoleDto.Role.RoleId;
            _context.Entry(user).Property(u => u.Role).IsModified = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
