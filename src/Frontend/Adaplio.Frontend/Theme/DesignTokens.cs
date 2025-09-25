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
            // Brand Colors - Azure Blue foundation
            public const string Primary = "#2E90FA";
            public const string OnPrimary = "#FFFFFF";
            public const string PrimaryContainer = "#E6F4FF";
            public const string OnPrimaryContainer = "#001F3D";

            public const string Secondary = "#7A7D87";
            public const string OnSecondary = "#FFFFFF";
            public const string SecondaryContainer = "#F4F5F6";
            public const string OnSecondaryContainer = "#2D3142";

            public const string Tertiary = "#5C6AC4";
            public const string OnTertiary = "#FFFFFF";
            public const string TertiaryContainer = "#E8EAFF";
            public const string OnTertiaryContainer = "#1A1B3A";

            // State Colors - Semantic system
            public const string Success = "#12B76A";
            public const string OnSuccess = "#FFFFFF";
            public const string SuccessContainer = "#E6F7F1";
            public const string OnSuccessContainer = "#002818";

            public const string Warning = "#FDB022";
            public const string OnWarning = "#000000";
            public const string WarningContainer = "#FFF8E6";
            public const string OnWarningContainer = "#2D1B00";

            public const string Error = "#F04438";
            public const string OnError = "#FFFFFF";
            public const string ErrorContainer = "#FFEAE9";
            public const string OnErrorContainer = "#2D0A0A";

            public const string Info = "#2E90FA";
            public const string OnInfo = "#FFFFFF";
            public const string InfoContainer = "#E6F4FF";
            public const string OnInfoContainer = "#001F3D";

            // Surface & Background - Layered system
            public const string Background = "#FCFCFD";
            public const string OnBackground = "#101828";
            public const string Surface = "#FFFFFF";
            public const string OnSurface = "#101828";
            public const string SurfaceVariant = "#F9FAFB";
            public const string OnSurfaceVariant = "#344054";
            public const string SurfaceTint = "#2E90FA";

            public const string InverseSurface = "#2A343E";
            public const string InverseOnSurface = "#F2F4F7";
            public const string InversePrimary = "#7AB8FF";

            // Neutral Scale - Tailored gray ramp
            public const string Neutral0 = "#FFFFFF";
            public const string Neutral50 = "#F9FAFB";
            public const string Neutral100 = "#F2F4F7";
            public const string Neutral200 = "#EAECF0";
            public const string Neutral300 = "#D0D5DD";
            public const string Neutral400 = "#98A2B3";
            public const string Neutral500 = "#667085";
            public const string Neutral600 = "#475467";
            public const string Neutral700 = "#344054";
            public const string Neutral800 = "#1D2939";
            public const string Neutral900 = "#101828";

            // Interactive states
            public const string Outline = "#D0D5DD";
            public const string OutlineVariant = "#EAECF0";
            public const string Scrim = "rgba(16, 24, 40, 0.5)";
            public const string Shadow = "rgba(16, 24, 40, 0.1)";
        }

        public static class Dark
        {
            // Brand Colors - Contrast-corrected for dark
            public const string Primary = "#7AB8FF";
            public const string OnPrimary = "#0E1116";
            public const string PrimaryContainer = "#1A365D";
            public const string OnPrimaryContainer = "#B8E6FF";

            public const string Secondary = "#9CA3AF";
            public const string OnSecondary = "#111827";
            public const string SecondaryContainer = "#374151";
            public const string OnSecondaryContainer = "#D1D7E0";

            public const string Tertiary = "#8B95FF";
            public const string OnTertiary = "#1A1B3A";
            public const string TertiaryContainer = "#2D3142";
            public const string OnTertiaryContainer = "#C7CCFF";

            // State Colors - Dark mode optimized
            public const string Success = "#22C55E";
            public const string OnSuccess = "#0F1011";
            public const string SuccessContainer = "#14532D";
            public const string OnSuccessContainer = "#BBF7D0";

            public const string Warning = "#F59E0B";
            public const string OnWarning = "#0F1011";
            public const string WarningContainer = "#78350F";
            public const string OnWarningContainer = "#FDE68A";

            public const string Error = "#EF4444";
            public const string OnError = "#FFFFFF";
            public const string ErrorContainer = "#7F1D1D";
            public const string OnErrorContainer = "#FECACA";

            public const string Info = "#7AB8FF";
            public const string OnInfo = "#0E1116";
            public const string InfoContainer = "#1A365D";
            public const string OnInfoContainer = "#B8E6FF";

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
    /// Border radius scale for cohesive roundness
    /// </summary>
    public static class BorderRadius
    {
        public const int None = 0;
        public const int XS = 4;
        public const int SM = 6;
        public const int MD = 8;
        public const int LG = 12;
        public const int XL = 16;
        public const int XXL = 20;
        public const int XXXL = 24;
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
            public const string Primary = "Inter Variable, Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
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