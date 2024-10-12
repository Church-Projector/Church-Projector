using Avalonia.Platform;
using ChurchProjector.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ChurchProjector.Classes;

public class Bible
{
    public required string Filename { get; set; }
    public required string Title { get; set; }
    public required List<Book> Books { get; set; }

    public static Bible LoadFromFile(string filename)
    {
        if (Path.GetExtension(filename).Equals(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return LoadFromFileFromXml(filename);
        }
        if (Path.GetExtension(filename).Equals(".spb", StringComparison.OrdinalIgnoreCase))
        {
            return LoadFromFileFromSpb(filename);
        }
        throw new InvalidOperationException("Unknown file format.");
    }

    private static Bible LoadFromFileFromXml(string filename)
    {
        XmlDocument xmlDocument = new();
        xmlDocument.Load(filename);

        return new Bible()
        {
            Filename = filename,
            Title = (xmlDocument.DocumentElement.SelectSingleNode("/XMLBIBLE/INFORMATION/title")
                ?? xmlDocument.DocumentElement.SelectSingleNode("/XMLBIBLE").Attributes.GetNamedItem("biblename")).InnerText,
            Books = xmlDocument.DocumentElement.SelectNodes("/XMLBIBLE/BIBLEBOOK").Cast<XmlNode>().Select(book =>
            {
                XmlNode? bNumberStr = book.Attributes?.GetNamedItem("bnumber");
                if (bNumberStr is null)
                {
                    throw new InvalidOperationException("Not all books contains a book number.");
                }
                if (!int.TryParse(bNumberStr.InnerText, out int bNumber))
                {
                    throw new InvalidOperationException("Not all books contains a valid book number.");
                }

                string? bName = book.Attributes?.GetNamedItem("bname")?.InnerText;
                if (string.IsNullOrEmpty(bName))
                {
                    bName = GetBookName(bName, bNumber, BookLanguage.German);
                }
                if (string.IsNullOrEmpty(bName))
                {
                    throw new InvalidOperationException("Not all books contains a book name.");
                }
                return new Book()
                {
                    Title = bName,
                    Number = bNumber,
                    Verses = book.SelectNodes("CHAPTER").Cast<XmlNode>().SelectMany(c => c.SelectNodes("VERS").Cast<XmlNode>().Select(v => new BibleVerse()
                    {
                        GlobalChapterNumber = int.Parse(c.Attributes!.GetNamedItem("cnumber")!.InnerText),
                        ChapterNumber = int.Parse(c.Attributes.GetNamedItem("cnumber")!.InnerText),
                        GlobalVerseNumber = int.Parse(v.Attributes!.GetNamedItem("vnumber")!.InnerText),
                        VerseNumber = int.Parse(v.Attributes.GetNamedItem("vnumber")!.InnerText),
                        Content = v.InnerText,
                    })).ToList()
                };
            }).ToList(),
        };
    }

    internal static string? GetBookName(string? bookName, int bookNumber, BookLanguage bookLanguage)
    {
        switch (bookLanguage)
        {
            case Enums.BookLanguage.SameAsBook:
                return bookName;
            case Enums.BookLanguage.English:
                using (Stream stream = AssetLoader.Open(new Uri("avares://ChurchProjector/Assets/books.en.txt")))
                {
                    using StreamReader sr = new(stream);
                    List<string> lines = sr.ReadToEnd().Split(Environment.NewLine).ToList();
                    if (lines.Count <= bookNumber)
                    {
                        return bookName;
                    }
                    return lines[bookNumber - 1];
                }
            case Enums.BookLanguage.German:
                using (Stream stream = AssetLoader.Open(new Uri("avares://ChurchProjector/Assets/books.de.txt")))
                {
                    using StreamReader sr = new(stream);
                    List<string> lines = sr.ReadToEnd().Split(Environment.NewLine).ToList();
                    if (lines.Count <= bookNumber)
                    {
                        return bookName;
                    }
                    return lines[bookNumber - 1];
                }
            case Enums.BookLanguage.Russian:
                using (Stream stream = AssetLoader.Open(new Uri("avares://ChurchProjector/Assets/books.ru.txt")))
                {
                    using StreamReader sr = new(stream);
                    List<string> lines = sr.ReadToEnd().Split(Environment.NewLine).ToList();
                    if (lines.Count <= bookNumber)
                    {
                        return bookName;
                    }
                    return lines[bookNumber - 1];
                }
        }
        return bookName;
    }

    private static Bible LoadFromFileFromSpb(string filename)
    {
        List<string> lines = [.. File.ReadAllLines(filename)];
        return new Bible()
        {
            Filename = filename,
            Title = lines.Single(x => x.StartsWith("##Title:"))["##Title:".Length..].Trim(),
            Books = lines.SkipWhile(x => x.StartsWith("#"))
            .TakeWhile(x => !x.All(y => y == '-'))
            .Select(x =>
            {
                string[] splittedBook = x.Split("\t", 3);
                List<string> versesFromBook = lines.Where(l => l.StartsWith($"B{splittedBook[0].PadLeft(3, '0')}")).ToList();
                Dictionary<int, bool> chapterContainsVerseZero = [];
                foreach (string verseFromBook in versesFromBook)
                {
                    int chapter = int.Parse(verseFromBook.Substring(5, 3));
                    if (chapterContainsVerseZero.TryGetValue(chapter, out bool result) && result)
                    {
                        continue;
                    }
                    chapterContainsVerseZero[chapter] = verseFromBook.Substring(9, 3) == "000";
                }
                return new Book()
                {
                    Number = int.Parse(splittedBook[0]),
                    Title = splittedBook[1],
                    Verses = versesFromBook
                    .Select(y =>
                    {
                        var splittedLine = y.Split("\t", 5);
                        int globalChapterNumber = int.Parse(splittedLine[0].Substring(5, 3));
                        bool globalVerseAddOne = chapterContainsVerseZero[globalChapterNumber];
                        return new BibleVerse()
                        {
                            GlobalChapterNumber = globalChapterNumber,
                            GlobalVerseNumber = int.Parse(splittedLine[0].Substring(9, 3)) + (globalVerseAddOne ? 1 : 0),
                            ChapterNumber = int.Parse(splittedLine[2]),
                            VerseNumber = int.Parse(splittedLine[3]),
                            Content = splittedLine[4],
                        };
                    }).ToList(),
                };
            }).ToList(),
        };
    }
}

public class Book
{
    public required string Title { get; set; }
    public string SearchTitle => Bible.GetBookName(Title, Number, GlobalConfig.JsonFile.Settings.BibleSettings.BookLanguage) ?? string.Empty;
    public required int Number { get; set; }
    public required List<BibleVerse> Verses { get; set; }
}

public class BibleVerse
{
    public required int ChapterNumber { get; set; }
    public required int VerseNumber { get; set; }
    public required int GlobalChapterNumber { get; set; }
    public required int GlobalVerseNumber { get; set; }

    public required string Content { get; set; }
}