using OpportunityEngineering.Api;

namespace OpportunityEngineering.ApiTests;

[TestClass]
public sealed class DisplayNameModerationTests
{
    private const string RightToLeftOverride = "‮";
    private const string ZeroWidthSpace = "​";
    private const string ZeroWidthNonJoiner = "‌";

    [TestMethod]
    public void SanitizeTrimsLeadingAndTrailingWhitespace()
    {
        Assert.AreEqual("Riley", DisplayNameModeration.Sanitize("   Riley   "));
    }

    [TestMethod]
    public void SanitizeCollapsesInternalWhitespaceRuns()
    {
        Assert.AreEqual("Riley Jordan", DisplayNameModeration.Sanitize("Riley     Jordan"));
    }

    [TestMethod]
    public void SanitizeStripsControlCharacters()
    {
        Assert.AreEqual("Riley", DisplayNameModeration.Sanitize("Riley\n"));
    }

    [TestMethod]
    public void SanitizeStripsBidiOverrideCharactersUsedToSpoofRenderedText()
    {
        // U+202E (Right-to-Left Override) is a real-world display-name spoofing technique:
        // it makes the characters that follow render in reverse order.
        Assert.AreEqual("Riley", DisplayNameModeration.Sanitize(RightToLeftOverride + "Riley"));
        Assert.AreEqual("Riley", DisplayNameModeration.Sanitize("Ri" + RightToLeftOverride + "ley"));
    }

    [TestMethod]
    public void SanitizeStripsZeroWidthCharactersUsedToPadOrDisguiseAName()
    {
        Assert.AreEqual(
            "Riley",
            DisplayNameModeration.Sanitize("Ri" + ZeroWidthSpace + "ley" + ZeroWidthNonJoiner));
    }

    [TestMethod]
    public void SanitizeOfOnlyWhitespaceAndControlCharactersIsEmpty()
    {
        Assert.AreEqual(string.Empty, DisplayNameModeration.Sanitize("   " + ZeroWidthSpace + "  "));
    }

    [TestMethod]
    public void SanitizePreservesOrdinaryUnicodeLetters()
    {
        Assert.AreEqual("Renée Müller", DisplayNameModeration.Sanitize("Renée Müller"));
    }
}
