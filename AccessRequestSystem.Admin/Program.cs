using AccessRequestSystem.Admin.Forms;
using AccessRequestSystem.Admin.Services;
using Microsoft.Extensions.Configuration;

namespace AccessRequestSystem.Admin;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7241";

        var apiClient = new ApiClient(baseUrl);

        Application.Run(new LoginForm(apiClient));
    }
}