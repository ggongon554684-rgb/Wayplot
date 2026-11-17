using System.Net;
using System.Net.Http.Json;
using WayPlot.domain.Contracts;

namespace WayPlot;

/// <summary>Talks to WayPlot.api. First sign-in needs a connection.</summary>
public sealed class AuthClient(HttpClient http)
{
    public async Task<LoginResult?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(username, password), ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResult>(ct);
    }
}
