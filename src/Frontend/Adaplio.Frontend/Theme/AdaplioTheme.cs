using MudBlazor;

namespace Adaplio.Frontend.Theme;

public static class AdaplioTheme
{
    public static MudTheme LightTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand colors
            Primary = DesignTokens.Colors.Light.Primary,
            PrimaryContrastText = DesignTokens.Colors.Light.OnPrimary,
            Secondary = DesignTokens.Colors.Light.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Light.OnSecondary,
            Tertiary = DesignTokens.Colors.Light.Primary,
            TertiaryContrastText = DesignTokens.Colors.Light.OnPrimary,

            // State colors
            Success = DesignTokens.Colors.Light.Success,
            SuccessContrastText = DesignTokens.Colors.Light.OnSuccess,
            Warning = DesignTokens.Colors.Light.Warning,
            WarningContrastText = DesignTokens.Colors.Light.OnWarning,
            Error = DesignTokens.Colors.Light.Error,
            ErrorContrastText = DesignTokens.Colors.Light.OnError,
            Info = DesignTokens.Colors.Light.Primary,
            InfoContrastText = DesignTokens.Colors.Light.OnPrimary,

            // Surface colors
            Surface = DesignTokens.Colors.Light.Surface,
            Background = DesignTokens.Colors.Light.Background,

            // Text colors
            TextPrimary = DesignTokens.Colors.Light.OnSurface,
            TextSecondary = DesignTokens.Colors.Light.Neutral700,
            TextDisabled = DesignTokens.Colors.Light.Neutral500,

            // Action colors
            ActionDefault = DesignTokens.Colors.Light.Neutral700,
            ActionDisabled = DesignTokens.Colors.Light.Neutral300,
            ActionDisabledBackground = DesignTokens.Colors.Light.Neutral100,

            // AppBar colors
            AppbarBackground = DesignTokens.Colors.Light.Surface,
            AppbarText = DesignTokens.Colors.Light.OnSurface,

            // Drawer colors
            DrawerBackground = DesignTokens.Colors.Light.Surface,
            DrawerText = DesignTokens.Colors.Light.OnSurface,
            DrawerIcon = DesignTokens.Colors.Light.Neutral700,

            // Divider
            Divider = DesignTokens.Colors.Light.Neutral200,
            DividerLight = DesignTokens.Colors.Light.Neutral100,

            // Tables
            TableLines = DesignTokens.Colors.Light.Neutral200,
            TableStriped = DesignTokens.Colors.Light.Neutral50,
            TableHover = DesignTokens.Colors.Light.Neutral50,

