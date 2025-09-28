namespace Adaplio.Frontend.Theme;

/// <summary>
/// Comprehensive design token system - single source of truth for all design decisions.
/// Based on 2025 design system principles with dark mode intentionality.
/// </summary>
public static class DesignTokens
{
    public static class Colors
    {
        public static class Light
        {
            // 2025 Color System - Energetic Orange Primary & Deep Charcoal Base
            public const string Primary = "#FF7A00";
            public const string OnPrimary = "#FFFFFF";
            public const string PrimaryContainer = "#FFF4EC";
            public const string OnPrimaryContainer = "#2D1B00";

            public const string Secondary = "#1E2837";
            public const string OnSecondary = "#FFFFFF";
            public const string SecondaryContainer = "#F5F5F5";
            public const string OnSecondaryContainer = "#1E2837";

            public const string Tertiary = "#00BFA5";
            public const string OnTertiary = "#FFFFFF";
            public const string TertiaryContainer = "#E0F7F4";
            public const string OnTertiaryContainer = "#003530";

            // State Colors - Semantic system with 2025 accents
            public const string Success = "#00BFA5";
            public const string OnSuccess = "#FFFFFF";
            public const string SuccessContainer = "#E0F7F4";
            public const string OnSuccessContainer = "#003530";

            public const string Warning = "#FF7A00";
            public const string OnWarning = "#FFFFFF";
            public const string WarningContainer = "#FFF4EC";
            public const string OnWarningContainer = "#2D1B00";

            public const string Error = "#F04438";
            public const string OnError = "#FFFFFF";
            public const string ErrorContainer = "#FFEAE9";
            public const string OnErrorContainer = "#2D0A0A";

            public const string Info = "#00BFA5";
            public const string OnInfo = "#FFFFFF";
            public const string InfoContainer = "#E0F7F4";
            public const string OnInfoContainer = "#003530";

            // Surface & Background - Warm Light Grey foundation
            public const string Background = "#F5F5F5";
            public const string OnBackground = "#1E2837";
            public const string Surface = "#FFFFFF";
            public const string OnSurface = "#1E2837";
            public const string SurfaceVariant = "#F5F5F5";
            public const string OnSurfaceVariant = "#1E2837";
            public const string SurfaceTint = "#FF7A00";

            public const string InverseSurface = "#2A343E";
            public const string InverseOnSurface = "#F2F4F7";
            public const string InversePrimary = "#7AB8FF";

            // Neutral Scale - 2025 Warm Grey System
            public const string Neutral0 = "#FFFFFF";
            public const string Neutral50 = "#FAFAFA";
            public const string Neutral100 = "#F5F5F5";
            public const string Neutral200 = "#EEEEEE";
            public const string Neutral300 = "#E0E0E0";
            public const string Neutral400 = "#BDBDBD";
            public const string Neutral500 = "#9E9E9E";
            public const string Neutral600 = "#757575";
            public const string Neutral700 = "#616161";
            public const string Neutral800 = "#424242";
            public const string Neutral900 = "#1E2837";

            // Interactive states
            public const string Outline = "#D0D5DD";
            public const string OutlineVariant = "#EAECF0";
            public const string Scrim = "rgba(16, 24, 40, 0.5)";
            public const string Shadow = "rgba(16, 24, 40, 0.1)";
        }

        public static class Dark
        {
            // 2025 Dark Theme - Adjusted Orange & Teal
            public const string Primary = "#FF9D47";
            public const string OnPrimary = "#1E2837";
            public const string PrimaryContainer = "#CC5500";
            public const string OnPrimaryContainer = "#FFF4EC";

            public const string Secondary = "#F5F5F5";
            public const string OnSecondary = "#1E2837";
            public const string SecondaryContainer = "#424242";
            public const string OnSecondaryContainer = "#F5F5F5";

            public const string Tertiary = "#26E5CC";
            public const string OnTertiary = "#1E2837";
            public const string TertiaryContainer = "#008570";
            public const string OnTertiaryContainer = "#E0F7F4";

