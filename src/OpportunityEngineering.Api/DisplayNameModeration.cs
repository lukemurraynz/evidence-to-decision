using System.Globalization;
using System.Text;

namespace OpportunityEngineering.Api;

/// <summary>
/// Sanitizes a participant-supplied display name before it's embedded in a token claim. This
/// is spoofing/hygiene defense, not a profanity filter. Word-list-based content moderation has
/// well-known false-positive/false-negative and localization problems disproportionate to a
/// display name only the participant themselves currently ever sees. What this does defend
/// against: bidi-override characters that make a name render differently than its actual
/// content (a known display-name spoofing vector), other invisible formatting characters used
/// to pad or disguise a name, and control characters that could break single-line rendering.
/// </summary>
public static class DisplayNameModeration
{
    public static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastAppendedWasSpace = false;
        foreach (var character in value)
        {
            if (char.IsControl(character) || IsFormattingCharacter(character))
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (builder.Length > 0 && !lastAppendedWasSpace)
                {
                    builder.Append(' ');
                    lastAppendedWasSpace = true;
                }
                continue;
            }

            builder.Append(character);
            lastAppendedWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }

    private static bool IsFormattingCharacter(char character) =>
        CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format;
}
