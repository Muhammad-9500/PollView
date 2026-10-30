using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.DTOs;
using PollViews.Services;

namespace PollViews.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminAuthController(PollViewsDbContext db, AdminPasswordService passwords) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var admin = await db.AdminUsers.FirstOrDefaultAsync(user => user.Username == username && user.IsActive, cancellationToken);
        if (admin is null || !passwords.Verify(request.Password, admin.PasswordHash))
            return Unauthorized(new { message = "Invalid admin credentials." });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

        return Ok(new { username = admin.Username, mustChangePassword = admin.MustChangePassword });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { username = User.Identity?.Name, authenticated = true });

    [Authorize(Roles = "Admin")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { authenticated = false });
    }
}