            // State Colors - Dark mode optimized
            public const string Success = "#26E5CC";
            public const string OnSuccess = "#1E2837";
            public const string SuccessContainer = "#008570";
            public const string OnSuccessContainer = "#E0F7F4";

            public const string Warning = "#FF9D47";
            public const string OnWarning = "#1E2837";
            public const string WarningContainer = "#CC5500";
            public const string OnWarningContainer = "#FFF4EC";

            public const string Error = "#FF6B6B";
            public const string OnError = "#FFFFFF";
            public const string ErrorContainer = "#CC1A1A";
            public const string OnErrorContainer = "#FFEAE9";

            public const string Info = "#26E5CC";
            public const string OnInfo = "#1E2837";
            public const string InfoContainer = "#008570";
            public const string OnInfoContainer = "#E0F7F4";

            // Surface & Background - Subtle depth
            public const string Background = "#070B0F";
            public const string OnBackground = "#E6EAF2";
            public const string Surface = "#0E1116";
            public const string OnSurface = "#E6EAF2";
            public const string SurfaceVariant = "#1A2129";
            public const string OnSurfaceVariant = "#9CA3AF";
            public const string SurfaceTint = "#7AB8FF";

            public const string InverseSurface = "#E6EAF2";
            public const string InverseOnSurface = "#1A2129";
            public const string InversePrimary = "#2E90FA";

            // Neutral Scale - Dark mode hierarchy
            public const string Neutral0 = "#000000";
            public const string Neutral50 = "#070B0F";
            public const string Neutral100 = "#0E1116";
            public const string Neutral200 = "#1A2129";
            public const string Neutral300 = "#2A343E";
            public const string Neutral400 = "#3E4A56";
            public const string Neutral500 = "#54606F";
            public const string Neutral600 = "#6B7485";
            public const string Neutral700 = "#818B9A";
            public const string Neutral800 = "#A3ABB8";
            public const string Neutral900 = "#E6EAF2";

            // Interactive states
            public const string Outline = "#54606F";
            public const string OutlineVariant = "#3E4A56";
            public const string Scrim = "rgba(7, 11, 15, 0.7)";
            public const string Shadow = "rgba(20, 24, 30, 0.24)";
        }
    }

    /// <summary>
    /// Consistent spacing scale - 4px base unit
    /// </summary>
    public static class Spacing
    {
        public const int XXS = 2;   // 2px
        public const int XS = 4;    // 4px
        public const int SM = 8;    // 8px
        public const int MD = 12;   // 12px
        public const int Base = 16; // 16px
        public const int LG = 20;   // 20px
        public const int XL = 24;   // 24px
        public const int XXL = 32;  // 32px
        public const int XXXL = 40; // 40px
        public const int XXXXL = 48; // 48px
        public const int XXXXXL = 64; // 64px
    }

    /// <summary>
    /// Border radius scale for 2025 modern roundness
    /// </summary>
    public static class BorderRadius
    {
        public const int None = 0;
        public const int XS = 4;
        public const int SM = 8;
        public const int MD = 12;
        public const int LG = 16;   // Primary radius for modern cards
        public const int XL = 20;
        public const int XXL = 24;
        public const int XXXL = 32;
        public const int Round = 9999; // Pill shape
    }

    /// <summary>
    /// Elevation system with subtle depth
    /// </summary>
    public static class Elevation
    {
        public static class Light
        {
            public const string Level0 = "none";
            public const string Level1 = "0 1px 2px rgba(16, 24, 40, 0.05)";
            public const string Level2 = "0 1px 3px rgba(16, 24, 40, 0.1), 0 1px 2px rgba(16, 24, 40, 0.06)";
            public const string Level3 = "0 4px 8px -2px rgba(16, 24, 40, 0.1), 0 2px 4px -2px rgba(16, 24, 40, 0.06)";
            public const string Level4 = "0 12px 16px -4px rgba(16, 24, 40, 0.08), 0 4px 6px -2px rgba(16, 24, 40, 0.03)";
            public const string Level5 = "0 20px 24px -4px rgba(16, 24, 40, 0.08), 0 8px 8px -4px rgba(16, 24, 40, 0.03)";
        }

