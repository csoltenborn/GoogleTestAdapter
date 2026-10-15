using System;
using System.Drawing;
using System.Globalization;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    /// <summary>
    /// The colors of the release notes dialog and its HTML page, taken from Visual Studio's current theme.
    /// </summary>
    public class ReleaseNotesTheme
    {
        public Color Background { get; set; }
        public Color Text { get; set; }
        public Color Link { get; set; }
        public Color ButtonBackground { get; set; }
        public Color ButtonText { get; set; }
        public Color ButtonBorder { get; set; }

        // e.g. if the colors of Visual Studio's theme can not be determined
        public static ReleaseNotesTheme Default => new ReleaseNotesTheme
        {
            Background = SystemColors.Window,
            Text = SystemColors.WindowText,
            Link = SystemColors.HotTrack,
            ButtonBackground = SystemColors.Control,
            ButtonText = SystemColors.ControlText,
            ButtonBorder = SystemColors.ControlDark
        };

        /// <summary>
        /// Must be called on Visual Studio's UI thread.
        /// </summary>
        public static ReleaseNotesTheme FromVisualStudio()
        {
            try
            {
                return new ReleaseNotesTheme
                {
                    Background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey),
                    Text = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey),
                    Link = VSColorTheme.GetThemedColor(EnvironmentColors.ControlLinkTextColorKey),
                    ButtonBackground = VSColorTheme.GetThemedColor(CommonControlsColors.ButtonColorKey),
                    ButtonText = VSColorTheme.GetThemedColor(CommonControlsColors.ButtonTextColorKey),
                    ButtonBorder = VSColorTheme.GetThemedColor(CommonControlsColors.ButtonBorderColorKey)
                };
            }
            catch (Exception)
            {
                return Default;
            }
        }

        // for code, distinguishable from the background in light and dark themes
        public Color CodeBackground => Blend(Background, Text, 0.1);

        public string ToCss()
        {
            return "html { " +
                   // Internet Explorer's scroll bars, which would be light in dark themes
                   $"scrollbar-base-color: {ToCss(Background)}; scrollbar-face-color: {ToCss(CodeBackground)}; " +
                   $"scrollbar-track-color: {ToCss(Background)}; scrollbar-arrow-color: {ToCss(Text)}; }}" +
                   $"body {{ background-color: {ToCss(Background)}; color: {ToCss(Text)}; " +
                   "font-family: 'Segoe UI', sans-serif; font-size: 10pt; margin: 16px; }" +
                   $"a {{ color: {ToCss(Link)}; }}" +
                   $"code, pre {{ background-color: {ToCss(CodeBackground)}; font-family: Consolas, monospace; }}" +
                   "pre { padding: 8px; }";
        }

        public static string ToCss(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        private static Color Blend(Color color, Color other, double amountOfOther)
        {
            int Mix(int a, int b) => (int)Math.Round(a + (b - a) * amountOfOther);
            return Color.FromArgb(Mix(color.R, other.R), Mix(color.G, other.G), Mix(color.B, other.B));
        }
    }
}
