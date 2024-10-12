using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Classes.Converter;
using ChurchProjector.Classes.History;
using ChurchProjector.Classes.Schedule;
using ChurchProjector.Views.Bible;
using ChurchProjector.Views.History;
using ChurchProjector.Views.Schedule;
using ChurchProjector.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Office.Interop.PowerPoint;
using MuPDFCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using static ChurchProjector.Classes.DrawingHelper;

namespace ChurchProjector.Views.Main;

public partial class MainViewModel : ObservableObject
{
    public string TempDirectory { get; }
    private PresentationFile? _images = null;
    public PresentationFile? Images
    {
        get => _images;
        set
        {
            if (_images != value)
            {
                if (_images is not null)
                {
                    foreach (ImageWithName image in _images.Images)
                    {
                        if (!HistoryViewModel.Histories.Any(hi => hi.Images.Contains(image)))
                        {
                            image.Dispose();
                        }
                    }
                }
                if (Presentation is not null)
                {
                    Presentation.Close();
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.FinalReleaseComObject(Presentation);
                    }
                    Presentation = null;
                }
                SelectedImage = null;
                _images = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MayEdit));
            }
        }
    }

    public Application? PowerPoint { get; }
    public Presentation? Presentation { get; set; }

    private void PowerPoint_SlideShowNextSlide(SlideShowWindow Wn)
    {
        if (Presentation is null)
        {
            return;
        }
        int slideIndex = Presentation.SlideShowWindow.View.Slide.SlideIndex - 1;
        if (slideIndex < 0 || slideIndex >= Images.Images.Count)
        {
            return;
        }
        SelectedImage = Images.Images[slideIndex];
    }

    private void PowerPoint_SlideShowBegin(SlideShowWindow Wn)
    {
        //throw new NotImplementedException();
    }

    private void PowerPoint_SlideShowEnd(Presentation Pres)
    {
        Dispatcher.UIThread.Invoke(new Action(() =>
        {
            ImageWindow.Show();
            // May not be done here
            //ClosePowerPoint(PowerPoint);
            //PowerPoint = null;
            //SelectedImage = null;
            //Images = [];
        }));
    }

    private ImageWithName? _selectedImage;
    public ImageWithName? SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (_selectedImage != value)
            {
                _selectedImage = value;
                OnPropertyChanged();
                if (Presentation is null)
                {
                    if (value != null)
                    {
                        ImageWindow.ViewModel.ImageSource = value.Image;
                        ImageWindow.ViewModel.CurrentFileType = FileType.Image;
                    }
                }
                else
                {
                    int index = _selectedImage == null ? -1 : Images.Images.IndexOf(_selectedImage);
                    if (index >= 0)
                    {
                        Presentation.SlideShowSettings.Run();

                        // This is in points
                        // https://learn.microsoft.com/en-us/office/vba/api/powerpoint.slideshowwindow.left
                        Presentation.SlideShowWindow.Left = (float)(Settings.SelectedMonitore.Value.Bounds.X * 0.75);
                        Presentation.SlideShowWindow.Width = (float)(Settings.SelectedMonitore.Value.Bounds.Width * 0.75);

                        Presentation.SlideShowWindow.Top = (float)(Settings.SelectedMonitore.Value.Bounds.Y * 0.75);
                        Presentation.SlideShowWindow.Height = (float)(Settings.SelectedMonitore.Value.Bounds.Height * 0.75);

                        Dispatcher.UIThread.Invoke(ImageWindow.Hide);

                        Presentation.SlideShowWindow.View.GotoSlide(index + 1);
                    }
                    else
                    {
                        // TODO Powerpoint history?!
                        Presentation.Close();
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            Marshal.FinalReleaseComObject(Presentation);
                        }
                        Presentation = null;

                        SelectedImage = null;
                        Images = null;
                    }
                }
                OnPropertyChanged(nameof(ImageSelected));
                OnPropertyChanged(nameof(SlideSelected));
            }
        }
    }

    public bool ImageSelected => ImageWindow.ViewModel.ImageSource is not null;
    public bool SlideSelected => ImageWindow.ViewModel.ImageSource is not null || (Presentation is not null && Presentation.SlideShowWindow.View.State != PpSlideShowState.ppSlideShowBlackScreen);

    public HistoryViewModel HistoryViewModel { get; }
    public ScheduleViewModel ScheduleViewModel { get; }

    public void RenderImages(string title, List<ImageWithName> images, string? filename = null)
    {
        RenderImages(title, images, addToHistory: true, filename);
    }

    public void RenderImages(string title, List<ImageWithName> images, bool addToHistory, string? filename = null)
    {
        if (addToHistory)
        {
            HistoryViewModel.Histories.Insert(0, new HistoryEntry(title, images, filename));
        }
        Images = new PresentationFile(images, filename);
    }

    public BibleViewModel BibleViewModel { get; set; }

    private string? _updateText;
    public string? UpdateText
    {
        get => _updateText;
        set
        {
            if (_updateText != value)
            {
                _updateText = value;
                OnPropertyChanged(nameof(UpdateText));
                OnPropertyChanged(nameof(MayUpdate));
            }
        }
    }
    public bool MayUpdate => !string.IsNullOrEmpty(UpdateText);

    public ImageWindow ImageWindow { get; }
    public SettingsViewModel Settings { get; set; }

    public MainViewModel(Window window, SettingsViewModel settings, IStorageProvider storageProvider, IClipboard clipboard, string? version)
    {
        try
        {
            PowerPoint = new();
            PowerPoint.SlideShowEnd += PowerPoint_SlideShowEnd;
            PowerPoint.SlideShowBegin += PowerPoint_SlideShowBegin;
            PowerPoint.SlideShowNextSlide += PowerPoint_SlideShowNextSlide;
        }
        catch
        {
            Log.Error("Es können keine PowerPoint Präsentationen angezeigt werden, da PowerPoint nicht installiert ist.");
        }

        GlobalConfig.HasError.PropertyChanged += HasError_PropertyChanged;

        TempDirectory = Directory.CreateTempSubdirectory().FullName;
        HistoryViewModel = new()
        {
            OpenHistoryEntryCommand = new RelayCommand<HistoryEntry>((HistoryEntry? historyEntry) =>
            {
                if (HistoryViewModel is null || historyEntry is null)
                {
                    return;
                }
                HistoryViewModel.Histories.Remove(historyEntry);
                RenderImages(historyEntry.Title, historyEntry.Images, historyEntry.Filename);
            }),
            RemoveHistoryEntryCommand = new RelayCommand<HistoryEntry>((HistoryEntry? historyEntry) =>
            {
                if (HistoryViewModel is null || historyEntry is null)
                {
                    return;
                }
                if (HistoryViewModel.Histories.Contains(historyEntry))
                {
                    HistoryViewModel.Histories.Remove(historyEntry);
                    foreach (ImageWithName image in historyEntry.Images.ToList())
                    {
                        if (SelectedImage == image)
                        {
                            SelectedImage = null;
                        }
                        if (!Images.Images.Contains(image))
                        {
                            image.Dispose();
                        }
                    }

                }
            }),
        };

        ScheduleViewModel = new(storageProvider, clipboard, powerPointAvailable: PowerPoint is not null, libVlcAvailable: false)
        {
            OpenScheduleEntryCommand = new RelayCommand<ScheduleEntry>((ScheduleEntry? scheduleEntry) =>
            {
                if (scheduleEntry is null || !scheduleEntry.FileExists)
                {
                    return;
                }
                if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Image)
                {
                    Images = new(
                    [
                        new ImageWithName(scheduleEntry.Title, new Bitmap(scheduleEntry.FilePath), false)
                    ], scheduleEntry.FilePath);
                }
                else if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Pdf)
                {
                    using MuPDFContext muPDFContext = new();
                    using MuPDFDocument muPDFDocument = new(muPDFContext, scheduleEntry.FilePath);
                    List<string> files = [];
                    for (int pageNumber = 0; pageNumber < muPDFDocument.Pages.Count; pageNumber++)
                    {
                        string tempFile = $"{Path.Combine(TempDirectory, Guid.NewGuid().ToString())}.png";
                        muPDFDocument.SaveImage(pageNumber, 1, PixelFormats.RGBA, tempFile, RasterOutputFileTypes.PNG);
                        files.Add(tempFile);
                    }
                    Images = new(files.AsParallel()
                        .Select((f, i) =>
                        {
                            return new
                            {
                                Index = i,
                                Image = new ImageWithName(scheduleEntry.Title, new Bitmap(f), false),
                            };
                        })
                        .OrderBy(x => x.Index)
                        .Select(x => x.Image)
                        .ToList(), scheduleEntry.FilePath);
                }
                else if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Powerpoint)
                {
                    if (PowerPoint is null)
                    {
                        return;
                    }
                    string tempDir = Path.Combine(TempDirectory, Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    PresentationFile presentationFile = new(new List<ImageWithName>(), scheduleEntry.FilePath);
                    (List<string> files, Presentation presentation) result = PowerPointToImages.ConvertToImages(scheduleEntry.FilePath, tempDir, PowerPoint);
                    foreach (string image in result.files)
                    {
                        presentationFile.Images.Add(new ImageWithName(Path.GetFileNameWithoutExtension(image), new Bitmap(image), false));
                    }
                    Images = presentationFile;
                    Presentation = result.presentation;
                    Presentation.SlideShowSettings.ShowPresenterView = Microsoft.Office.Core.MsoTriState.msoFalse;
                    new Thread(() => Presentation.SlideShowSettings.Run()).Start();
                }
                else if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Song)
                {
                    Classes.Song song = Classes.Song.LoadFromFile(scheduleEntry.FilePath);
                    RenderImages(song.Title, song.GetImages().ConvertAll(x => new ImageWithName(x.title, x.bitmap, x.isOverflowing)), false, scheduleEntry.FilePath);
                }
            }),
        };
        BibleViewModel = new()
        {
            AcceptSearchTextCommand = new RelayCommand(async () =>
            {
                if (BibleViewModel is null)
                {
                    return;
                }
                if (!string.IsNullOrWhiteSpace(BibleViewModel.SearchHint))
                {
                    if (BibleViewModel.SelectedBible1 is null)
                    {
                        return;
                    }
                    BibleSearchWindow bibleSearchWindow = new(BibleViewModel.SelectedBible1, BibleViewModel.SearchText!.Trim())
                    {
                        OpenVerse = OpenVerse
                    };
                    await bibleSearchWindow.ShowDialog(window);
                    return;
                }
                List<int> verseRange = Enumerable.Range(BibleViewModel.SelectedVerseStart!.Value, BibleViewModel.SelectedVerseEnd!.Value - BibleViewModel.SelectedVerseStart!.Value + 1).ToList();
                List<ImageCreationContent> GetImageCreationContent(List<int> verses)
                {
                    List<ImageCreationContent> imageCreationContents = [];
                    if (BibleViewModel.SelectedBible1 is not null)
                    {
                        imageCreationContents.Add(new()
                        {
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible1, BibleViewModel.SelectedBookNumber, BibleViewModel.SelectedChapter, verses.Min(), verses.Max()),
                            Content = [string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(BibleViewModel.SelectedBible1,
                                                                                     BibleViewModel.SelectedBookNumber,
                                                                                     BibleViewModel.SelectedChapter.Value,
                                                                                     verses.Min(),
                                                                                     verses.Max()))
                            ],
                            Bottom = BibleViewModel.SelectedBible1.Title,
                        });
                    }
                    if (BibleViewModel.SelectedBible2 is not null)
                    {
                        imageCreationContents.Add(new()
                        {
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible2, BibleViewModel.SelectedBookNumber, BibleViewModel.SelectedChapter, verses.Min(), verses.Max()),
                            Content = [string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(BibleViewModel.SelectedBible2,
                                                                                     BibleViewModel.SelectedBookNumber,
                                                                                     BibleViewModel.SelectedChapter.Value,
                                                                                     verses.Min(),
                                                                                     verses.Max()))
                            ],
                            Bottom = BibleViewModel.SelectedBible2.Title,
                        });
                    }
                    if (BibleViewModel.SelectedBible3 is not null)
                    {
                        imageCreationContents.Add(new()
                        {
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible3, BibleViewModel.SelectedBookNumber, BibleViewModel.SelectedChapter, verses.Min(), verses.Max()),
                            Content = [string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(BibleViewModel.SelectedBible3,
                                                                                     BibleViewModel.SelectedBookNumber,
                                                                                     BibleViewModel.SelectedChapter.Value,
                                                                                     verses.Min(),
                                                                                     verses.Max()))
                            ],
                            Bottom = BibleViewModel.SelectedBible3.Title,
                        });
                    }
                    return imageCreationContents;
                }
                List<List<int>> ranges = [];
                while (verseRange.Count != 0)
                {
                    int versesToTake = 1;

                    while (true)
                    {
                        while (versesToTake <= verseRange.Count && DrawingHelper.MayRenderImage(new ImageCreation()
                        {
                            Configuration = GlobalConfig.JsonFile.Settings.DisplayConfiguration.BibleConfiguration,
                            ImageCreationContent = GetImageCreationContent(verseRange.Take(versesToTake).ToList()),
                            LangCount = 1,
                        }))
                        {
                            versesToTake++;
                        }

                        versesToTake = versesToTake == 1 ? versesToTake : versesToTake - 1;
                        ranges.Add(verseRange.Take(versesToTake).ToList());
                        verseRange = verseRange.Skip(versesToTake).ToList();
                        if (verseRange.Count == 0)
                        {
                            break;
                        }
                    }
                };

                List<ImageWithName> images = ranges.AsParallel()
                        .Select((range, i) =>
                        {
                            var (isOverflowing, bitmap) = DrawingHelper.GetImage(new ImageCreation()
                            {
                                Configuration = GlobalConfig.JsonFile.Settings.DisplayConfiguration.BibleConfiguration,
                                ImageCreationContent = GetImageCreationContent(range),
                                LangCount = 1,
                            });
                            return new
                            {
                                Index = i,
                                Image = new ImageWithName(BibleViewModel.SelectedBiblePosition, bitmap, isOverflowing),
                            };
                        }).OrderBy(x => x.Index)
                        .Select(x => x.Image)
                        .ToList();
                RenderImages(BibleViewModel.SelectedBiblePosition, images);
            }),
            NextVerseCommand = new RelayCommand(() =>
            {
                if (BibleViewModel?.SelectedVerseEnd is null)
                {
                    return;
                }
                int nextVerse = BibleViewModel.SelectedVerseEnd.Value + 1;
                BibleViewModel.SetBiblePosition(BibleViewModel.SelectedBookName, BibleViewModel.SelectedChapter, nextVerse, nextVerse);
                BibleViewModel.SearchText = BibleViewModel.SelectedBiblePosition;
                BibleViewModel.AcceptSearchTextCommand.Execute(null);
                SelectedImage = Images.Images.FirstOrDefault();
            }),
            BackVerseCommand = new RelayCommand(() =>
            {
                if (BibleViewModel?.SelectedVerseStart is null)
                {
                    return;
                }
                int previous = BibleViewModel.SelectedVerseStart.Value - 1;
                BibleViewModel.SetBiblePosition(BibleViewModel.SelectedBookName, BibleViewModel.SelectedChapter, previous, previous);
                BibleViewModel.SearchText = BibleViewModel.SelectedBiblePosition;
                BibleViewModel.AcceptSearchTextCommand.Execute(null);
                SelectedImage = Images.Images.FirstOrDefault();
            }),
        };

        Classes.Version.GetNewestVersionStringAsync(CancellationToken.None).ContinueWith(async (newestVersionTask) =>
        {
            string? newestVersion = await newestVersionTask;
            if (newestVersion != version && !string.IsNullOrEmpty(newestVersion))
            {
                UpdateText = $"Es ist ein Update auf Version {newestVersion} verfügbar.";
            }
        });

        Settings = settings;

        ImageWindow = new ImageWindow(Settings);
        Settings.RecreateBanner += ImageWindow.CreateBanner;

        ImageWindow.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageViewModel.ImageSource))
        {
            this.OnPropertyChanged(nameof(ImageSelected));
            this.OnPropertyChanged(nameof(SlideSelected));
        }
    }

    private void HasError_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasError));
    }

    internal void OpenVerse(ExactVerse verse)
    {
        if (BibleViewModel.SelectedBible1 is null || !BibleViewModel.SelectedBible1.Books.Any(b => b.Number == verse.BookNumber))
        {
            // TODO Select correct book
            return;
        }
        Book book = BibleViewModel.SelectedBible1.Books.First(b => b.Number == verse.BookNumber);
        BibleViewModel.SearchText = $"{book.Title} {verse.ChapterNumber}, {verse.VerseNumber}";
        BibleViewModel.AcceptSearchTextCommand.Execute(null);
    }

    public bool HasError => GlobalConfig.HasError.Value;

    [ObservableProperty]
    private ICommand openSongQuickSearchCommand;

    [ObservableProperty]
    private ICommand openSongSearchCommand;

    [ObservableProperty]
    private ICommand openBibleSearchCommand;

    [ObservableProperty]
    private ICommand hideImageCommand;

    [ObservableProperty]
    private ICommand openShowNotificationCommand;

    [ObservableProperty]
    private ICommand openLogsCommand;

    [ObservableProperty]
    private ICommand editCommand;

    [ObservableProperty]
    private ICommand addSongCommand;

    public bool MayEdit => Images is not null && !string.IsNullOrWhiteSpace(Images.Filename) && Path.GetExtension(Images.Filename).ToLowerInvariant() is ".sng";

    public void StopAll()
    {
        ImageWindow.StopBanner();
        ImageWindow.Hide();
        ImageWindow.ViewModel.ImageSource = null;
        if (PowerPoint is not null)
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
            PowerPoint.SlideShowEnd -= PowerPoint_SlideShowEnd;
            PowerPoint.SlideShowBegin -= PowerPoint_SlideShowBegin;
            PowerPoint.SlideShowNextSlide -= PowerPoint_SlideShowNextSlide;
            try
            {
                PowerPoint.Quit();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Marshal.FinalReleaseComObject(PowerPoint);
                }
            }
            catch
            {
                // Mission failed.
            }
        }

        try
        {
            Directory.Delete(TempDirectory, recursive: true);
        }
        catch
        {

        }
    }

    public void HideImage(bool fadeOut)
    {
        if (this.Presentation is not null)
        {
            // Hide presentation
            this.Presentation.SlideShowWindow.View.State = PpSlideShowState.ppSlideShowBlackScreen;
            _selectedImage = null;
            this.OnPropertyChanged(nameof(SelectedImage));
            this.OnPropertyChanged(nameof(SlideSelected));
        }
        else
        {
            SelectedImage = null;
            ImageWindow.ViewModel.HideImage(fadeOut);
        }
    }

    public void SetHasSecondScreen(bool hasSecondScreen)
    {
        if (hasSecondScreen)
        {
            ImageWindow.Show();
        }
        else
        {
            ImageWindow.StopBanner();
            ImageWindow.Hide();
        }
    }
}
