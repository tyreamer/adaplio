using Microsoft.AspNetCore.Components;
using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Components.Base;

public abstract class BaseForm<T> : ComponentBase where T : class, new()
{
    [Inject] protected IErrorHandlingService ErrorHandler { get; set; } = default!;
    [Inject] protected ILogger<BaseForm<T>> Logger { get; set; } = default!;

    protected T FormData { get; set; } = new T();
    protected T? OriginalData { get; set; }

    protected bool HasUnsavedChanges { get; private set; } = false;
    protected bool IsSaving { get; private set; } = false;
    protected bool IsLoading { get; private set; } = false;
    protected DateTime? LastSaved { get; private set; }
    protected string? ErrorMessage { get; private set; }

    // Configuration
    protected virtual bool AutoSave => false;
    protected virtual TimeSpan AutoSaveDelay => TimeSpan.FromSeconds(2);
    protected virtual bool TrackChanges => true;
    protected virtual bool ShowUnsavedWarning => true;

    private Timer? _autoSaveTimer;
    private string? _lastFormDataJson;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();

        if (TrackChanges)
        {
            StartChangeTracking();
        }
    }

    protected virtual async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            var data = await OnLoadDataAsync();
            if (data != null)
            {
                FormData = data;
                OriginalData = CloneData(data);
                UpdateChangeTracking();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to load data", GetType().Name);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected virtual async Task SaveDataAsync()
    {
        if (!ValidateForm())
        {
            return;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            StateHasChanged();

            var success = await OnSaveDataAsync(FormData);
            if (success)
            {
                OriginalData = CloneData(FormData);
                HasUnsavedChanges = false;
                LastSaved = DateTime.Now;
                UpdateChangeTracking();

                await OnSaveSuccessAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to save data", GetType().Name);
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    protected virtual async Task ResetFormAsync()
    {
        if (OriginalData != null)
        {
            FormData = CloneData(OriginalData);
            HasUnsavedChanges = false;
            ErrorMessage = null;
            UpdateChangeTracking();
            StateHasChanged();

            await OnResetAsync();
        }
    }

    protected virtual void MarkAsChanged()
    {
        if (!TrackChanges) return;

        HasUnsavedChanges = true;
        StateHasChanged();

        if (AutoSave)
        {
            ScheduleAutoSave();
        }
    }

    protected virtual bool ValidateForm()
    {
        ErrorMessage = null;
        var validationResult = OnValidateForm();

        if (!validationResult.IsValid)
        {
            ErrorMessage = validationResult.ErrorMessage;
            StateHasChanged();
        }

        return validationResult.IsValid;
    }

    // Abstract methods to implement in derived classes
    protected abstract Task<T?> OnLoadDataAsync();
    protected abstract Task<bool> OnSaveDataAsync(T data);

    // Virtual methods that can be overridden
    protected virtual FormValidationResult OnValidateForm()
    {
        return FormValidationResult.Success();
    }

    protected virtual async Task OnSaveSuccessAsync()
    {
        ErrorHandler.ShowSuccess("Data saved successfully!");
    }

    protected virtual async Task OnResetAsync()
    {
        // Override if needed
    }

    protected virtual T CloneData(T data)
    {
        // Simple JSON-based cloning - override for custom cloning logic
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    private void StartChangeTracking()
    {
        _lastFormDataJson = System.Text.Json.JsonSerializer.Serialize(FormData);
    }

    private void UpdateChangeTracking()
    {
        if (!TrackChanges) return;

        _lastFormDataJson = System.Text.Json.JsonSerializer.Serialize(FormData);
    }

    private void CheckForChanges()
    {
        if (!TrackChanges) return;

        var currentJson = System.Text.Json.JsonSerializer.Serialize(FormData);
        var hasChanges = currentJson != _lastFormDataJson;

        if (hasChanges != HasUnsavedChanges)
        {
            HasUnsavedChanges = hasChanges;
            StateHasChanged();
        }
    }

    private void ScheduleAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(async _ => await AutoSaveCallback(), null, AutoSaveDelay, Timeout.InfiniteTimeSpan);
    }

    private async Task AutoSaveCallback()
    {
        if (HasUnsavedChanges && !IsSaving)
        {
            await SaveDataAsync();
        }
    }

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
    }
}

public class FormValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static FormValidationResult Success() => new() { IsValid = true };
    public static FormValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
}