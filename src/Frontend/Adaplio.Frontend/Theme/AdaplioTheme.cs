using MudBlazor;

namespace Adaplio.Frontend.Theme;

/// <summary>
/// Premium MudBlazor theme implementation using comprehensive design tokens.
/// Delivers a 2025-quality visual language with intentional dark mode.
/// </summary>
public static class AdaplioTheme
{
    public static MudTheme LightTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand colors
            Primary = DesignTokens.Colors.Light.Primary,
            PrimaryContrastText = DesignTokens.Colors.Light.OnPrimary,
            PrimaryDarken = DesignTokens.Colors.Light.PrimaryContainer,
            PrimaryLighten = DesignTokens.Colors.Light.OnPrimaryContainer,

            Secondary = DesignTokens.Colors.Light.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Light.OnSecondary,
            SecondaryDarken = DesignTokens.Colors.Light.SecondaryContainer,
            SecondaryLighten = DesignTokens.Colors.Light.OnSecondaryContainer,

            Tertiary = DesignTokens.Colors.Light.Tertiary,
            TertiaryContrastText = DesignTokens.Colors.Light.OnTertiary,
            TertiaryDarken = DesignTokens.Colors.Light.TertiaryContainer,
            TertiaryLighten = DesignTokens.Colors.Light.OnTertiaryContainer,

            // State colors
            Success = DesignTokens.Colors.Light.Success,
            SuccessContrastText = DesignTokens.Colors.Light.OnSuccess,
            SuccessDarken = DesignTokens.Colors.Light.SuccessContainer,
            SuccessLighten = DesignTokens.Colors.Light.OnSuccessContainer,

            Warning = DesignTokens.Colors.Light.Warning,
            WarningContrastText = DesignTokens.Colors.Light.OnWarning,
            WarningDarken = DesignTokens.Colors.Light.WarningContainer,
            WarningLighten = DesignTokens.Colors.Light.OnWarningContainer,

            Error = DesignTokens.Colors.Light.Error,
            ErrorContrastText = DesignTokens.Colors.Light.OnError,
            ErrorDarken = DesignTokens.Colors.Light.ErrorContainer,
            ErrorLighten = DesignTokens.Colors.Light.OnErrorContainer,

            Info = DesignTokens.Colors.Light.Info,
            InfoContrastText = DesignTokens.Colors.Light.OnInfo,
            InfoDarken = DesignTokens.Colors.Light.InfoContainer,
            InfoLighten = DesignTokens.Colors.Light.OnInfoContainer,

            // Surface colors
            Surface = DesignTokens.Colors.Light.Surface,
            Background = DesignTokens.Colors.Light.Background,
            BackgroundGray = DesignTokens.Colors.Light.SurfaceVariant,

            // Text colors
            TextPrimary = DesignTokens.Colors.Light.OnSurface,
            TextSecondary = DesignTokens.Colors.Light.OnSurfaceVariant,
            TextDisabled = DesignTokens.Colors.Light.Neutral400,

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

            // Lines and dividers
            Divider = DesignTokens.Colors.Light.Outline,
            DividerLight = DesignTokens.Colors.Light.OutlineVariant,

            // Tables
            TableLines = DesignTokens.Colors.Light.Outline,
            TableStriped = DesignTokens.Colors.Light.SurfaceVariant,
            TableHover = DesignTokens.Colors.Light.Neutral50,

