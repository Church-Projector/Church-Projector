
using ChurchProjector.Lang;
using System.ComponentModel.DataAnnotations;

namespace ChurchProjector.Enums;

public enum Language
{
    [Display(Name = "Windows", ResourceType = typeof(Resources))]
    Windows = 0,
    [Display(Name = "German", ResourceType = typeof(Resources))]
    German = 1,
    [Display(Name = "English", ResourceType = typeof(Resources))]
    English = 2,
}
