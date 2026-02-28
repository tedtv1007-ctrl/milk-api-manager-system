using System.Diagnostics;
using System.Text;

namespace MilkApiManager.Services;

public class LoadTestService : ILoadTestService
{
    private readonly ILogger<LoadTestService> _logger;
    private readonly string _scriptsPath = "load-tests";

    public LoadTestService(ILogger<LoadTestService> logger)
    {
        _logger = logger;
        if (!Directory.Exists(_scriptsPath)) Directory.CreateDirectory(_scriptsPath);
    }

    public async Task<string> RunTestAsync(string targetUrl, int vus, int durationSeconds)
    {
        _logger.LogInformation($"Starting load test: {targetUrl}, VUs: {vus}, Duration: {durationSeconds}s");

        // Simple k6 script generation
        var script = $@"
import http from 'k6/http';
import {{ sleep }} from 'k6';

export const options = {{
  vus: {vus},
  duration: '{durationSeconds}s',
}};

export default function () {{
  http.get('{targetUrl}');
  sleep(1);
}}";
        var scriptFile = Path.Combine(_scriptsPath, $"test_{Guid.NewGuid():N}.js");
        await File.WriteAllTextAsync(scriptFile, script);

        try
        {
            // Execute k6 (assuming k6 is installed in the environment)
            var startInfo = new ProcessStartInfo
            {
                FileName = "k6",
                Arguments = $"run \"{scriptFile}\" --summary-export=summary.json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var output = new StringBuilder();
            
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine("[STDERR] " + args.Data); };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return output.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute k6. Ensure k6 is installed.");
            return "k6 execution failed. Is it installed?";
        }
    }
}
