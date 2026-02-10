using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<KeycloakService> _logger;
        private readonly string _keycloakUrl;
        private readonly string _adminUsername;
        private readonly string _adminPassword;
        private string? _accessToken;

        public KeycloakService(HttpClient httpClient, ILogger<KeycloakService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _keycloakUrl = Environment.GetEnvironmentVariable("KEYCLOAK_URL") ?? "http://keycloak:8080";
            _adminUsername = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_USER") ?? "admin";
            _adminPassword = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD") ?? "admin";
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return _accessToken;

            var tokenEndpoint = $"{_keycloakUrl}/realms/master/protocol/openid-connect/token";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "admin-cli"),
                new KeyValuePair<string, string>("username", _adminUsername),
                new KeyValuePair<string, string>("password", _adminPassword)
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            _accessToken = tokenResponse.GetProperty("access_token").GetString();

            return _accessToken!;
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _httpClient;
        }

        public async Task CreateRealmAsync(string realmName)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var realm = new
                {
                    realm = realmName,
                    enabled = true,
                    displayName = "Milk API Manager"
                };

                var json = JsonSerializer.Serialize(realm);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_keycloakUrl}/admin/realms", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully created realm {realmName}");
                }
                else
                {
                    _logger.LogWarning($"Failed to create realm {realmName}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating realm {realmName}");
            }
        }

        public async Task CreateClientAsync(string realmName, string clientId, string clientSecret)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var clientConfig = new
                {
                    clientId = clientId,
                    secret = clientSecret,
                    directAccessGrantsEnabled = true,
                    serviceAccountsEnabled = true,
                    implicitFlowEnabled = false,
                    standardFlowEnabled = true,
                    publicClient = false,
                    protocol = "openid-connect",
                    attributes = new
                    {
                        saml_assertion_consumer_url_post = "",
                        saml_assertion_consumer_url_redirect = "",
                        saml_single_logout_service_url_post = "",
                        saml_single_logout_service_url_redirect = ""
                    }
                };

                var json = JsonSerializer.Serialize(clientConfig);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_keycloakUrl}/admin/realms/{realmName}/clients", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully created client {clientId} in realm {realmName}");
                }
                else
                {
                    _logger.LogWarning($"Failed to create client {clientId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating client {clientId} in realm {realmName}");
            }
        }
    }
}