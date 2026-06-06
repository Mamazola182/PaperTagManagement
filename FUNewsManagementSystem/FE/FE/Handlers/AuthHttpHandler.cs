using FE.Services;
using System.Net.Http.Headers;

namespace FE.Handlers
{
    public class AuthHttpHandler:DelegatingHandler
    {
        private readonly TokenServices _tokenService;

        public AuthHttpHandler(TokenServices tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_tokenService.IsTokenExpiredSoon())
            {
                await _tokenService.RefreshTokenAsync();
            }

            var token = _tokenService.GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
