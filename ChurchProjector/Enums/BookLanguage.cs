using ChurchProjector.Lang;
using System.ComponentModel.DataAnnotations;

namespace ChurchProjector.Enums;

// TODO: Add same as app?
public enum BookLanguage
{
    [Display(Name = "SameAsBible", ResourceType = typeof(Resources))]
    SameAsBible = 0,
    [Display(Name = "German", ResourceType = typeof(Resources))]
    German = 1,
    [Display(Name = "English", ResourceType = typeof(Resources))]
    English = 2,
    [Display(Name = "Russian", ResourceType = typeof(Resources))]
    Russian = 3,
}
