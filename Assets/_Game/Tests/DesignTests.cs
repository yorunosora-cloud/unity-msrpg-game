using NUnit.Framework;
using UnityEngine;

public class DesignTests
{
    // ════════════════════════════════════════════════════════════════════════
    // ContinentColors
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ContinentColors_Physics_IsBlue()
    {
        var c = ContinentColors.Of(Continent.Physics);
        Assert.AreEqual(0.169f, c.r, 0.01f);
        Assert.AreEqual(0.498f, c.g, 0.01f);
        Assert.AreEqual(1.000f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_Chemistry_IsScarlet()
    {
        var c = ContinentColors.Of(Continent.Chemistry);
        Assert.AreEqual(1.000f, c.r, 0.01f);
        Assert.AreEqual(0.090f, c.g, 0.01f);
        Assert.AreEqual(0.267f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_Biology_IsBrightGreen()
    {
        var c = ContinentColors.Of(Continent.Biology);
        Assert.AreEqual(0.239f, c.r, 0.01f);
        Assert.AreEqual(0.769f, c.g, 0.01f);
        Assert.AreEqual(0.153f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_EarthSci_IsTerracotta()
    {
        var c = ContinentColors.Of(Continent.EarthSci);
        Assert.AreEqual(0.769f, c.r, 0.01f);
        Assert.AreEqual(0.361f, c.g, 0.01f);
        Assert.AreEqual(0.125f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_Math_IsYellowGreen()
    {
        var c = ContinentColors.Of(Continent.Math);
        Assert.AreEqual(0.784f, c.r, 0.01f);
        Assert.AreEqual(0.824f, c.g, 0.01f);
        Assert.AreEqual(0.000f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_Info_IsNeonPurple()
    {
        var c = ContinentColors.Of(Continent.Info);
        Assert.AreEqual(0.667f, c.r, 0.01f);
        Assert.AreEqual(0.000f, c.g, 0.01f);
        Assert.AreEqual(1.000f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_Mesoria_IsGold()
    {
        var c = ContinentColors.Of(Continent.Mesoria);
        Assert.AreEqual(0.784f, c.r, 0.01f);
        Assert.AreEqual(0.659f, c.g, 0.01f);
        Assert.AreEqual(0.251f, c.b, 0.01f);
    }

    [Test]
    public void ContinentColors_AllContinents_ReturnNonWhite()
    {
        foreach (Continent cont in System.Enum.GetValues(typeof(Continent)))
        {
            var c = ContinentColors.Of(cont);
            Assert.AreNotEqual(Color.white, c, $"Continent {cont} returned Color.white (missing entry?)");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Rarity.DisplayColor + IsRainbow
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Rarity_N_IsGreen()
    {
        var c = Rarity.N.DisplayColor();
        Assert.AreEqual(0.298f, c.r, 0.01f);
        Assert.AreEqual(0.686f, c.g, 0.01f);
        Assert.AreEqual(0.314f, c.b, 0.01f);
    }

    [Test]
    public void Rarity_R_IsPurple()
    {
        var c = Rarity.R.DisplayColor();
        Assert.AreEqual(0.612f, c.r, 0.01f);
        Assert.AreEqual(0.153f, c.g, 0.01f);
        Assert.AreEqual(0.690f, c.b, 0.01f);
    }

    [Test]
    public void Rarity_SR_IsYellow()
    {
        var c = Rarity.SR.DisplayColor();
        Assert.AreEqual(1.000f, c.r, 0.01f);
        Assert.AreEqual(0.839f, c.g, 0.01f);
        Assert.AreEqual(0.000f, c.b, 0.01f);
    }

    [Test]
    public void Rarity_SSR_IsGold()
    {
        var c = Rarity.SSR.DisplayColor();
        Assert.AreEqual(1.000f, c.r, 0.01f);
        Assert.AreEqual(0.702f, c.g, 0.01f);
        Assert.AreEqual(0.000f, c.b, 0.01f);
        // SSR 황금은 SR 노란보다 g채널이 낮아 더 따뜻한 금색
        Assert.Less(c.g, Rarity.SR.DisplayColor().g);
    }

    [Test]
    public void Rarity_UR_FallbackColor_IsHotPink()
    {
        var c = Rarity.UR.DisplayColor();
        Assert.AreEqual(1.000f, c.r, 0.01f);
        Assert.AreEqual(0.000f, c.g, 0.01f);
        Assert.AreEqual(0.502f, c.b, 0.01f);
    }

    [Test]
    public void Rarity_UR_IsRainbow()
    {
        Assert.IsTrue(Rarity.UR.IsRainbow());
        Assert.IsFalse(Rarity.SSR.IsRainbow());
    }
}
