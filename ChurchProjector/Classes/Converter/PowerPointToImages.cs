using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System.Collections.Generic;
using System.IO;

namespace ChurchProjector.Classes.Converter;
public static class PowerPointToImages
{
    public static (List<string> files, Presentation presentation) ConvertToImages(string powerPointFile, string outputDirectory, Application powerPointapplication)
    {
        Presentation pptPresentation = powerPointapplication.Presentations.Open(powerPointFile, MsoTriState.msoTrue, MsoTriState.msoFalse, MsoTriState.msoFalse);

        List<string> imagePaths = [];

        for (int i = 1; i <= pptPresentation.Slides.Count; i++)
        {
            Slide slide = pptPresentation.Slides[i];
            string imagePath = Path.Combine(outputDirectory, $"slide_{i}.png");
            slide.Export(imagePath, "PNG");
            imagePaths.Add(imagePath);
        }

        return (imagePaths, pptPresentation);
    }
}
