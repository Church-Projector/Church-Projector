using System.Runtime.InteropServices;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

namespace ChurchProjector.PowerPoint;

public class PowerPoint
{
    private readonly Application _application;
    private Presentation? Presentation { get; set; }

    public Action? SlideShowBegin { get; set; }
    public Action<int>? SlideShowNextSlide { get; set; }
    public Action? SlideShowEnd { get; set; }
    public Action<List<string>>? ImagesGenerated { get; set; }
    private readonly string _tempDirectory = Directory.CreateTempSubdirectory().FullName;

    public PowerPoint()
    {
        _application = new Application();
        _application.SlideShowBegin += ApplicationSlideShowBegin;
        _application.SlideShowNextSlide += ApplicationSlideShowNextSlide;
        _application.SlideShowEnd += ApplicationSlideShowEnd;
    }

    private void ApplicationSlideShowBegin(SlideShowWindow wn)
    {
        SlideShowBegin?.Invoke();
    }

    private void ApplicationSlideShowNextSlide(SlideShowWindow wn)
    {
        if (Presentation is null)
        {
            return;
        }

        SlideShowNextSlide?.Invoke(Presentation.SlideShowWindow.View.Slide.SlideIndex - 1);
    }

    private void ApplicationSlideShowEnd(Presentation pres)
    {
        SlideShowEnd?.Invoke();
    }

    public void StartPowerPointViewer(string file)
    {
        string tempDir = Path.Combine(_tempDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        (List<string> files, Presentation presentation) result = ConvertToImages(file, tempDir, _application);

        ImagesGenerated?.Invoke(result.files);

        Presentation = result.presentation;
        Presentation.SlideShowSettings.ShowPresenterView = MsoTriState.msoFalse;
        new Thread(() => Presentation.SlideShowSettings.Run()).Start();
    }

    private static (List<string> files, Presentation presentation) ConvertToImages(string powerPointFile,
        string outputDirectory, Application powerPointApplication)
    {
        Presentation pptPresentation = powerPointApplication.Presentations.Open(powerPointFile, MsoTriState.msoTrue,
            MsoTriState.msoFalse, MsoTriState.msoFalse);

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

    public void SetCurrentSlide(int index, float left, float top, float width, float height)
    {
        if (Presentation is null)
        {
            return;
        }
        
        Presentation.SlideShowSettings.Run();
        
        Presentation.SlideShowWindow.Left = left;
        Presentation.SlideShowWindow.Top = top;
        Presentation.SlideShowWindow.Width = width;
        Presentation.SlideShowWindow.Height = height;
        
        Presentation.SlideShowWindow.View.GotoSlide(index + 1);
    }

    public void Stop()
    {
        if (Presentation is not null)
        {
            Presentation.Close();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Marshal.FinalReleaseComObject(Presentation);
            }

            Presentation = null;
        }

        try
        {
            _application.Quit();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Marshal.FinalReleaseComObject(_application);
            }
        }
        catch
        {
            // Ignore
        }

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Ignore
        }
    }

    public void HidePresentationAsync()
    {
        Presentation?.SlideShowWindow.View.State = PpSlideShowState.ppSlideShowBlackScreen;
    }
}