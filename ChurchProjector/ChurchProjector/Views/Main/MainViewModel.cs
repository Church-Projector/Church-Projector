using System;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes;
using ChurchProjector.Classes.History;
using ChurchProjector.Views.Bible;
using ChurchProjector.Views.History;
using ChurchProjector.Views.Schedule;
using ChurchProjector.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChurchProjector.Classes.Schedule;
using ChurchProjector.Views.Notification;
using ChurchProjector.Views.Notifications;
using MuPDFCore;
using static ChurchProjector.Classes.DrawingHelper;

namespace ChurchProjector.Views.Main;

public partial class MainViewModel : ObservableObject
{
    public PresentationFile? Images
    {
        get;
        set
        {
            if (field != value)
            {
                if (field is not null)
                {
                    foreach (ImageWithName image in field.Images)
                    {
                        if (!HistoryViewModel.Histories.Any(hi => hi.Images.Contains(image)))
                        {
                            image.Dispose();
                        }
                    }
                }

                if (_powerPointClient?.IsRunning ?? false)
                {
                    // If we set images and currently PowerPoint is running it means that the presentation gets closed anyway.
                    // If we don't unset the image we will get a disposed exception when the image window will be displayed again.
                    ImageWindow.ViewModel.ImageSource = null;
                }

                _powerPointClient?.ClosePresentationAsync();

                SelectedImage = null;
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MayEdit));
            }
        }
    } = null;

    public NotificationService Notifications { get; } = new();

    private void PowerPoint_SlideShowNextSlider(int slideIndex)
    {
        if (slideIndex < 0 || slideIndex >= Images.Images.Count)
        {
            return;
        }

        SelectedImage = Images.Images[slideIndex];
    }

    private void PowerPoint_SlideShowEnd()
    {
        Dispatcher.UIThread.Invoke(ImageWindow.Show);
    }

    public ImageWithName? SelectedImage
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                if (_powerPointClient is not { IsRunning: true })
                {
                    if (value != null)
                    {
                        ImageWindow.ViewModel.ImageSource = value.Image;
                        ImageWindow.ViewModel.CurrentFileType = FileType.Image;
                    }
                }
                else
                {
                    int index = field == null ? -1 : Images.Images.IndexOf(value);
                    if (value is not null)
                    {
                        ImageWindow.ViewModel.ImageSource = value.Image;
                    }

                    if (index >= 0)
                    {
                        // This is in points
                        // https://learn.microsoft.com/en-us/office/vba/api/powerpoint.slideshowwindow.left
                        _powerPointClient.SetCurrentSlideAsync(index + 1,
                            left: (float)(Settings.SelectedMonitore.Value.Bounds.X * 0.75),
                            top: (float)(Settings.SelectedMonitore.Value.Bounds.Y * 0.75),
                            width: (float)(Settings.SelectedMonitore.Value.Bounds.Width * 0.75),
                            height: (float)(Settings.SelectedMonitore.Value.Bounds.Height * 0.75));

                        // Hide the image window, so it is not in front of the power point file.
                        Dispatcher.UIThread.Invoke(ImageWindow.Hide);
                    }
                    else
                    {
                        PowerPoint_SlideShowEnd();
                        ImageWindow.ViewModel.CurrentFileType = FileType.Image;
                        _powerPointClient?.HidePresentationAsync();
                    }
                }

                OnPropertyChanged(nameof(ImageSelected));
                OnPropertyChanged(nameof(SlideSelected));
            }
        }
    }

    public bool ImageSelected => ImageWindow.ViewModel.ImageSource is not null;
    public bool SlideSelected => ImageWindow.ViewModel.ImageSource is not null;

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

    public string? UpdateText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(UpdateText));
                OnPropertyChanged(nameof(MayUpdate));
            }
        }
    }

    public bool MayUpdate => !string.IsNullOrEmpty(UpdateText);

    public ImageWindow ImageWindow { get; }
    public SettingsViewModel Settings { get; set; }

    private readonly PowerPointClient? _powerPointClient;
    private readonly string _tempDirectory = Directory.CreateTempSubdirectory().FullName;

    public MainViewModel(Window window, SettingsViewModel settings, IStorageProvider storageProvider,
        IClipboard clipboard, string? version)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _powerPointClient = new PowerPointClient();
            _powerPointClient.SlideShowNextSlide += PowerPoint_SlideShowNextSlider;
            _powerPointClient.SlideShowEnd += PowerPoint_SlideShowEnd;
            _powerPointClient.ImagesGenerated += async result =>
            {
                PresentationFile presentationFile = new([], "file" /*TODO Filename*/);
                foreach (var image in result)
                {
                    presentationFile.Images.Add(new ImageWithName(Path.GetFileNameWithoutExtension(image),
                        new Bitmap(image), false));
                }

                Images = presentationFile;
                await _powerPointClient.ImagesSetAsync();
            };
            _powerPointClient.PowerPointConnectionChanged = isConnected =>
            {
                if (!isConnected) {
                    Dispatcher.UIThread.Invoke(() => Notifications.Show(Lang.Resources.PowerPointIsMissing, NotificationType.Error));
                } else {
                    Dispatcher.UIThread.Invoke(() => Notifications.Show(Lang.Resources.PowerPointIsConnected, NotificationType.Success));
                }
            };
        }

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

        ScheduleViewModel = new ScheduleViewModel(storageProvider, clipboard,
            powerPointAvailable: _powerPointClient is not null, libVlcAvailable: false)
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
                        string tempFile = $"{Path.Combine(_tempDirectory, Guid.NewGuid().ToString())}.png";
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
                    if (_powerPointClient is null)
                    {
                        return;
                    }

                    _powerPointClient.StartPowerPointViewerAsync(scheduleEntry.FilePath);
                }
                else if (FileExtensions.GetFileType(scheduleEntry.FileType) == FileType.Song)
                {
                    Classes.Song song = Classes.Song.LoadFromFile(scheduleEntry.FilePath);
                    RenderImages(song.Title,
                        song.GetImages().ConvertAll(x => new ImageWithName(x.title, x.bitmap, x.isOverflowing)), false,
                        scheduleEntry.FilePath);
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

                    BibleSearchWindow bibleSearchWindow =
                        new(BibleViewModel.SelectedBible1, BibleViewModel.SearchText!.Trim())
                        {
                            OpenVerse = OpenVerse
                        };
                    await bibleSearchWindow.ShowDialog(window);
                    return;
                }

                List<int> verseRange = Enumerable.Range(BibleViewModel.SelectedVerseStart!.Value,
                    BibleViewModel.SelectedVerseEnd!.Value - BibleViewModel.SelectedVerseStart!.Value + 1).ToList();

                List<ImageCreationContent> GetImageCreationContent(List<int> verses)
                {
                    List<ImageCreationContent> imageCreationContents = [];
                    if (BibleViewModel.SelectedBible1 is not null)
                    {
                        imageCreationContents.Add(new()
                        {
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible1,
                                BibleViewModel.SelectedBook.Number, BibleViewModel.SelectedChapter, verses.Min(),
                                verses.Max()),
                            Content =
                            [
                                string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(
                                    BibleViewModel.SelectedBible1,
                                    BibleViewModel.SelectedBook.Number,
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
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible2,
                                BibleViewModel.SelectedBook.Number, BibleViewModel.SelectedChapter, verses.Min(),
                                verses.Max()),
                            Content =
                            [
                                string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(
                                    BibleViewModel.SelectedBible2,
                                    BibleViewModel.SelectedBook.Number,
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
                            Header = BibleViewModel.GetSelectedBiblePositionHeader(BibleViewModel.SelectedBible3,
                                BibleViewModel.SelectedBook.Number, BibleViewModel.SelectedChapter, verses.Min(),
                                verses.Max()),
                            Content =
                            [
                                string.Join(" ", BibleViewModel.GetPreviewSelectedBiblePosition(
                                    BibleViewModel.SelectedBible3,
                                    BibleViewModel.SelectedBook.Number,
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
                                   Configuration = GlobalConfig.JsonFile.Settings.DisplayConfiguration
                                       .BibleConfiguration,
                                   ImageCreationContent =
                                       GetImageCreationContent(verseRange.Take(versesToTake).ToList()),
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
                }

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
                BibleViewModel.SetBiblePosition(BibleViewModel.SelectedBook.Title, BibleViewModel.SelectedChapter,
                    nextVerse, nextVerse);
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
                BibleViewModel.SetBiblePosition(BibleViewModel.SelectedBook.Title, BibleViewModel.SelectedChapter,
                    previous, previous);
                BibleViewModel.SearchText = BibleViewModel.SelectedBiblePosition;
                BibleViewModel.AcceptSearchTextCommand.Execute(null);
                SelectedImage = Images.Images.FirstOrDefault();
            }),
        };

        Classes.Version.GetNewestVersionStringAsync(CancellationToken.None).ContinueWith(async (newestVersionTask) =>
        {
            string? newestVersion = await newestVersionTask;
            if (newestVersion != version && !string.IsNullOrWhiteSpace(version) && !string.IsNullOrEmpty(newestVersion))
            {
                UpdateText = string.Format(Lang.Resources.UpdateAvailable, newestVersion);
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

    internal void OpenVerse(ExactVerse verse)
    {
        if (BibleViewModel.SelectedBible1 is null ||
            !BibleViewModel.SelectedBible1.Books.Any(b => b.Number == verse.BookNumber))
        {
            // TODO Select correct book
            return;
        }

        Book book = BibleViewModel.SelectedBible1.Books.First(b => b.Number == verse.BookNumber);
        BibleViewModel.SearchText = $"{book.Title} {verse.ChapterNumber}, {verse.VerseNumber}";
        BibleViewModel.AcceptSearchTextCommand.Execute(null);
    }

    [ObservableProperty] private ICommand openSongQuickSearchCommand;

    [ObservableProperty] private ICommand openSongSearchCommand;

    [ObservableProperty] private ICommand openBibleSearchCommand;

    [ObservableProperty] private ICommand hideImageCommand;

    [ObservableProperty] private ICommand openShowNotificationCommand;

    [ObservableProperty] private ICommand editCommand;

    [ObservableProperty] private ICommand addSongCommand;

    public bool MayEdit => Images is not null && !string.IsNullOrWhiteSpace(Images.Filename) &&
                           Path.GetExtension(Images.Filename).ToLowerInvariant() is ".sng";

    [ObservableProperty] private bool _bannerIsRunning;
    public void StopAll()
    {
        ImageWindow.StopBanner();
        ImageWindow.Hide();
        ImageWindow.ViewModel.ImageSource = null;
        _powerPointClient?.StopPowerPointViewerAsync();
    }

    public void HideImage(bool fadeOut)
    {
        SelectedImage = null;
        ImageWindow.ViewModel.HideImage(fadeOut);
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