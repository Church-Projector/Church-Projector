using Avalonia.Media.Imaging;
using ChurchProjector.Classes.Configuration.Image;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChurchProjector.Classes;
public static class DrawingHelper
{
    private static Bitmap ToAvaloniaImage(this SKBitmap bitmap)
    {
        MemoryStream ms = MemoryStreamPool.Shared.GetStream();
        try
        {
            bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
            bitmap.Dispose();
            ms.Position = 0;
            return new Bitmap(ms);
        }
        finally
        {
            MemoryStreamPool.Shared.ReturnStream(ms);
        }
    }

    public static (float headerTextSize, float contentTextSize, float bottomTextSize, List<List<string>> splittedContent) GetMaxTextSize(SKFont sKFont,
                                                                                                                                         ImageCreationConfiguration imageCreationConfiguration,
                                                                                                                                         float height,
                                                                                                                                         float width,
                                                                                                                                         List<string> header,
                                                                                                                                         List<List<string>> content,
                                                                                                                                         List<string> bottom,
                                                                                                                                         bool disableLineBreaks)
    {
        float GetFreeHeight(int rowCount)
        {
            return height - (rowCount * _padding);
        };
        float maxWidth = width - _padding;

        float headerTextSize = imageCreationConfiguration.Header.MinFontSize;
        float contentTextSize = imageCreationConfiguration.Content.MinFontSize;
        float bottomTextSize = imageCreationConfiguration.Bottom.MinFontSize;
        List<List<string>> tempContent = content.ToList();
        List<List<string>> acceptedTempContent = tempContent.ToList();
        while (true)
        {
            if (headerTextSize == imageCreationConfiguration.Header.MaxFontSize
                && contentTextSize == imageCreationConfiguration.Content.MaxFontSize
                && bottomTextSize == imageCreationConfiguration.Bottom.MaxFontSize)
            {
                return (headerTextSize, contentTextSize, bottomTextSize, acceptedTempContent);
            }
            headerTextSize = Math.Min(headerTextSize + 1, imageCreationConfiguration.Header.MaxFontSize);
            contentTextSize = Math.Min(contentTextSize + 1, imageCreationConfiguration.Content.MaxFontSize);
            bottomTextSize = Math.Min(bottomTextSize + 1, imageCreationConfiguration.Bottom.MaxFontSize);

            float totalRenderedHeight = 0;
            float maxRenderedWidth = 0;

            foreach (string line in header)
            {
                totalRenderedHeight += headerTextSize;
                maxRenderedWidth = Math.Max(maxRenderedWidth, MeasureText(sKFont, headerTextSize, line));
            }

            if (!disableLineBreaks)
            {
                foreach (List<string> tempLines in tempContent)
                {
                    while (true)
                    {
                        sKFont.Size = contentTextSize;
                        for (int i = 0; i < tempLines.Count; i++)
                        {
                            string currentLine = tempLines[i];
                            if (MeasureText(sKFont, contentTextSize, currentLine) > maxWidth)
                            {
                                int lastSpace = currentLine.LastIndexOf(' ');
                                if (lastSpace > 0)
                                {
                                    if (currentLine.Substring(0, lastSpace).EndsWith("</upper>"))
                                    {
                                        lastSpace = currentLine.Substring(0, currentLine.LastIndexOf(' ')).LastIndexOf(' ');
                                    }
                                }
                                if (lastSpace > 0)
                                {
                                    tempLines[i] = currentLine.Substring(0, lastSpace);
                                    if (tempLines.Count == i + 1)
                                    {
                                        tempLines.Add(currentLine.Substring(lastSpace + 1));
                                    }
                                    else
                                    {
                                        tempLines[i + 1] = $"{currentLine.Substring(lastSpace + 1)} {tempLines[i + 1]}";
                                    }
                                    i--;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            foreach (List<string> lines in tempContent)
            {
                foreach (string line in lines)
                {
                    totalRenderedHeight += contentTextSize;
                    maxRenderedWidth = Math.Max(maxRenderedWidth, MeasureText(sKFont, contentTextSize, line));
                }
            }

            foreach (string line in bottom)
            {
                totalRenderedHeight += bottomTextSize;
                maxRenderedWidth = Math.Max(maxRenderedWidth, MeasureText(sKFont, bottomTextSize, line));
            }

            if (totalRenderedHeight > GetFreeHeight(tempContent.Sum(x => x.Count) + header.Count + bottom.Count) || maxRenderedWidth > maxWidth)
            {
                break;
            }
            acceptedTempContent = tempContent.ToList();
        }

        return (headerTextSize - 1, contentTextSize - 1, bottomTextSize - 1, acceptedTempContent);
    }

    public static bool MayRenderImage(this ImageCreation imageCreation)
    {
        bool disableLineBreaks = imageCreation.LangCount != 1;

        using SKFont _font = new();
        // The first call is a bit flaggy and doesn't always return the correct text size.
        _ = GetMaxTextSize(_font,
            imageCreation.Configuration,
            _height,
            _width,
            imageCreation.ImageCreationContent.ConvertAll(x => x.Header),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Content),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Bottom),
            disableLineBreaks);

        // Normally we could just check, if the min text size works, but we want to split the content.
        // For even faster checks we could create a get split text method that just split's the text without searching for the text size.
        (float headerTextSize, float contentTextSize, float bottomTextSize, List<List<string>> splittedContent) textSizes = GetMaxTextSize(_font,
            imageCreation.Configuration,
            _height,
            _width,
            imageCreation.ImageCreationContent.ConvertAll(x => x.Header),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Content),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Bottom),
            disableLineBreaks);

        int headerRowCount = imageCreation.ImageCreationContent.Count(x => !string.IsNullOrWhiteSpace(x.Header));
        int contentRowCount = textSizes.splittedContent.Sum(x => x.Count());
        int bottomRowCount = imageCreation.ImageCreationContent.Count(x => !string.IsNullOrWhiteSpace(x.Bottom));

        return (headerRowCount * textSizes.headerTextSize)
            + (contentRowCount * textSizes.contentTextSize)
            + (bottomRowCount * textSizes.bottomTextSize)
            + ((headerRowCount + contentRowCount + bottomRowCount) * _padding) <= _height;
    }

    const int _width = 1920;
    const int _height = 1080;
    const int _padding = 10;
    public static (bool isOverflowing, Bitmap bitmap) GetImage(this ImageCreation imageCreation)
    {
        (bool isOverflowing, SKBitmap bitmap) skImage = GetSKImage(imageCreation);
        return (skImage.isOverflowing, skImage.bitmap.ToAvaloniaImage());
    }

    public static (bool isOverflowing, SKBitmap bitmap) GetSKImage(ImageCreation imageCreation)
    {
        bool disableLineBreaks = imageCreation.LangCount != 1;

        SKBitmap bitmap = new(_width, _height);
        using SKFont _font = new();
        using SKCanvas? canvas = new(bitmap);

        using SKPaint paint = new()
        {
            IsAntialias = true,
        };
        canvas.DrawColor(SKColor.Parse("#000000"));

        // The first call is a bit flaggy and doesn't always return the correct text size.
        _ = GetMaxTextSize(_font,
            imageCreation.Configuration,
            _height,
            _width,
            imageCreation.ImageCreationContent.ConvertAll(x => x.Header),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Content),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Bottom),
            disableLineBreaks);

        (float headerTextSize, float contentTextSize, float bottomTextSize, List<List<string>> splittedContent) textSizes = GetMaxTextSize(_font,
            imageCreation.Configuration,
            _height,
            _width,
            imageCreation.ImageCreationContent.ConvertAll(x => x.Header),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Content),
            imageCreation.ImageCreationContent.ConvertAll(x => x.Bottom),
            disableLineBreaks);

