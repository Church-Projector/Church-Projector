using System.Runtime.InteropServices;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

namespace ChurchProjector.PowerPoint;

public class PowerPoint
{
    private Application? _application;
    private Presentation? Presentation { get; set; }

    public Action? SlideShowBegin { get; set; }
    public Action<int>? SlideShowNextSlide { get; set; }
    public Action? SlideShowEnd { get; set; }
    public Action? PowerPointImagesSet { get; set; }
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
        if (Presentation is not null)
        {
            Presentation.Close();
            Marshal.FinalReleaseComObject(Presentation);
            Presentation = null;
            
            for (var i = 0; i < 15; i++)
            {
                // PowerPoint needs some time...
                Thread.Sleep(10);
            }
        }

        (List<string> files, Presentation presentation) result = ConvertToImages(file, tempDir, _application);

        PowerPointImagesSet = () =>
        {
            Presentation = result.presentation;
            Presentation.SlideShowSettings.ShowPresenterView = MsoTriState.msoFalse;
            var thread = new Thread(() => Presentation.SlideShowSettings.Run());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        };
        ImagesGenerated?.Invoke(result.files);
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

            Marshal.ReleaseComObject(slide);
        }

        return (imagePaths, pptPresentation);
    }

    public void SetCurrentSlide(int index, float left, float top, float width, float height)
    {
        if (Presentation is null)
        {
            return;
        }

        Presentation.SlideShowWindow.Left = left;
        Presentation.SlideShowWindow.Top = top;
        Presentation.SlideShowWindow.Width = width;
        Presentation.SlideShowWindow.Height = height;

        Presentation.SlideShowWindow.View.GotoSlide(index);
    }

    public void Stop()
    {
        if (Presentation is not null)
        {
            Presentation.Close();
            Marshal.FinalReleaseComObject(Presentation);

            Presentation = null;
        }

        try
        {
            if (_application is not null)
            {
                _application.SlideShowBegin -= ApplicationSlideShowBegin;
                _application.SlideShowNextSlide -= ApplicationSlideShowNextSlide;
                _application.SlideShowEnd -= ApplicationSlideShowEnd;
                
                _application.Quit();
                Marshal.FinalReleaseComObject(_application);
            }

            _application = null;
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
        if (Presentation is null)
        {
            return;
        }

        Presentation.SlideShowWindow.View.State = PpSlideShowState.ppSlideShowBlackScreen;
    }

    public void ClosePresentationAsync()
    {
        if (Presentation is null)
        {
            return;
        }

        Presentation.Close();
        Marshal.FinalReleaseComObject(Presentation);
        Presentation = null;
    }
}