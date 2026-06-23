using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace FatBaby.ModCode.Mechanics;

internal static class SplashHoverTip
{
    public static IHoverTip Get() => new HoverTip(
        new LocString("static_hover_tips", "SPLASH.title"),
        new LocString("static_hover_tips", "SPLASH.description"));
}
