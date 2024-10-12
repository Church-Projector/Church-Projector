using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ChurchProjector.Classes;
public class Song : ObservableObject
{
    public Dictionary<string, string?> Tags { get; set; } = [];
    public string? FilePath { get; set; }
    public string? Title { get => GetTag("Title") ?? Path.GetFileNameWithoutExtension(FilePath); set => SetTag("Title", value); }
    public string? QuickFind { get => GetTag("QuickFind"); set => SetTag("QuickFind", value); }
    public string? ChurchSongID { get => GetTag("ChurchSongID"); set => SetTag("ChurchSongID", value); }
    public string? VerseOrderStr { get => GetTag("VerseOrder"); set => SetTag("VerseOrder", value); }
    public int LangCount
    {
        get
        {
            string? langTagStr = GetTag("LangCount");
            if (!string.IsNullOrWhiteSpace(langTagStr))
            {
                if (int.TryParse(langTagStr.Trim(), out int result))
                {
                    return result;
                }
                else
                {
                    throw new InvalidOperationException("The LangCount tag doesn't contain a valid number.");
                }
            }
            return 1;
        }
        set => SetTag("LangCount", value.ToString());
    }
    public List<string> VerseOrder => [.. (GetTag("VerseOrder") ?? string.Empty).Split(",", StringSplitOptions.RemoveEmptyEntries)];
    public ObservableCollection<Verse> Verses { get; set; } = [];

    public string LowerContent => string.Join(Environment.NewLine, Verses.SelectMany(v => v.Lines.Select(l => l.ToLowerInvariant())));

    public static Song LoadFromFile(string filename)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        List<string> lines = [.. (File.ReadAllLines(filename, GetEncoding(filename)))];


        List<string> verseLines = lines.Where(x => !x.StartsWith('#')).ToList();
        List<Verse> verses = [];
        Verse? currentVerse = null;
        foreach (string line in verseLines)
        {
            if (line == "---")
            {
                if (currentVerse is null)
                {
                    continue;
                }
                verses.Add(currentVerse);
                currentVerse = null;
            }
            else if (currentVerse is null)
            {
                currentVerse = new()
                {
                    Title = line,
                };
                List<string> validVerseTitles = ["vers", "verse", "refrain", "strophe", "chorus", "bridge"];
                if (!validVerseTitles.Any(vvt => line.StartsWith(vvt, StringComparison.OrdinalIgnoreCase)))
                {
                    currentVerse.Lines.Add(line);
                }
            }
            else
            {
                currentVerse.Lines.Add(line);
            }
        }
        if (currentVerse is not null)
        {
            verses.Add(currentVerse);
        }

        // Some have an empty verse at the end
        verses = verses.Where(x => !x.Lines.All(string.IsNullOrWhiteSpace)).ToList();

        return new Song()
        {
            FilePath = filename,
            Tags = lines.Where(X => X.StartsWith('#')).ToDictionary(x => x.Substring(1).Split('=', 2)[0], x => x.Split('=', 2).Skip(1).FirstOrDefault()),
            Verses = new ObservableCollection<Verse>(verses),
        };
    }

    public void SaveSong()
    {
        StringBuilder sb = new();
        foreach (KeyValuePair<string, string?> tag in Tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Value))
            {
                continue;
            }
            sb.AppendLine($"#{tag.Key}={tag.Value}");
        }
        foreach (Verse verse in Verses)
        {
            sb.AppendLine("---");
            if (verse.Title != verse.Lines.FirstOrDefault())
            {
                sb.AppendLine(verse.Title);
            }
            foreach (string line in verse.Lines)
            {
                sb.AppendLine(line);
            }
        }

        File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static Song Create()
    {
        return new();
    }

    private static Encoding GetEncoding(string filename)
    {
        // Read the BOM
        var bom = new byte[4];
        using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            file.Read(bom, 0, 4);
        }

        // Analyze the BOM
        if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
        {
            return Encoding.UTF7;
        }

        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
        {
            return Encoding.UTF8;
        }

        if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0)
        {
            return Encoding.UTF32; //UTF-32LE
        }

        if (bom[0] == 0xff && bom[1] == 0xfe)
        {
            return Encoding.Unicode; //UTF-16LE
        }

        if (bom[0] == 0xfe && bom[1] == 0xff)
        {
            return Encoding.BigEndianUnicode; //UTF-16BE
        }

        if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
        {
            return new UTF32Encoding(true, true);  //UTF-32BE
        }

        return Encoding.GetEncoding(1252);
    }

    private string? GetTag(string tagName)
    {
        if (Tags.TryGetValue(tagName, out string? value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        return null;
    }

    private void SetTag(string tagName, string? value)
    {
        Tags[tagName] = value;
    }
}

public class Verse
{
    public string Title { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = [];

    public string Content
    {
        get => string.Join(Environment.NewLine, Lines);
        set => Lines = value.Split(Environment.NewLine).ToList();
    }

    public override string ToString()
    {
        return Content;
    }
}

public static class SongExtensions
{
    public static List<(string title, Bitmap bitmap, bool isOverflowing)> GetImages(this Song song)
    {
        List<KeyValuePair<string, (string Title, Bitmap bitmap, bool isOverflowing)>> dic = song.Verses.Select(verse =>
    {
        var renderResult = DrawingHelper.GetImage(new DrawingHelper.ImageCreation()
        {
            Configuration = GlobalConfig.JsonFile.Settings.DisplayConfiguration.SongConfiguration,
            LangCount = song.LangCount,
            ImageCreationContent =
        [
            new()
                {
                    Header = song.ChurchSongID,
                    Content = verse.Lines,
                }
        ]
        });
        return new KeyValuePair<string, (string Title, Bitmap bitmap, bool isOverflowing)>(verse.Title, (verse.Title, renderResult.bitmap, renderResult.isOverflowing));
    }).ToList();

        if (song.VerseOrder is
            {
                Count: > 0
            } && song.VerseOrder.All(vo => song.Verses.Any(v => v.Title == vo))
            && song.Verses.All(v => song.VerseOrder.Any(vo => vo == v.Title)))
        {
            return song.VerseOrder.SelectMany(x => dic.Where(d => d.Key == x).Select(y => y.Value)).ToList();
        }
        return dic.Select(x => x.Value).ToList();
    }
}
