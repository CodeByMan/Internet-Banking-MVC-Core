namespace InternetBanking.WebApp.Helpers
{
    public static class PaymentSubmissionTokenHelper
    {
        private const string SessionKeyPrefix = "PaymentSubmission:";

        public static string Issue(HttpContext context)
        {
            string token = Guid.NewGuid().ToString("N");
            context.Session.SetString(SessionKeyPrefix + token, "valid");
            return token;
        }

        public static bool IsValid(HttpContext context, string? token)
        {
            return !string.IsNullOrWhiteSpace(token)
                && context.Session.GetString(SessionKeyPrefix + token) == "valid";
        }

        public static bool Consume(HttpContext context, string? token)
        {
            if (!IsValid(context, token))
            {
                return false;
            }

            context.Session.Remove(SessionKeyPrefix + token);
            return true;
        }
    }
}
