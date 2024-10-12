using System.ComponentModel;

namespace ChurchProjector.Enums;

// TODO: Add same as app?
public enum BookLanguage
{
    [Description("Sprache aus der Datei")]
    SameAsBook = 0,
    [Description("Deutsch")]
    German = 1,
    [Description("Englisch")]
    English = 2,
    [Description("Russisch")]
    Russian = 3,
}