            // Overlays
            OverlayDark = "rgba(16, 24, 40, 0.6)",
            OverlayLight = "rgba(255, 255, 255, 0.6)"
        },

        PaletteDark = new PaletteDark
        {
            // Brand colors
            Primary = DesignTokens.Colors.Dark.Primary,
            PrimaryContrastText = DesignTokens.Colors.Dark.OnPrimary,
            Secondary = DesignTokens.Colors.Dark.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Dark.OnSecondary,
            Tertiary = DesignTokens.Colors.Dark.Primary,
            TertiaryContrastText = DesignTokens.Colors.Dark.OnPrimary,

            // State colors
            Success = DesignTokens.Colors.Dark.Success,
            SuccessContrastText = DesignTokens.Colors.Dark.OnSuccess,
            Warning = DesignTokens.Colors.Dark.Warning,
            WarningContrastText = DesignTokens.Colors.Dark.OnWarning,
            Error = DesignTokens.Colors.Dark.Error,
            ErrorContrastText = DesignTokens.Colors.Dark.OnError,
            Info = DesignTokens.Colors.Dark.Primary,
            InfoContrastText = DesignTokens.Colors.Dark.OnPrimary,

            // Surface colors
            Surface = DesignTokens.Colors.Dark.Surface,
            Background = DesignTokens.Colors.Dark.Background,

            // Text colors
            TextPrimary = DesignTokens.Colors.Dark.OnSurface,
            TextSecondary = DesignTokens.Colors.Dark.Neutral700,
            TextDisabled = DesignTokens.Colors.Dark.Neutral500,

            // Action colors
            ActionDefault = DesignTokens.Colors.Dark.Neutral700,
            ActionDisabled = DesignTokens.Colors.Dark.Neutral300,
            ActionDisabledBackground = DesignTokens.Colors.Dark.Neutral100,

            // AppBar colors
            AppbarBackground = DesignTokens.Colors.Dark.Surface,
            AppbarText = DesignTokens.Colors.Dark.OnSurface,

            // Drawer colors
            DrawerBackground = DesignTokens.Colors.Dark.Surface,
            DrawerText = DesignTokens.Colors.Dark.OnSurface,
            DrawerIcon = DesignTokens.Colors.Dark.Neutral700,

            // Divider
            Divider = DesignTokens.Colors.Dark.Neutral200,
            DividerLight = DesignTokens.Colors.Dark.Neutral100,

            // Tables
            TableLines = DesignTokens.Colors.Dark.Neutral200,
            TableStriped = DesignTokens.Colors.Dark.Neutral50,
            TableHover = DesignTokens.Colors.Dark.Neutral50,

            // Overlays
            OverlayDark = "rgba(20, 24, 30, 0.8)",
            OverlayLight = "rgba(14, 17, 22, 0.6)"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = $"{DesignTokens.BorderRadius.LG}px",
            AppbarHeight = "64px"
        },

        Shadows = new Shadow
        {
            Elevation = new string[]
            {
                DesignTokens.Elevation.Light.Level0,  // 0
                DesignTokens.Elevation.Light.Level1,  // 1
                DesignTokens.Elevation.Light.Level2,  // 2
                DesignTokens.Elevation.Light.Level3,  // 3
                DesignTokens.Elevation.Light.Level4,  // 4
                DesignTokens.Elevation.Light.Level5,  // 5
                DesignTokens.Elevation.Light.Level5,  // 6
                DesignTokens.Elevation.Light.Level5,  // 7
                DesignTokens.Elevation.Light.Level5,  // 8
                DesignTokens.Elevation.Light.Level5,  // 9
                DesignTokens.Elevation.Light.Level5,  // 10
                DesignTokens.Elevation.Light.Level5,  // 11
                DesignTokens.Elevation.Light.Level5,  // 12
                DesignTokens.Elevation.Light.Level5,  // 13
                DesignTokens.Elevation.Light.Level5,  // 14
                DesignTokens.Elevation.Light.Level5,  // 15
                DesignTokens.Elevation.Light.Level5,  // 16
                DesignTokens.Elevation.Light.Level5,  // 17
                DesignTokens.Elevation.Light.Level5,  // 18
                DesignTokens.Elevation.Light.Level5,  // 19
                DesignTokens.Elevation.Light.Level5,  // 20
                DesignTokens.Elevation.Light.Level5,  // 21
                DesignTokens.Elevation.Light.Level5,  // 22
                DesignTokens.Elevation.Light.Level5,  // 23
                DesignTokens.Elevation.Light.Level5   // 24
            }
        }
    };

    public static MudTheme DarkTheme => new()
    {
        PaletteLight = LightTheme.PaletteLight,
        PaletteDark = LightTheme.PaletteDark,
        LayoutProperties = LightTheme.LayoutProperties,

        Shadows = new Shadow
        {
            Elevation = new string[]
            {
                DesignTokens.Elevation.Dark.Level0,  // 0
                DesignTokens.Elevation.Dark.Level1,  // 1
                DesignTokens.Elevation.Dark.Level2,  // 2
                DesignTokens.Elevation.Dark.Level3,  // 3
                DesignTokens.Elevation.Dark.Level4,  // 4
                DesignTokens.Elevation.Dark.Level5,  // 5
                DesignTokens.Elevation.Dark.Level5,  // 6
                DesignTokens.Elevation.Dark.Level5,  // 7
                DesignTokens.Elevation.Dark.Level5,  // 8
                DesignTokens.Elevation.Dark.Level5,  // 9
                DesignTokens.Elevation.Dark.Level5,  // 10
                DesignTokens.Elevation.Dark.Level5,  // 11
                DesignTokens.Elevation.Dark.Level5,  // 12
                DesignTokens.Elevation.Dark.Level5,  // 13
                DesignTokens.Elevation.Dark.Level5,  // 14
                DesignTokens.Elevation.Dark.Level5,  // 15
                DesignTokens.Elevation.Dark.Level5,  // 16
                DesignTokens.Elevation.Dark.Level5,  // 17
                DesignTokens.Elevation.Dark.Level5,  // 18
                DesignTokens.Elevation.Dark.Level5,  // 19
                DesignTokens.Elevation.Dark.Level5,  // 20
                DesignTokens.Elevation.Dark.Level5,  // 21
                DesignTokens.Elevation.Dark.Level5,  // 22
                DesignTokens.Elevation.Dark.Level5,  // 23
                DesignTokens.Elevation.Dark.Level5   // 24
            }
        }
    };
}