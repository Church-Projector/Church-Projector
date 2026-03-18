using System.IO;

namespace ChurchProjector.Classes.Schedule;
public class ScheduleEntry(string title, string filePath, string fileType)
{
    public string Title { get; set; } = title;
    public string FilePath { get; set; } = filePath;
    public string FileType { get; set; } = fileType;
    public bool FileExists => File.Exists(FilePath);
    public bool ApplicationExists { get; set; }
    public string? Icon => FileExtensions.GetFileIcon(FileType);
    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    public static ScheduleEntry FromFile(string filePath)
    {
        return new(Path.GetFileNameWithoutExtension(filePath), filePath, Path.GetExtension(filePath));
    }
}
