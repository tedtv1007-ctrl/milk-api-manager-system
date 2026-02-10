using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly KeycloakService _keycloakService;

        public AuthController(KeycloakService keycloakService)
        {
            _keycloakService = keycloakService;
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("user")]
        public IActionResult GetUser()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new
                {
                    IsAuthenticated = true,
                    Name = User.Identity.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                });
            }
            return Ok(new { IsAuthenticated = false });
        }

        [HttpPost("setup")]
        public async Task<IActionResult> SetupKeycloak()
        {
            try
            {
                // Create realm
                await _keycloakService.CreateRealmAsync("milk-api-manager");

                // Create client
                await _keycloakService.CreateClientAsync("milk-api-manager", "milk-api-manager-client", "client-secret");

                return Ok(new { message = "Keycloak setup completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to setup Keycloak", details = ex.Message });
            }
        }
    }
}