using MilkDemo.Shared.DTOs;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace MilkDemo.WebApp.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(string username, string password);
    Task LogoutAsync();
    string? GetToken();
    LoginResponseDto? GetCurrentUser();
    bool IsAuthenticated { get; }
    event Action? OnAuthStateChanged;
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private LoginResponseDto? _currentUser;
    private string? _token;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { Username = username, Password = password });

        if (!response.IsSuccessStatusCode)
            return null;

        _currentUser = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        _token = _currentUser?.Token;

        if (_token != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        OnAuthStateChanged?.Invoke();
        return _currentUser;
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        OnAuthStateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public string? GetToken() => _token;
    public LoginResponseDto? GetCurrentUser() => _currentUser;
}
