using ChurchProjector.Lang;
using System.ComponentModel.DataAnnotations;

namespace ChurchProjector.Enums;

// TODO: Add same as app?
public enum BookLanguage
{
    [Display(Name = "BookLanguage_SameAsBible", ResourceType = typeof(Resources))]
    SameAsBible = 0,
    [Display(Name = "BookLanguage_German", ResourceType = typeof(Resources))]
    German = 1,
    [Display(Name = "BookLanguage_English", ResourceType = typeof(Resources))]
    English = 2,
    [Display(Name = "BookLanguage_Russian", ResourceType = typeof(Resources))]
    Russian = 3,
}
