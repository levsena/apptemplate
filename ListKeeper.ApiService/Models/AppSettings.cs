namespace ListKeeperWebApi.WebApi.Models
{
    public class AppSettings
    {
        // A placeholder property for custom settings from appsettings.json.
        // Update these properties to match your configuration.
        public string? ServiceName { get; set; }

        // Optionally, you can include JWT settings here to map to an "AppSettings:Jwt" section.
        public string? RoutePrefix { get; set; }
    }
}
