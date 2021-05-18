namespace NeoClient.Microsoft.Extensions.DependencyInjection.Options
{
    public class NeoClientOptions
    {
        public string Uri { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public bool StripHyphens { get; set; } = false;
    }
}