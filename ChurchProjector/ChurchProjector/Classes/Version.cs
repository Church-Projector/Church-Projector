using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchProjector.Classes;
public static class Version
{
    public static string? GetCurrentVersion()
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "ChurchProjector.exe")))
        {
            return FileVersionInfo.GetVersionInfo(Path.Combine(AppContext.BaseDirectory, "ChurchProjector.exe")).FileVersion;
        }
        else
        {
            return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        }
    }

    public static async Task<string?> GetNewestVersionStringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            WebsiteRelease? release =
                await WebsiteService.GetNewestReleaseAsync(cancellationToken);
            return release?.Version;
        }
        catch
        {
            // Ignore
        }
        return null;
    }

}
