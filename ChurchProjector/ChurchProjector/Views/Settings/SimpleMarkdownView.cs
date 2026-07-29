using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ChurchProjector.Views.Settings;

public sealed class SimpleMarkdownView : StackPanel
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<SimpleMarkdownView, string?>(nameof(Markdown));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public SimpleMarkdownView()
    {
        Spacing = 5;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownProperty)
        {
            RenderMarkdown(change.NewValue as string);
        }
    }

    private void RenderMarkdown(string? markdown)
    {
        Children.Clear();

        foreach (string rawLine in (markdown ?? string.Empty).ReplaceLineEndings("\n").Split('\n'))
        {
            string line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                Children.Add(new Border { Height = 4 });
            }
            else if (line.StartsWith("# "))
            {
                Children.Add(CreateText(line[2..], 22, FontWeight.Bold, new Thickness(0, 8, 0, 2)));
            }
            else if (line.StartsWith("## "))
            {
                Children.Add(CreateText(line[3..], 17, FontWeight.SemiBold, new Thickness(0, 6, 0, 1)));
            }
            else if (line.StartsWith("- "))
            {
                Children.Add(new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("auto,*"),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "•",
                            Margin = new Thickness(2, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Top
                        },
                        CreateText(line[2..], 14, FontWeight.Normal, default, 1)
                    }
                });
            }
            else
            {
                Children.Add(CreateText(line, 14, FontWeight.Normal, default));
            }
        }
    }

    private static TextBlock CreateText(
        string text,
        double fontSize,
        FontWeight fontWeight,
        Thickness margin,
        int column = 0)
    {
        TextBlock block = new()
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = margin,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(block, column);
        return block;
    }
}
