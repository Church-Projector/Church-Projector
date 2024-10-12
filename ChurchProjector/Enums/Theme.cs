using ChurchProjector.Lang;
using System.ComponentModel.DataAnnotations;

namespace ChurchProjector.Enums;

public enum Theme
{
    [Display(Name = "Theme_Light", ResourceType = typeof(Resources))]
    Light = 0,
    [Display(Name = "Theme_Dark", ResourceType = typeof(Resources))]
    Dark = 1,
}
