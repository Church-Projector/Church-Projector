namespace ChurchProjector.Classes;
public record ExactVerse(string BookName, int BookNumber, int ChapterNumber, int VerseNumber, string Content)
{
    public string Position => $"{BookName} {ChapterNumber}, {VerseNumber}";
}
