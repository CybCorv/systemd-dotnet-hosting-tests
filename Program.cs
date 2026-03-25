using Microsoft.Extensions.Hosting.Systemd;

// Capture before UseSystemd
var notifySocketBefore = Environment.GetEnvironmentVariable("NOTIFY_SOCKET") ?? "(unset)";
var systemdExecPidBefore = Environment.GetEnvironmentVariable("SYSTEMD_EXEC_PID") ?? "(unset)";
var journalStreamBefore = Environment.GetEnvironmentVariable("JOURNAL_STREAM") ?? "(unset)";

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();
var app = builder.Build();

PrintDiagnostics(app.Logger, notifySocketBefore, systemdExecPidBefore, journalStreamBefore);

app.MapGet("/", () => "Hello World!");
app.Run();

static void PrintDiagnostics(ILogger logger, string notifySocketBefore, string systemdExecPidBefore, string journalStreamBefore)
{
    var systemdDetection = $@"=== Systemd Detection Diagnostics ===
  .NET Version: {Environment.Version}
  OS Platform: {Environment.OSVersion.Platform}
  Process ID: {Environment.ProcessId}
  IsSystemdService: {SystemdHelpers.IsSystemdService()}
  --- Before UseSystemd ---
  NOTIFY_SOCKET: {notifySocketBefore}
  SYSTEMD_EXEC_PID: {systemdExecPidBefore}
  JOURNAL_STREAM: {journalStreamBefore}
  --- After UseSystemd ---
  NOTIFY_SOCKET: {Environment.GetEnvironmentVariable("NOTIFY_SOCKET") ?? "(unset)"}
  SYSTEMD_EXEC_PID: {Environment.GetEnvironmentVariable("SYSTEMD_EXEC_PID") ?? "(unset)"}
  JOURNAL_STREAM: {Environment.GetEnvironmentVariable("JOURNAL_STREAM") ?? "(unset)"}";
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