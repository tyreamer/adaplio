using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adaplio.Frontend.Models;

public class ClientProfileModel : INotifyPropertyChanged, IEquatable<ClientProfileModel>
{
    private string? _displayName;
    private string? _timezone;
    private string? _injury;
    private string? _affectedSide;
    private string? _emergencyContactName;
    private string? _emergencyContactPhone;
    private bool _largeTextMode;
    private bool _highContrastMode;
    private bool _reducedMotionMode;
    private string? _avatarUrl;

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 60 characters")]
    public string? DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [Required(ErrorMessage = "Timezone is required")]
    public string? Timezone
    {
        get => _timezone;
        set
        {
            if (_timezone != value)
            {
                _timezone = value;
                OnPropertyChanged(nameof(Timezone));
            }
        }
    }

    [StringLength(200, ErrorMessage = "Injury description cannot exceed 200 characters")]
    public string? Injury
    {
        get => _injury;
        set
        {
            if (_injury != value)
            {
                _injury = value;
                OnPropertyChanged(nameof(Injury));
            }
        }
    }

    public string? AffectedSide
    {
        get => _affectedSide;
        set
        {
            if (_affectedSide != value)
            {
                _affectedSide = value;
                OnPropertyChanged(nameof(AffectedSide));
            }
        }
    }

    [StringLength(100, ErrorMessage = "Emergency contact name cannot exceed 100 characters")]
    public string? EmergencyContactName
    {
        get => _emergencyContactName;
        set
        {
            if (_emergencyContactName != value)
            {
                _emergencyContactName = value;
                OnPropertyChanged(nameof(EmergencyContactName));
            }
        }
    }

    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890)")]
    public string? EmergencyContactPhone
    {
        get => _emergencyContactPhone;
        set
        {
            if (_emergencyContactPhone != value)
            {
                _emergencyContactPhone = value;
                OnPropertyChanged(nameof(EmergencyContactPhone));
            }
        }
    }

    public bool LargeTextMode
    {
        get => _largeTextMode;
        set
        {
            if (_largeTextMode != value)
            {
                _largeTextMode = value;
                OnPropertyChanged(nameof(LargeTextMode));
            }
        }
    }

    public bool HighContrastMode
    {
        get => _highContrastMode;
        set
        {
            if (_highContrastMode != value)
            {
                _highContrastMode = value;
                OnPropertyChanged(nameof(HighContrastMode));
            }
        }
    }

    public bool ReducedMotionMode
    {
        get => _reducedMotionMode;
        set
        {
            if (_reducedMotionMode != value)
            {
                _reducedMotionMode = value;
                OnPropertyChanged(nameof(ReducedMotionMode));
            }
        }
    }

    public string? AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            if (_avatarUrl != value)
            {
                _avatarUrl = value;
                OnPropertyChanged(nameof(AvatarUrl));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ClientProfileModel Clone()
    {
        return new ClientProfileModel
        {
            DisplayName = DisplayName,
            Timezone = Timezone,
            Injury = Injury,
            AffectedSide = AffectedSide,
            EmergencyContactName = EmergencyContactName,
            EmergencyContactPhone = EmergencyContactPhone,
            LargeTextMode = LargeTextMode,
            HighContrastMode = HighContrastMode,
            ReducedMotionMode = ReducedMotionMode,
            AvatarUrl = AvatarUrl
        };
    }

    public bool Equals(ClientProfileModel? other)
    {
        if (other == null) return false;

        return DisplayName == other.DisplayName &&
               Timezone == other.Timezone &&
               Injury == other.Injury &&
               AffectedSide == other.AffectedSide &&
               EmergencyContactName == other.EmergencyContactName &&
               EmergencyContactPhone == other.EmergencyContactPhone &&
               LargeTextMode == other.LargeTextMode &&
               HighContrastMode == other.HighContrastMode &&
               ReducedMotionMode == other.ReducedMotionMode &&
               AvatarUrl == other.AvatarUrl;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ClientProfileModel);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DisplayName);
        hash.Add(Timezone);
        hash.Add(Injury);
        hash.Add(AffectedSide);
        hash.Add(EmergencyContactName);
        hash.Add(EmergencyContactPhone);
        hash.Add(LargeTextMode);
        hash.Add(HighContrastMode);
        hash.Add(ReducedMotionMode);
        hash.Add(AvatarUrl);
        return hash.ToHashCode();
    }
}