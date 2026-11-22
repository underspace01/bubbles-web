using UnityEngine;

namespace Bubbles
{
    public static class Palette
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static readonly Color Deep = Hex("#123449"), Water = Hex("#4B8895"), Window = Hex("#F3F9FC"), Horizon = Hex("#294C5B"), Sky = Hex("#AECBD4"), Pink = Hex("#E8BDD8"), Mint = Hex("#B7DFD4"), Ink = Hex("#25465A"), Paper = Hex("#EFF6F6");
        public static readonly Color GardenSky = Hex("#32657E"), GardenHaze = Hex("#B3CABA"), Meadow = Hex("#557F6E"), Foliage = Hex("#244F43"), Sunlight = Hex("#F2DFB5");
    }
}
