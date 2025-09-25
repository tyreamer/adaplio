namespace Adaplio.Frontend.Theme;

public static class DesignTokens
{
    public static class Colors
    {
        public static class Brand
        {
            public const string Primary = "#2E90FA";
            public const string PrimaryLight = "#7AB8FF";
            public const string OnPrimary = "#FFFFFF";
            public const string OnPrimaryDark = "#0A1829";
        }

        public static class Light
        {
            public const string Primary = "#2E90FA";
            public const string OnPrimary = "#FFFFFF";
            public const string Secondary = "#7A7D87";
            public const string OnSecondary = "#FFFFFF";
            public const string Success = "#12B76A";
            public const string OnSuccess = "#FFFFFF";
            public const string Warning = "#FDB022";
            public const string OnWarning = "#000000";
            public const string Error = "#F04438";
            public const string OnError = "#FFFFFF";
            public const string Surface = "#FFFFFF";
            public const string OnSurface = "#1A1D29";
            public const string Background = "#FAFBFC";
            public const string OnBackground = "#1A1D29";

            // Neutral scale
            public const string Neutral50 = "#F9FAFB";
            public const string Neutral100 = "#F2F4F7";
            public const string Neutral200 = "#E4E7EC";
            public const string Neutral300 = "#D0D5DD";
            public const string Neutral500 = "#667085";
            public const string Neutral700 = "#344054";
            public const string Neutral900 = "#101828";
        }

        public static class Dark
        {
            public const string Primary = "#7AB8FF";
            public const string OnPrimary = "#0A1829";
            public const string Secondary = "#9CA3AF";
            public const string OnSecondary = "#000000";
            public const string Success = "#10B981";
            public const string OnSuccess = "#000000";
            public const string Warning = "#F59E0B";
            public const string OnWarning = "#000000";
            public const string Error = "#EF4444";
            public const string OnError = "#000000";
            public const string Surface = "#0E1116";
            public const string OnSurface = "#E6EAF2";
            public const string Background = "#0A0E13";
            public const string OnBackground = "#E6EAF2";

            // Neutral scale for dark
            public const string Neutral50 = "#1F2937";
            public const string Neutral100 = "#374151";
            public const string Neutral200 = "#4B5563";
            public const string Neutral300 = "#6B7280";
            public const string Neutral500 = "#9CA3AF";
            public const string Neutral700 = "#D1D5DB";
            public const string Neutral900 = "#F3F4F6";
        }
    }

    public static class Spacing
    {
        public const double XS = 4;    // 4px
        public const double SM = 8;    // 8px
        public const double MD = 12;   // 12px
        public const double LG = 16;   // 16px
        public const double XL = 20;   // 20px
        public const double XXL = 24;  // 24px
        public const double XXXL = 32; // 32px
        public const double XXXXL = 40; // 40px
    }

    public static class BorderRadius
    {
        public const int SM = 4;   // 4px
        public const int MD = 8;   // 8px
        public const int LG = 12;  // 12px
        public const int XL = 16;  // 16px
        public const int XXL = 20; // 20px
        public const int Round = 9999; // Full round
    }

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

    public static class Typography
    {
        public static class FontFamily
        {
            public const string Primary = "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
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
        }

        public static class FontWeight
        {
            public const int Normal = 400;
            public const int Medium = 500;
            public const int Semibold = 600;
            public const int Bold = 700;
        }

        public static class LineHeight
        {
            public const double Tight = 1.25;
            public const double Normal = 1.5;
            public const double Relaxed = 1.625;
        }

        // Semantic typography tokens
        public static class Headings
        {
            public static class H1
            {
                public const string Size = FontSize.XXXXXL;  // 40px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.2; // 48px
            }

            public static class H2
            {
                public const string Size = FontSize.XXXXL;   // 32px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.25; // 40px
            }

            public static class H3
            {
                public const string Size = FontSize.XXXL;    // 28px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.29; // 36px
            }

            public static class H4
            {
                public const string Size = FontSize.XXL;     // 24px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.33; // 32px
            }

            public static class H5
            {
                public const string Size = FontSize.XL;      // 20px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.4; // 28px
            }

            public static class H6
            {
                public const string Size = FontSize.LG;      // 18px
                public const int Weight = FontWeight.Semibold;
                public const double LineHeight = 1.44; // 26px
            }
        }

        public static class Body
        {
            public static class Large
            {
                public const string Size = FontSize.LG;      // 18px
                public const int Weight = FontWeight.Normal;
                public const double LineHeight = 1.5; // 1.5
            }

            public static class Medium
            {
                public const string Size = FontSize.Base;    // 16px
                public const int Weight = FontWeight.Normal;
                public const double LineHeight = 1.5; // 1.5
            }

            public static class Small
            {
                public const string Size = FontSize.SM;      // 14px
                public const int Weight = FontWeight.Normal;
                public const double LineHeight = 1.5; // 1.5
            }
        }
    }

    public static class Motion
    {
        public static class Duration
        {
            public const int Fast = 120;     // 120ms
            public const int Base = 200;     // 200ms
            public const int Gentle = 280;   // 280ms
        }

        public static class Easing
        {
            public const string Standard = "cubic-bezier(0.4, 0.0, 0.2, 1)";
            public const string Decelerate = "cubic-bezier(0.0, 0.0, 0.2, 1)";
            public const string Accelerate = "cubic-bezier(0.4, 0.0, 1, 1)";
        }
    }

    public static class Breakpoints
    {
        public const int SM = 600;   // Small devices
        public const int MD = 900;   // Medium devices
        public const int LG = 1200;  // Large devices
        public const int XL = 1536;  // Extra large devices
    }

    public static class ZIndex
    {
        public const int Dropdown = 1000;
        public const int Sticky = 1020;
        public const int Fixed = 1030;
        public const int Modal = 1040;
        public const int Popover = 1050;
        public const int Tooltip = 1060;
    }
}