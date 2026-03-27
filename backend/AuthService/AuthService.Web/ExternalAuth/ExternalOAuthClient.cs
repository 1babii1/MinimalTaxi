using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AuthService.Web.ExternalAuth;

public sealed class ExternalOAuthClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalAuthOptions _options;

    public ExternalOAuthClient(IHttpClientFactory httpClientFactory, IOptions<ExternalAuthOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public bool TryBuildAuthorizeUrl(string provider, string state, out string? url, out string? error)
    {
        url = null;
        error = null;

        var configResult = TryGetProvider(provider, out var config, out error);
        if (!configResult)
            return false;

        if (!config!.Enabled)
        {
            error = $"{provider} auth is disabled.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.RedirectUri))
        {
            error = $"{provider} auth is not configured.";
            return false;
        }

        if (string.Equals(provider, "yandex", StringComparison.OrdinalIgnoreCase))
        {
            var query = new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = config.ClientId,
                ["redirect_uri"] = config.RedirectUri,
                ["scope"] = string.IsNullOrWhiteSpace(config.Scope) ? "login:email" : config.Scope,
                ["state"] = state,
            };

            url = QueryHelpers.AddQueryString(config.AuthorizationEndpoint, query!);
            return true;
        }

        if (string.Equals(provider, "vk", StringComparison.OrdinalIgnoreCase))
        {
            var query = new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = config.ClientId,
                ["redirect_uri"] = config.RedirectUri,
                ["scope"] = string.IsNullOrWhiteSpace(config.Scope) ? "email" : config.Scope,
                ["state"] = state,
                ["v"] = string.IsNullOrWhiteSpace(config.ApiVersion) ? "5.199" : config.ApiVersion,
            };

            url = QueryHelpers.AddQueryString(config.AuthorizationEndpoint, query!);
            return true;
        }

        error = "Unsupported provider.";
        return false;
    }

    public async Task<(ExternalUserInfo? user, string? error)> ExchangeCodeAsync(
        string provider,
        string code,
        CancellationToken cancellationToken)
    {
        if (!TryGetProvider(provider, out var config, out var providerError))
            return (null, providerError);

        if (!config!.Enabled)
            return (null, $"{provider} auth is disabled.");

        if (string.IsNullOrWhiteSpace(code))
            return (null, "Authorization code is required.");

        if (string.Equals(provider, "yandex", StringComparison.OrdinalIgnoreCase))
            return await ExchangeYandexAsync(config, code, cancellationToken);

        if (string.Equals(provider, "vk", StringComparison.OrdinalIgnoreCase))
            return await ExchangeVkAsync(config, code, cancellationToken);

        return (null, "Unsupported provider.");
    }

    private async Task<(ExternalUserInfo? user, string? error)> ExchangeYandexAsync(
        ExternalAuthOptions.ExternalProviderOptions config,
        string code,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["redirect_uri"] = config.RedirectUri,
        };

        using var tokenResponse = await httpClient.PostAsync(
            config.TokenEndpoint,
            new FormUrlEncodedContent(form),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
            return (null, "Yandex token exchange failed.");

        var tokenPayload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        using var tokenDoc = JsonDocument.Parse(tokenPayload);

        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            return (null, "Yandex access token is missing.");

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
            return (null, "Yandex access token is empty.");

        var userInfoUrl = QueryHelpers.AddQueryString(config.UserInfoEndpoint, "format", "json");
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

        using var userResponse = await httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
            return (null, "Yandex user info request failed.");

        var userPayload = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        using var userDoc = JsonDocument.Parse(userPayload);

        if (!userDoc.RootElement.TryGetProperty("id", out var idElement))
            return (null, "Yandex user id is missing.");

        var providerUserId = idElement.GetString();
        if (string.IsNullOrWhiteSpace(providerUserId))
            return (null, "Yandex user id is empty.");

        string? email = null;
        if (userDoc.RootElement.TryGetProperty("default_email", out var emailElement))
            email = emailElement.GetString();

        return (new ExternalUserInfo(providerUserId, email), null);
    }

    private async Task<(ExternalUserInfo? user, string? error)> ExchangeVkAsync(
        ExternalAuthOptions.ExternalProviderOptions config,
        string code,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var tokenUrl = QueryHelpers.AddQueryString(
            config.TokenEndpoint,
            new Dictionary<string, string?>
            {
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["redirect_uri"] = config.RedirectUri,
                ["code"] = code,
                ["v"] = string.IsNullOrWhiteSpace(config.ApiVersion) ? "5.199" : config.ApiVersion,
            }!);

        using var tokenResponse = await httpClient.GetAsync(tokenUrl, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
            return (null, "VK token exchange failed.");

        var tokenPayload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        using var tokenDoc = JsonDocument.Parse(tokenPayload);

        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            return (null, "VK access token is missing.");

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
            return (null, "VK access token is empty.");

        string? email = null;
        if (tokenDoc.RootElement.TryGetProperty("email", out var emailElement))
            email = emailElement.GetString();

        string? providerUserId = null;

        if (tokenDoc.RootElement.TryGetProperty("user_id", out var userIdElement))
            providerUserId = userIdElement.ValueKind == JsonValueKind.Number
                ? userIdElement.GetInt64().ToString()
                : userIdElement.GetString();

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            var userInfoUrl = QueryHelpers.AddQueryString(
                config.UserInfoEndpoint,
                new Dictionary<string, string?>
                {
                    ["access_token"] = accessToken,
                    ["v"] = string.IsNullOrWhiteSpace(config.ApiVersion) ? "5.199" : config.ApiVersion,
                }!);

            using var userInfoResponse = await httpClient.GetAsync(userInfoUrl, cancellationToken);
            if (!userInfoResponse.IsSuccessStatusCode)
                return (null, "VK user info request failed.");

            var userInfoPayload = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
            using var userInfoDoc = JsonDocument.Parse(userInfoPayload);

            if (userInfoDoc.RootElement.TryGetProperty("response", out var responseArray) &&
                responseArray.ValueKind == JsonValueKind.Array &&
                responseArray.GetArrayLength() > 0)
            {
                var firstUser = responseArray[0];
                if (firstUser.TryGetProperty("id", out var idElement))
                    providerUserId = idElement.ValueKind == JsonValueKind.Number
                        ? idElement.GetInt64().ToString()
                        : idElement.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(providerUserId))
            return (null, "VK user id is missing.");

        return (new ExternalUserInfo(providerUserId, email), null);
    }

    private bool TryGetProvider(
        string provider,
        out ExternalAuthOptions.ExternalProviderOptions? config,
        out string? error)
    {
        config = null;
        error = null;

        if (string.IsNullOrWhiteSpace(provider))
        {
            error = "Provider is required.";
            return false;
        }

        if (string.Equals(provider, "yandex", StringComparison.OrdinalIgnoreCase))
        {
            config = _options.Yandex;
            return true;
        }

        if (string.Equals(provider, "vk", StringComparison.OrdinalIgnoreCase))
        {
            config = _options.Vk;
            return true;
        }

        error = "Unsupported provider.";
        return false;
    }
}