            // Overlays
            OverlayDark = DesignTokens.Colors.Light.Scrim,
            OverlayLight = "rgba(255, 255, 255, 0.8)"
        },

        PaletteDark = new PaletteDark
        {
            // Brand colors
            Primary = DesignTokens.Colors.Dark.Primary,
            PrimaryContrastText = DesignTokens.Colors.Dark.OnPrimary,
            PrimaryDarken = DesignTokens.Colors.Dark.PrimaryContainer,
            PrimaryLighten = DesignTokens.Colors.Dark.OnPrimaryContainer,

            Secondary = DesignTokens.Colors.Dark.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Dark.OnSecondary,
            SecondaryDarken = DesignTokens.Colors.Dark.SecondaryContainer,
            SecondaryLighten = DesignTokens.Colors.Dark.OnSecondaryContainer,

            Tertiary = DesignTokens.Colors.Dark.Tertiary,
            TertiaryContrastText = DesignTokens.Colors.Dark.OnTertiary,
            TertiaryDarken = DesignTokens.Colors.Dark.TertiaryContainer,
            TertiaryLighten = DesignTokens.Colors.Dark.OnTertiaryContainer,

            // State colors
            Success = DesignTokens.Colors.Dark.Success,
            SuccessContrastText = DesignTokens.Colors.Dark.OnSuccess,
            SuccessDarken = DesignTokens.Colors.Dark.SuccessContainer,
            SuccessLighten = DesignTokens.Colors.Dark.OnSuccessContainer,

            Warning = DesignTokens.Colors.Dark.Warning,
            WarningContrastText = DesignTokens.Colors.Dark.OnWarning,
            WarningDarken = DesignTokens.Colors.Dark.WarningContainer,
            WarningLighten = DesignTokens.Colors.Dark.OnWarningContainer,

            Error = DesignTokens.Colors.Dark.Error,
            ErrorContrastText = DesignTokens.Colors.Dark.OnError,
            ErrorDarken = DesignTokens.Colors.Dark.ErrorContainer,
            ErrorLighten = DesignTokens.Colors.Dark.OnErrorContainer,

            Info = DesignTokens.Colors.Dark.Info,
            InfoContrastText = DesignTokens.Colors.Dark.OnInfo,
            InfoDarken = DesignTokens.Colors.Dark.InfoContainer,
            InfoLighten = DesignTokens.Colors.Dark.OnInfoContainer,

            // Surface colors
            Surface = DesignTokens.Colors.Dark.Surface,
            Background = DesignTokens.Colors.Dark.Background,
            BackgroundGray = DesignTokens.Colors.Dark.SurfaceVariant,

            // Text colors
            TextPrimary = DesignTokens.Colors.Dark.OnSurface,
            TextSecondary = DesignTokens.Colors.Dark.OnSurfaceVariant,
            TextDisabled = DesignTokens.Colors.Dark.Neutral600,

            // Action colors
            ActionDefault = DesignTokens.Colors.Dark.Neutral700,
            ActionDisabled = DesignTokens.Colors.Dark.Neutral500,
            ActionDisabledBackground = DesignTokens.Colors.Dark.Neutral300,

            // AppBar colors
            AppbarBackground = DesignTokens.Colors.Dark.Surface,
            AppbarText = DesignTokens.Colors.Dark.OnSurface,

            // Drawer colors
            DrawerBackground = DesignTokens.Colors.Dark.Surface,
            DrawerText = DesignTokens.Colors.Dark.OnSurface,
            DrawerIcon = DesignTokens.Colors.Dark.Neutral700,

            // Lines and dividers
            Divider = DesignTokens.Colors.Dark.Outline,
            DividerLight = DesignTokens.Colors.Dark.OutlineVariant,

            // Tables
            TableLines = DesignTokens.Colors.Dark.Outline,
            TableStriped = DesignTokens.Colors.Dark.SurfaceVariant,
            TableHover = DesignTokens.Colors.Dark.Neutral200,

            // Overlays
            OverlayDark = DesignTokens.Colors.Dark.Scrim,
            OverlayLight = "rgba(14, 17, 22, 0.8)"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = $"{DesignTokens.BorderRadius.LG}px",
            DrawerWidthLeft = "280px",
            DrawerWidthRight = "280px",
            AppbarHeight = "64px"
        },

        Shadows = new Shadow
        {
            Elevation = CreateElevationArray()
        }
    };

    public static MudTheme DarkTheme => new()
    {
        PaletteLight = LightTheme.PaletteLight,
        PaletteDark = LightTheme.PaletteDark,
        LayoutProperties = LightTheme.LayoutProperties,

        Shadows = new Shadow
        {
            Elevation = CreateDarkElevationArray()
        }
    };

    /// <summary>
    /// Creates a comprehensive elevation array for light mode (25 levels as required by MudBlazor)
    /// </summary>
    private static string[] CreateElevationArray()
    {
        var elevations = new string[25];

        // Safely populate elevation levels
        elevations[0] = DesignTokens.Elevation.Light.Level0;
        elevations[1] = DesignTokens.Elevation.Light.Level1;
        elevations[2] = DesignTokens.Elevation.Light.Level2;
        elevations[3] = DesignTokens.Elevation.Light.Level3;
        elevations[4] = DesignTokens.Elevation.Light.Level4;

        // Fill remaining slots with highest level
        for (int i = 5; i < 25; i++)
        {
            elevations[i] = DesignTokens.Elevation.Light.Level5;
        }

        return elevations;
    }

    /// <summary>
    /// Creates a comprehensive elevation array for dark mode (25 levels as required by MudBlazor)
    /// </summary>
    private static string[] CreateDarkElevationArray()
    {
        var elevations = new string[25];

        // Safely populate elevation levels
        elevations[0] = DesignTokens.Elevation.Dark.Level0;
        elevations[1] = DesignTokens.Elevation.Dark.Level1;
        elevations[2] = DesignTokens.Elevation.Dark.Level2;
        elevations[3] = DesignTokens.Elevation.Dark.Level3;
        elevations[4] = DesignTokens.Elevation.Dark.Level4;

        // Fill remaining slots with highest level
        for (int i = 5; i < 25; i++)
        {
            elevations[i] = DesignTokens.Elevation.Dark.Level5;
        }

        return elevations;
    }
}