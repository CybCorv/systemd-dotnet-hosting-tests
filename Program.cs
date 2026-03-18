using Microsoft.Extensions.Hosting.Systemd;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();
var app = builder.Build();

PrintDiagnostics(app.Logger);

app.MapGet("/", () => "Hello World!");
app.Run();

static void PrintDiagnostics(ILogger logger)
{
    var systemdDetection = $@"=== Systemd Detection Diagnostics ===
  .NET Version: {Environment.Version}
  OS Platform: {Environment.OSVersion.Platform}
  Process ID: {Environment.ProcessId}
  IsSystemdService: {SystemdHelpers.IsSystemdService()}";
    logger.LogInformation("{SystemdDetection}", systemdDetection);

    var envVariables = Environment.GetEnvironmentVariables()
                                  .Cast<System.Collections.DictionaryEntry>()
                                  .OrderBy(e => e.Key)
                                  .Select(e => $"{e.Key}={e.Value}");
    logger.LogDebug($@"=== Environment Variables ===
    {string.Join(Environment.NewLine, envVariables)}");

    var psi = new System.Diagnostics.ProcessStartInfo("env")
    {
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    using var child = System.Diagnostics.Process.Start(psi)!;
    logger.LogDebug($@"=== Child Process Environment ===
    {child.StandardOutput.ReadToEnd()}");
    child.WaitForExit();
}