namespace InternetBanking.WebApp.Middlewares
{
    public class ValidateUserSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ValidateUserSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool HasUser()
        {
            return _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
        }
    }
}
