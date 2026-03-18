using System.Collections.Generic;

namespace ChurchProjector.Classes;
public class PresentationFile(List<ImageWithName> images, string? filename = null)
{
    public List<ImageWithName> Images { get; set; } = images;
    public string? Filename { get; set; } = filename;
}
