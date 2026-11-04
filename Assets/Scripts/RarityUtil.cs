// RarityUtil.cs — Nadirlik yardimcilari (renk, isim, carpan)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public static class RarityUtil
{
    /// <summary>Fiyat carpani: 1x / 2x / 3x / 5x / 8x</summary>
    public static float GetMultiplier(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return 1f;
            case Rarity.Rare:      return 2f;
            case Rarity.Epic:      return 3f;
            case Rarity.Legendary: return 5f;
            case Rarity.Mythic:    return 8f;
            default:               return 1f;
        }
    }

    /// <summary>UI'da gosterilecek Turkce ad.</summary>
    public static string GetDisplayName(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return "Siradan";
            case Rarity.Rare:      return "Nadir";
            case Rarity.Epic:      return "Epik";
            case Rarity.Legendary: return "Efsanevi";
            case Rarity.Mythic:    return "Mitik";
            default:               return "Bilinmiyor";
        }
    }

    /// <summary>Katalog karti ve etiket rengi.</summary>
    public static Color GetColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return new Color(0.75f, 0.75f, 0.75f); // gri
            case Rarity.Rare:      return new Color(0.30f, 0.60f, 0.95f); // mavi
            case Rarity.Epic:      return new Color(0.65f, 0.35f, 0.90f); // mor
            case Rarity.Legendary: return new Color(0.95f, 0.70f, 0.20f); // altin
            case Rarity.Mythic:    return new Color(0.90f, 0.20f, 0.25f); // kirmizi
            default:               return Color.white;
        }
    }
}
