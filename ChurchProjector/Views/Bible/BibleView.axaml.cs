using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;

namespace ChurchProjector.Views.Bible;
public partial class BibleView : UserControl
{
    public BibleView()
    {
        InitializeComponent();
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && DataContext is BibleViewModel bvm && bvm.MayAcceptSearchTextCommand)
        {
            bvm.AcceptSearchTextCommand.Execute(null);
        }
    }

    // We use PointerReleased instead of selection changed because selection changed fires an infinity loop.
    private void ListBox_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (DataContext is not BibleViewModel bvm || ListBox.SelectedItems is null)
        {
            return;
        }

        List<KeyValuePair<int, string>> selectedVerses = ListBox.SelectedItems.Cast<KeyValuePair<int, string>>().ToList();

        if (selectedVerses.Count == 0)
        {
            return;
        }

        bool multipleVerses = selectedVerses.Count > 1;
        if (multipleVerses)
        {
            if (selectedVerses.Min(x => x.Key) == bvm.SelectedVerseStart && selectedVerses.Max(x => x.Key) == bvm.SelectedVerseEnd)
            {
                return;
            }
            bvm.SearchText = $"{bvm.SelectedBookPart} {bvm.SelectedChapter} {selectedVerses.Min(x => x.Key)} - {selectedVerses.Max(x => x.Key)}";
        }
        else
        {
            if (selectedVerses[0].Key == bvm.SelectedVerseStart)
            {
                return;
            }
            bvm.SearchText = $"{bvm.SelectedBookPart} {bvm.SelectedChapter} {selectedVerses[0].Key}";
        }

        if (bvm.MayAcceptSearchTextCommand)
        {
            bvm.AcceptSearchTextCommand.Execute(null);
        }
    }
}
