using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace FatBaby.ModCode.Mechanics;

internal static class BurnHoverTip
{
    public static IHoverTip Get() => new HoverTip(
        new LocString("static_hover_tips", "BURN.title"),
        new LocString("static_hover_tips", "BURN.description"));
}
