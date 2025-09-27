namespace Adaplio.Frontend.Services;

public class FormStateService
{
    private readonly Dictionary<string, FormState> _formStates = new();
    private readonly ILogger<FormStateService> _logger;

    public event EventHandler<FormStateChangedEventArgs>? FormStateChanged;

    public FormStateService(ILogger<FormStateService> logger)
    {
        _logger = logger;
    }

    public void RegisterForm(string formId, object originalModel)
    {
        var state = new FormState
        {
            FormId = formId,
            OriginalModel = originalModel,
            HasUnsavedChanges = false,
            LastSaved = null,
            ValidationErrors = new List<string>()
        };

        _formStates[formId] = state;
        _logger.LogDebug("Registered form: {FormId}", formId);
    }

    public void UpdateFormState(string formId, object currentModel, bool hasUnsavedChanges, List<string>? validationErrors = null)
    {
        if (!_formStates.TryGetValue(formId, out var state))
        {
            _logger.LogWarning("Attempted to update unknown form: {FormId}", formId);
            return;
        }

        var previousHasChanges = state.HasUnsavedChanges;

        state.CurrentModel = currentModel;
        state.HasUnsavedChanges = hasUnsavedChanges;

        if (validationErrors != null)
        {
            state.ValidationErrors = validationErrors;
        }

        // Notify if unsaved changes state has changed
        if (previousHasChanges != hasUnsavedChanges)
        {
            FormStateChanged?.Invoke(this, new FormStateChangedEventArgs(formId, hasUnsavedChanges));
        }

        _logger.LogDebug("Updated form state: {FormId}, HasChanges: {HasChanges}, Errors: {ErrorCount}",
            formId, hasUnsavedChanges, state.ValidationErrors.Count);
    }

    public void MarkFormSaved(string formId, object savedModel)
    {
        if (!_formStates.TryGetValue(formId, out var state))
        {
            _logger.LogWarning("Attempted to mark unknown form as saved: {FormId}", formId);
            return;
        }

        state.OriginalModel = savedModel;
        state.CurrentModel = savedModel;
        state.HasUnsavedChanges = false;
        state.LastSaved = DateTime.Now;
        state.ValidationErrors.Clear();

        FormStateChanged?.Invoke(this, new FormStateChangedEventArgs(formId, false));

        _logger.LogDebug("Marked form as saved: {FormId}", formId);
    }

    public void UnregisterForm(string formId)
    {
        if (_formStates.Remove(formId))
        {
            _logger.LogDebug("Unregistered form: {FormId}", formId);
        }
    }

    public FormState? GetFormState(string formId)
    {
        return _formStates.TryGetValue(formId, out var state) ? state : null;
    }

    public bool HasUnsavedChanges(string formId)
    {
        return _formStates.TryGetValue(formId, out var state) && state.HasUnsavedChanges;
    }

    public bool HasAnyUnsavedChanges()
    {
        return _formStates.Values.Any(s => s.HasUnsavedChanges);
    }

    public List<string> GetFormsWithUnsavedChanges()
    {
        return _formStates.Where(kvp => kvp.Value.HasUnsavedChanges)
                         .Select(kvp => kvp.Key)
                         .ToList();
    }

    public async Task<bool> ConfirmNavigationAsync()
    {
        if (!HasAnyUnsavedChanges())
            return true;

        // In a real app, you'd show a confirmation dialog
        // For now, we'll just log the attempt
        var formsWithChanges = GetFormsWithUnsavedChanges();
        _logger.LogInformation("Navigation attempted with unsaved changes in forms: {Forms}",
            string.Join(", ", formsWithChanges));

        return false; // Block navigation - this would be handled by a dialog in production
    }

    public void ResetForm(string formId)
    {
        if (!_formStates.TryGetValue(formId, out var state))
        {
            _logger.LogWarning("Attempted to reset unknown form: {FormId}", formId);
            return;
        }

        state.CurrentModel = state.OriginalModel;
        state.HasUnsavedChanges = false;
        state.ValidationErrors.Clear();

        FormStateChanged?.Invoke(this, new FormStateChangedEventArgs(formId, false));

        _logger.LogDebug("Reset form: {FormId}", formId);
    }
}

public class FormState
{
    public string FormId { get; set; } = "";
    public object? OriginalModel { get; set; }
    public object? CurrentModel { get; set; }
    public bool HasUnsavedChanges { get; set; }
    public DateTime? LastSaved { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsSubmitting { get; set; }
}

public class FormStateChangedEventArgs : EventArgs
{
    public string FormId { get; }
    public bool HasUnsavedChanges { get; }

    public FormStateChangedEventArgs(string formId, bool hasUnsavedChanges)
    {
        FormId = formId;
        HasUnsavedChanges = hasUnsavedChanges;
    }
}