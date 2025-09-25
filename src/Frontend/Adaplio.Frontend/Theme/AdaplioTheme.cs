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

            Secondary = DesignTokens.Colors.Light.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Light.OnSecondary,

            Tertiary = DesignTokens.Colors.Light.Tertiary,
            TertiaryContrastText = DesignTokens.Colors.Light.OnTertiary,

            // State colors
            Success = DesignTokens.Colors.Light.Success,
            SuccessContrastText = DesignTokens.Colors.Light.OnSuccess,

            Warning = DesignTokens.Colors.Light.Warning,
            WarningContrastText = DesignTokens.Colors.Light.OnWarning,

            Error = DesignTokens.Colors.Light.Error,
            ErrorContrastText = DesignTokens.Colors.Light.OnError,

            Info = DesignTokens.Colors.Light.Info,
            InfoContrastText = DesignTokens.Colors.Light.OnInfo,

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

            Secondary = DesignTokens.Colors.Dark.Secondary,
            SecondaryContrastText = DesignTokens.Colors.Dark.OnSecondary,

            Tertiary = DesignTokens.Colors.Dark.Tertiary,
            TertiaryContrastText = DesignTokens.Colors.Dark.OnTertiary,

            // State colors
            Success = DesignTokens.Colors.Dark.Success,
            SuccessContrastText = DesignTokens.Colors.Dark.OnSuccess,

            Warning = DesignTokens.Colors.Dark.Warning,
            WarningContrastText = DesignTokens.Colors.Dark.OnWarning,

            Error = DesignTokens.Colors.Dark.Error,
            ErrorContrastText = DesignTokens.Colors.Dark.OnError,

            Info = DesignTokens.Colors.Dark.Info,
            InfoContrastText = DesignTokens.Colors.Dark.OnInfo,

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
        }
    };

    public static MudTheme DarkTheme => new()
    {
        PaletteLight = LightTheme.PaletteLight,
        PaletteDark = LightTheme.PaletteDark,
        LayoutProperties = LightTheme.LayoutProperties
    };
}