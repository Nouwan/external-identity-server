namespace authorization
{
    public class AuthorizationConfig
    {
        public string ClientRegistrationsPath { get; set; }
        public Environment Environment { get; set; }
    }

    public enum Environment
    {
        Development,
        Test,
        Production
    }
}
