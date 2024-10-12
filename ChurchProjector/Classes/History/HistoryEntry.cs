using System.Collections.Generic;

namespace ChurchProjector.Classes.History;
public class HistoryEntry(string title, List<ImageWithName> images, string? filename)
{
    public string Title { get; set; } = title;
    public List<ImageWithName> Images { get; set; } = images;
    public string? Filename { get; set; } = filename;
}
