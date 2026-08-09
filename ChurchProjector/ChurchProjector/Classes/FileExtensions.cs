namespace ChurchProjector.Classes;
public static class FileExtensions
{
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
        if (extension is ".gif" or ".avi" or ".mp4" or ".mov")
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
