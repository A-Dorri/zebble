namespace Zebble
{
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Olive;

    partial class Font
    {
        /// <summary>
        /// Allows you to dynamically upscale or downscale the font sizes across the whole application.
        /// The native renderers will multiply the font size by this to find the effective rendering size.
        /// </summary>
        public static float FontSizeScale { get; set; } = 1;

        static readonly ConcurrentDictionary<string, float> TextWidthCache = new();
        static readonly ConcurrentDictionary<string, float> TextHeightCache = new();
        static readonly ConcurrentDictionary<string, float> LineHeightCache = new();
        static readonly ConcurrentDictionary<string, float> AutomaticExtraTopPaddings = new();

        /// <summary>
        /// Multiplies the font size by the FontSizeScale.
        /// </summary>
        public float EffectiveSize => GetScaledFontSize(FontSizeScale * Size);

        public float GetUnwantedExtraTopPadding()
        {
            // TODO: It varies for each font and each platform.
            // The most reliable way is to actually render the text and receive it
            // in an image, then process it to see how much the margin is.
            // This can then be cached in a file (and memory) to speed up the future runs.

            var key = ToString();
            if (key.IsEmpty()) return 0;

            return AutomaticExtraTopPaddings.GetOrAdd(key, () => Thread.UI.Run(() => CalculateAutomaticExtraTopPadding()));
        }

        public float GetTextWidth(string text)
        {
            if (text.IsEmpty()) return 0;
            var key = ToString() + "|" + text;
            return TextWidthCache.GetOrAdd(key,
                () => Thread.UI.Run(() => 1 + (float)Math.Ceiling(CalculateTextWidth(text.OrEmpty()))));
        }

        public float GetTextHeight(float width, string text)
        {
            if (width == 0) return 0;
            if (text.IsEmpty()) text = "Tg";
            var key = ToString() + "|" + text + "||" + width;

            if (TextHeightCache.TryGetValue(key, out var result)) return result;

            // Definitely single line?
            if (text.Sum(x => x.IsLower() ? 0.3 : 0.5) * GetLineHeight() < 0.7 * width)
                return TextHeightCache[key] = GetLineHeight();

            result = Thread.UI.Run(() => 1 + (float)Math.Ceiling(CalculateTextHeight(width, text.OrEmpty())));
            if (result != 0 || EffectiveSize == 0)
                return TextHeightCache[key] = result;

            Debug.WriteLine("Font problem!!! Text height was calculated as zero for " + key);
            return 0;
        }

        public float GetLineHeight() => LineHeightCache.GetOrAdd(ToString(), CalculateFontLineHeight);
    }
}