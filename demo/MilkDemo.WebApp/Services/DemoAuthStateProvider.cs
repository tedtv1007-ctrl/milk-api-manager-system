using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace MilkDemo.WebApp.Services;

public class DemoAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;

    public DemoAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _authService.GetCurrentUser();
        if (user == null || string.IsNullOrEmpty(_authService.GetToken()))
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.DisplayName),
        };
        foreach (var role in user.Roles)
            claims.Add(new(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
