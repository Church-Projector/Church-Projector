using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ChurchProjector.Classes.Loaders;
public static class BibleLoader
{
    private static readonly object LOCK = new();
    public static void LoadBibles(string? path, CancellationToken cancellationToken)
    {
        lock (LOCK) {
            GlobalConfig.Bibles.Clear();
            GlobalConfig.Bibles.Add(null);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                foreach (string extension in new List<string>() { "*.xml", "*.spb" })
                {
                    foreach (string bible in Directory.GetFiles(path, extension, SearchOption.AllDirectories))
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            GlobalConfig.Bibles.Add(Bible.LoadFromFile(bible));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Fehler bei dem Laden der Bibel \"{0}\". Meldung: \"{1}\"", Path.GetFileName(bible), ex.Message);
                        }
                    }
                }
            }
        }
    }
}