        public static class Dark
        {
            public const string Level0 = "none";
            public const string Level1 = "0 1px 2px rgba(20, 24, 30, 0.24)";
            public const string Level2 = "0 1px 3px rgba(20, 24, 30, 0.32), 0 1px 2px rgba(20, 24, 30, 0.24)";
            public const string Level3 = "0 4px 8px -2px rgba(20, 24, 30, 0.32), 0 2px 4px -2px rgba(20, 24, 30, 0.24)";
            public const string Level4 = "0 12px 16px -4px rgba(20, 24, 30, 0.40), 0 4px 6px -2px rgba(20, 24, 30, 0.24)";
            public const string Level5 = "0 20px 24px -4px rgba(20, 24, 30, 0.40), 0 8px 8px -4px rgba(20, 24, 30, 0.24)";
        }
    }

    /// <summary>
    /// Typography system with variable font support
    /// </summary>
    public static class Typography
    {
        public static class FontFamily
        {
            public const string Primary = "'Inter', 'Inter Variable', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif";
            public const string Display = "'Poppins', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif";
            public const string Mono = "JetBrains Mono Variable, JetBrains Mono, 'SF Mono', Monaco, Inconsolata, 'Roboto Mono', Consolas, 'Courier New', monospace";
        }

        public static class FontSize
        {
            public const string XS = "0.75rem";    // 12px
            public const string SM = "0.875rem";   // 14px
            public const string Base = "1rem";     // 16px
            public const string LG = "1.125rem";   // 18px
            public const string XL = "1.25rem";    // 20px
            public const string XXL = "1.5rem";    // 24px
            public const string XXXL = "1.75rem";  // 28px
            public const string XXXXL = "2rem";    // 32px
            public const string XXXXXL = "2.5rem"; // 40px
            public const string Display = "3rem";  // 48px
        }

        public static class FontWeight
        {
            public const int Light = 300;
            public const int Normal = 400;
            public const int Medium = 500;
            public const int Semibold = 600;
            public const int Bold = 700;
            public const int Extrabold = 800;
        }

        public static class LineHeight
        {
            public const double Tight = 1.25;
            public const double Snug = 1.375;
            public const double Normal = 1.5;
            public const double Relaxed = 1.625;
            public const double Loose = 2.0;
        }

        public static class LetterSpacing
        {
            public const string Tight = "-0.025em";
            public const string Normal = "0";
            public const string Wide = "0.025em";
            public const string Wider = "0.05em";
            public const string Widest = "0.1em";
        }
    }

    /// <summary>
    /// Motion system - purposeful transitions
    /// </summary>
    public static class Motion
    {
        public static class Duration
        {
            public const int Fast = 120;      // 120ms
            public const int Base = 200;      // 200ms
            public const int Gentle = 280;    // 280ms
            public const int Slow = 400;      // 400ms
        }

        public static class Easing
        {
            public const string Standard = "cubic-bezier(0.4, 0.0, 0.2, 1)";
            public const string Decelerated = "cubic-bezier(0.0, 0.0, 0.2, 1)";
            public const string Accelerated = "cubic-bezier(0.4, 0.0, 1, 1)";
            public const string Emphasized = "cubic-bezier(0.2, 0.0, 0, 1)";
        }
    }

    /// <summary>
    /// Responsive breakpoints
    /// </summary>
    public static class Breakpoints
    {
        public const int SM = 600;   // Small devices (landscape phones)
        public const int MD = 900;   // Medium devices (tablets)
        public const int LG = 1200;  // Large devices (desktops)
        public const int XL = 1536;  // Extra large devices
    }

    /// <summary>
    /// Focus system for WCAG 2.2 compliance
    /// </summary>
    public static class Focus
    {
        public const int RingWidth = 3;
        public const string RingOffset = "2px";
        public const string RingColorLight = "#2E90FA";
        public const string RingColorDark = "#7AB8FF";
    }
}