using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChurchProjector.Views.Controls;
public partial class HighlightedTextBox : UserControl
{
    public HighlightedTextBox()
    {
        InitializeComponent();

        PropertyChanged += HighlightedTextBox_PropertyChanged;
    }

    private void HighlightedTextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(SearchText) || e.Property.Name == nameof(Text))
        {
            Txt.Inlines?.Clear();
            Txt.Inlines ??= [];

            string searchText = SearchText?.Trim() ?? string.Empty;
            if (Text is null)
            {
                return;
            }
            if (searchText.Length == 0)
            {
                // No search text.
                Txt.Inlines.Add(Text);
                return;
            }

            int startIndex = Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                // The text contains the complete search text.
                int endIndex = startIndex + searchText.Length;

                Txt.Inlines.AddRange(GetRuns(
                [
                    new Marker(startIndex, endIndex),
                ]));
                return;
            }

            List<string> searchParts = [.. searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
            HashSet<int> positions = [];
            foreach (string searchPart in searchParts)
            {
                int searchPartIndex = Text.IndexOf(searchPart, StringComparison.OrdinalIgnoreCase);
                if (searchPartIndex >= 0)
                {
                    // The text contains the complete search text.
                    int endIndex = searchPartIndex + searchPart.Length;
                    foreach (int position in Enumerable.Range(searchPartIndex, endIndex - searchPartIndex + 1))
                    {
                        positions.Add(position);
                    }
                }
            }

            if (positions.Count > 0)
            {
                List<Marker> markers = [];
                Marker currentMarker = new(positions.Min(), positions.Min());
                foreach (int position in positions.OrderBy(x => x).Skip(1))
                {
                    if (currentMarker.End == position - 1)
                    {
                        currentMarker.End = position;
                    }
                    else
                    {
                        markers.Add(currentMarker);
                        currentMarker = new(position, position);
                    }
                }
                markers.Add(currentMarker);

                Txt.Inlines.AddRange(GetRuns(markers));
            }
            else
            {
                // Not found
                Txt.Inlines.Add(Text);
            }
        }
    }

    private List<Run> GetRuns(List<Marker> highlightPositions)
    {
        List<Run> runs = [];
        int lastPosition = 0;
        foreach (Marker highlightPosition in highlightPositions)
        {
            runs.Add(new Run(Text.Substring(lastPosition, highlightPosition.Start - lastPosition)));
            runs.Add(new Run(Text.Substring(highlightPosition.Start, highlightPosition.End - highlightPosition.Start))
            {
                FontWeight = HighlightFontWeight,
                Foreground = HighlightForeground ?? Foreground,
            });
            lastPosition = highlightPosition.End;
        }
        if (lastPosition < Text.Length)
        {
            runs.Add(new Run(Text.Substring(lastPosition, Text.Length - lastPosition)));
        }

        return runs;
    }

    public static readonly StyledProperty<string> SearchTextProperty =
        AvaloniaProperty.Register<HighlightedTextBox, string>(nameof(SearchText), defaultValue: "");

    public string SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HighlightedTextBox, string>(nameof(Text), defaultValue: "");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<HighlightedTextBox, IBrush?>(nameof(HighlightForeground), defaultValue: null);

    public IBrush? HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public static readonly StyledProperty<FontWeight> HighlightFontWeightProperty =
        AvaloniaProperty.Register<HighlightedTextBox, FontWeight>(nameof(HighlightFontWeight), defaultValue: FontWeight.Bold);

    public FontWeight HighlightFontWeight
    {
        get => GetValue(HighlightFontWeightProperty);
        set => SetValue(HighlightFontWeightProperty, value);
    }

    private class Marker(int start, int end)
    {
        public int Start { get; set; } = start;
        public int End { get; set; } = end;
    }
}
