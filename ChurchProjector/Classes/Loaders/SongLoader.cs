using Serilog;
using System;
using System.IO;
using System.Threading;

namespace ChurchProjector.Classes.Loaders;
public static class SongLoader
{
    private static readonly object LOCK = new();
    public static void LoadSongs(string? path, CancellationToken cancellationToken)
    {
        lock (LOCK)
        {
            GlobalConfig.Songs.Clear();
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                foreach (string song in Directory.GetFiles(path, "*.sng", SearchOption.AllDirectories))
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GlobalConfig.Songs.Add(Song.LoadFromFile(song));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Fehler bei dem Laden der Liedes \"{0}\". Meldung: \"{1}\"", Path.GetFileName(song), ex.Message);
                    }
                }
            }
        }
    }
}
