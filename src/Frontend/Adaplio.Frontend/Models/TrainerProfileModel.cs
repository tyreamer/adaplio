using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adaplio.Frontend.Models;

public class TrainerProfileModel : INotifyPropertyChanged, IEquatable<TrainerProfileModel>
{
    private string? _displayName;
    private string? _credentials;
    private string? _licenseNumber;
    private string? _clinicName;
    private string? _location;
    private string? _phone;
    private string? _website;
    private string? _bio;
    private TimeSpan? _defaultReminderTime;
    private string? _logoUrl;
    private List<string> _specialties = new();

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
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

    [StringLength(200, ErrorMessage = "Credentials cannot exceed 200 characters")]
    public string? Credentials
    {
        get => _credentials;
        set
        {
            if (_credentials != value)
            {
                _credentials = value;
                OnPropertyChanged(nameof(Credentials));
            }
        }
    }

    [StringLength(100, ErrorMessage = "License number cannot exceed 100 characters")]
    public string? LicenseNumber
    {
        get => _licenseNumber;
        set
        {
            if (_licenseNumber != value)
            {
                _licenseNumber = value;
                OnPropertyChanged(nameof(LicenseNumber));
            }
        }
    }

    [StringLength(200, ErrorMessage = "Clinic name cannot exceed 200 characters")]
    public string? ClinicName
    {
        get => _clinicName;
        set
        {
            if (_clinicName != value)
            {
                _clinicName = value;
                OnPropertyChanged(nameof(ClinicName));
            }
        }
    }

    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string? Location
    {
        get => _location;
        set
        {
            if (_location != value)
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }
    }

    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890)")]
    public string? Phone
    {
        get => _phone;
        set
        {
            if (_phone != value)
            {
                _phone = value;
                OnPropertyChanged(nameof(Phone));
            }
        }
    }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? Website
    {
        get => _website;
        set
        {
            if (_website != value)
            {
                _website = value;
                OnPropertyChanged(nameof(Website));
            }
        }
    }

    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
    public string? Bio
    {
        get => _bio;
        set
        {
            if (_bio != value)
            {
                _bio = value;
                OnPropertyChanged(nameof(Bio));
            }
        }
    }

    public TimeSpan? DefaultReminderTime
    {
        get => _defaultReminderTime;
        set
        {
            if (_defaultReminderTime != value)
            {
                _defaultReminderTime = value;
                OnPropertyChanged(nameof(DefaultReminderTime));
            }
        }
    }

    public string? LogoUrl
    {
        get => _logoUrl;
        set
        {
            if (_logoUrl != value)
            {
                _logoUrl = value;
                OnPropertyChanged(nameof(LogoUrl));
            }
        }
    }

    public List<string> Specialties
    {
        get => _specialties;
        set
        {
            if (_specialties != value)
            {
                _specialties = value ?? new List<string>();
                OnPropertyChanged(nameof(Specialties));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public TrainerProfileModel Clone()
    {
        return new TrainerProfileModel
        {
            DisplayName = DisplayName,
            Credentials = Credentials,
            LicenseNumber = LicenseNumber,
            ClinicName = ClinicName,
            Location = Location,
            Phone = Phone,
            Website = Website,
            Bio = Bio,
            DefaultReminderTime = DefaultReminderTime,
            LogoUrl = LogoUrl,
            Specialties = new List<string>(Specialties)
        };
    }

    public bool Equals(TrainerProfileModel? other)
    {
        if (other == null) return false;

        return DisplayName == other.DisplayName &&
               Credentials == other.Credentials &&
               LicenseNumber == other.LicenseNumber &&
               ClinicName == other.ClinicName &&
               Location == other.Location &&
               Phone == other.Phone &&
               Website == other.Website &&
               Bio == other.Bio &&
               DefaultReminderTime == other.DefaultReminderTime &&
               LogoUrl == other.LogoUrl &&
               Specialties.SequenceEqual(other.Specialties);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TrainerProfileModel);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DisplayName);
        hash.Add(Credentials);
        hash.Add(LicenseNumber);
        hash.Add(ClinicName);
        hash.Add(Location);
        hash.Add(Phone);
        hash.Add(Website);
        hash.Add(Bio);
        hash.Add(DefaultReminderTime);
        hash.Add(LogoUrl);
        foreach (var specialty in Specialties)
        {
            hash.Add(specialty);
        }
        return hash.ToHashCode();
    }
}