        for (int i = 0; i < imageCreation.ImageCreationContent.Count; i++)
        {
            imageCreation.ImageCreationContent[i].Content = textSizes.splittedContent[i];
        }

        // we add half padding to the top
        float y = (_padding / 2);
        int index = 0;

        void DrawText(string text, HorizontalAlignment alignment, string color, float textSize, bool italic = false)
        {
            _font.Size = textSize;
            _font.Typeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, italic ? SKFontStyle.Italic : SKFontStyle.Normal);
            paint.Color = SKColor.Parse(color);
            float x = GetX(_font, textSize, text, alignment, _width, _padding);
            y += textSize;
            RenderText(canvas, paint, _font, textSize, text, x, y);
            y += _padding;
        }

        foreach (ImageCreationContent imageCreationItem in imageCreation.ImageCreationContent)
        {
            if (!string.IsNullOrWhiteSpace(imageCreationItem.Header))
            {
                int langIndex = imageCreation.ImageCreationContent.IndexOf(imageCreationItem);
                DrawText(imageCreationItem.Header, imageCreation.Configuration.Header.HorizontalAlignment, imageCreation.Configuration.Header.GetColor(langIndex), textSizes.headerTextSize);
            }
            foreach (var line in imageCreationItem.Content)
            {
                if (string.IsNullOrEmpty(line))
                {
                    y += _font.Size + _padding;
                    continue;
                }

                // lang detected by image creation item.
                int langIndex = imageCreation.ImageCreationContent.IndexOf(imageCreationItem);
                bool italic = false;
                if (imageCreation.ImageCreationContent.Count == 1)
                {
                    // Lang detected by row index
                    langIndex = index % imageCreation.LangCount;
                    italic = langIndex % 2 == 1;
                }

                DrawText(line, imageCreation.Configuration.Content.HorizontalAlignment, imageCreation.Configuration.Content.GetColor(langIndex), textSizes.contentTextSize, italic);
                index++;
            }
            if (!string.IsNullOrWhiteSpace(imageCreationItem.Bottom))
            {
                int langIndex = imageCreation.ImageCreationContent.IndexOf(imageCreationItem);
                DrawText(imageCreationItem.Bottom, imageCreation.Configuration.Bottom.HorizontalAlignment, imageCreation.Configuration.Bottom.GetColor(langIndex), textSizes.bottomTextSize);
            }
        }
        canvas.Save();

        // at the end there is a full padding, we are using a spacing of a half padding
        return (y - (_padding / 2) > _height, bitmap);
    }

    private static float GetX(SKFont sKFont, float textSize, string text, HorizontalAlignment horizontalAlignment, float width, float padding)
    {
        float textWidth = MeasureText(sKFont, textSize, text);
        if (horizontalAlignment == HorizontalAlignment.Left)
        {
            return padding / 2;
        }
        else if (horizontalAlignment == HorizontalAlignment.Center)
        {
            return (width - textWidth) / 2;
        }
        else
        {
            return width - textWidth - (padding / 2);
        }
    }

    private static void RenderText(SKCanvas sKCanvas, SKPaint sKPaint, SKFont sKFont, float textSize, string text, float x, float y)
    {
        int renderFrom = 0;

        while (true)
        {
            int renderTo = text.IndexOf("<upper>", renderFrom);
            if (renderTo < 0)
            {
                break;
            }
            string textToRender = text.Substring(renderFrom, renderTo - renderFrom);
            sKFont.Size = textSize;
            sKCanvas.DrawText(textToRender, x, y, SKTextAlign.Left, sKFont, sKPaint);
            x += sKFont.MeasureText(textToRender);

            renderFrom = renderTo + "<upper>".Length;
            renderTo = text.IndexOf("</upper>", renderFrom);
            textToRender = text.Substring(renderFrom, renderTo - renderFrom);
            sKFont.Size = textSize / 3 * 2;
            sKCanvas.DrawText(textToRender, x, y - (textSize / 2) + _padding, SKTextAlign.Left, sKFont, sKPaint);
            x += sKFont.MeasureText(textToRender);
            renderFrom += textToRender.Length + "</upper>".Length;
        }

        sKFont.Size = textSize;
        sKCanvas.DrawText(text.Substring(renderFrom, text.Length - renderFrom), x, y, SKTextAlign.Left, sKFont, sKPaint);
    }

    private static float MeasureText(SKFont sKFont, float textSize, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }
        float width = 0;
        int renderFrom = 0;

        while (true)
        {
            int renderTo = text.IndexOf("<upper>", renderFrom);
            if (renderTo < 0)
            {
                break;
            }
            string textToRender = text.Substring(renderFrom, renderTo - renderFrom);
            sKFont.Size = textSize;
            width += sKFont.MeasureText(textToRender);

            renderFrom = renderTo + "<upper>".Length;
            renderTo = text.IndexOf("</upper>", renderFrom);
            textToRender = text.Substring(renderFrom, renderTo - renderFrom);
            sKFont.Size = textSize / 3 * 2;
            width += sKFont.MeasureText(textToRender);
            renderFrom += textToRender.Length + "</upper>".Length;
        }

        sKFont.Size = textSize;
        width += sKFont.MeasureText(text.Substring(renderFrom, text.Length - renderFrom));

        return width;
    }

    public class ImageCreation
    {
        public required ImageCreationConfiguration Configuration { get; set; }
        public required List<ImageCreationContent> ImageCreationContent { get; set; }
        public int LangCount { get; set; }
    }

    public class ImageCreationContent
    {
        public required string Header { get; set; }
        public required List<string> Content { get; set; }
        public string? Bottom { get; set; }
    }
}
