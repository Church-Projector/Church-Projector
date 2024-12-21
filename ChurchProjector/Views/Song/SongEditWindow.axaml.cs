using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.Input;

namespace ChurchProjector.Views.Song;
public partial class SongEditWindow : Window
{
    private readonly SongEditViewModel _viewModel;

    public SongEditWindow() : this(null) { }

    public SongEditWindow(string? filename)
    {
        InitializeComponent();

        _viewModel = new SongEditViewModel()
        {
            Song = filename is null ? ChurchProjector.Classes.Song.Create() : ChurchProjector.Classes.Song.LoadFromFile(filename),
            CloseDialogCommand = new RelayCommand(Close),
            SaveDialogCommand = new RelayCommand(async () =>
            {
                if (_viewModel is null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(_viewModel.Song.FilePath))
                {
                    IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
                    {
                        Title = "Lied speichern",
                        FileTypeChoices =
                        [
                            new FilePickerFileType("Lied")
                            {
                                Patterns = ["*.sng"],
                            },
                        ],
                        DefaultExtension = "*.sng",
                        SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(GlobalConfig.JsonFile.Settings.PathSettings.SongPath!),
                    });
                    if (file is null)
                    {
                        return;
                    }
                    _viewModel.Song.FilePath = file.Path.AbsolutePath;
                }
                _viewModel.Song.SaveSong();
                Song = _viewModel.Song;
                Close();
            }),
        };
        DataContext = _viewModel;

        this.SizeChanged += SongEditWindow_SizeChanged;
    }

    public Classes.Song? Song { get; private set; }

    private void SongEditWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (this.Bounds.Height > 654)
        {
            this.Height = 654;
            this.SizeToContent = SizeToContent.Manual;
        }
    }
}
