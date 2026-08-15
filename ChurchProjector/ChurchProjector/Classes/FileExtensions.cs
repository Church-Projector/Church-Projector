using System.IO;

namespace ChurchProjector.Classes;
public static class FileExtensions
{
    public static bool IsAudio(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        extension = Path.GetExtension(extension).ToLowerInvariant();
        return extension is ".aac" or ".ac3" or ".aif" or ".aiff" or ".alac" or ".amr" or ".flac"
            or ".m4a" or ".mid" or ".midi" or ".mp2" or ".mp3" or ".oga" or ".ogg" or ".opus"
            or ".wav" or ".wma";
    }

    public static FileType? GetFileType(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }
        extension = extension.ToLowerInvariant();
        if (extension is ".png" or ".jpg" or ".jpeg" or ".webp")
        {
            return FileType.Image;
        }
        if (extension is ".gif" or ".3gp" or ".avi" or ".flv" or ".m2ts" or ".m4v" or ".mkv"
            or ".mov" or ".mp4" or ".mpeg" or ".mpg" or ".mts" or ".ogv" or ".ts" or ".vob"
            or ".webm" or ".wmv" or ".aac" or ".ac3" or ".aif" or ".aiff" or ".alac" or ".amr"
            or ".flac" or ".m4a" or ".mid" or ".midi" or ".mp2" or ".mp3" or ".oga" or ".ogg"
            or ".opus" or ".wav" or ".wma")
        {
            return FileType.Movie;
        }
        if (extension is ".pdf")
        {
            return FileType.Pdf;
        }
        if (extension is ".pptx")
        {
            return FileType.Powerpoint;
        }
        if (extension is ".sng")
        {
            return FileType.Song;
        }

        return null;
    }
}
