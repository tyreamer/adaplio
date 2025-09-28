using Microsoft.AspNetCore.Components;
using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Components.Base;

public abstract class DataManager<T> : ComponentBase, IDisposable where T : class
{
    [Inject] protected IErrorHandlingService ErrorHandler { get; set; } = default!;
    [Inject] protected ILogger<DataManager<T>> Logger { get; set; } = default!;

    protected List<T> Items { get; set; } = new();
    protected T? SelectedItem { get; set; }
    protected bool IsLoading { get; set; } = false;
    protected bool IsInitialized { get; set; } = false;
    protected string? ErrorMessage { get; set; }

    // Filtering and sorting
    protected string SearchQuery { get; set; } = "";
    protected string SortField { get; set; } = "";
    protected bool SortDescending { get; set; } = false;

    // Pagination
    protected int CurrentPage { get; set; } = 1;
    protected int PageSize { get; set; } = 10;
    protected int TotalItems { get; set; } = 0;
    protected int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // Cache management
    protected DateTime? LastRefresh { get; set; }
    protected TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
    protected bool IsCacheValid => LastRefresh.HasValue && DateTime.UtcNow - LastRefresh.Value < CacheExpiry;

    // Configuration
    protected virtual bool EnableCaching => true;
    protected virtual bool EnableAutoRefresh => false;
    protected virtual TimeSpan AutoRefreshInterval => TimeSpan.FromMinutes(1);
    protected virtual bool LoadOnInitialize => true;

    private Timer? _autoRefreshTimer;

    protected override async Task OnInitializedAsync()
    {
        if (LoadOnInitialize)
        {
            await LoadDataAsync();
        }

        if (EnableAutoRefresh)
        {
            StartAutoRefresh();
        }
    }

    protected virtual async Task LoadDataAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && EnableCaching && IsCacheValid)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StateHasChanged();

            var result = await OnLoadDataAsync();

            Items = result.Items;
            TotalItems = result.TotalItems;
            LastRefresh = DateTime.UtcNow;
            IsInitialized = true;

            await OnDataLoadedAsync(result);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to load data", GetType().Name);
            ErrorMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected virtual async Task RefreshDataAsync()
    {
        await LoadDataAsync(forceRefresh: true);
    }

    protected virtual async Task SearchAsync(string query)
    {
        SearchQuery = query;
        CurrentPage = 1;
        await LoadDataAsync(forceRefresh: true);
    }

    protected virtual async Task SortAsync(string field, bool descending = false)
    {
        SortField = field;
        SortDescending = descending;
        await LoadDataAsync(forceRefresh: true);
    }

    protected virtual async Task GoToPageAsync(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadDataAsync(forceRefresh: true);
        }
    }

    protected virtual async Task SelectItemAsync(T item)
    {
        SelectedItem = item;
        await OnItemSelectedAsync(item);
        StateHasChanged();
    }

    protected virtual async Task AddItemAsync(T item)
    {
        try
        {
            var success = await OnAddItemAsync(item);
            if (success)
            {
                Items.Add(item);
                TotalItems++;
                await OnItemAddedAsync(item);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to add item", GetType().Name);
        }
    }

    protected virtual async Task UpdateItemAsync(T item)
    {
        try
        {
            var success = await OnUpdateItemAsync(item);
            if (success)
            {
                var index = Items.FindIndex(i => GetItemId(i) == GetItemId(item));
                if (index >= 0)
                {
                    Items[index] = item;
                    await OnItemUpdatedAsync(item);
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to update item", GetType().Name);
        }
    }

    protected virtual async Task DeleteItemAsync(T item)
    {
        try
        {
            var success = await OnDeleteItemAsync(item);
            if (success)
            {
                Items.Remove(item);
                TotalItems--;
                if (SelectedItem != null && GetItemId(SelectedItem) == GetItemId(item))
                {
                    SelectedItem = null;
                }
                await OnItemDeletedAsync(item);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to delete item", GetType().Name);
        }
    }

    protected virtual void InvalidateCache()
    {
        LastRefresh = null;
    }

    private void StartAutoRefresh()
    {
        _autoRefreshTimer = new Timer(async _ =>
        {
            if (!IsLoading)
            {
                await LoadDataAsync();
            }
        }, null, AutoRefreshInterval, AutoRefreshInterval);
    }

    // Abstract methods to implement
    protected abstract Task<DataResult<T>> OnLoadDataAsync();
    protected abstract string GetItemId(T item);

    // Virtual methods that can be overridden
    protected virtual async Task<bool> OnAddItemAsync(T item) => true;
    protected virtual async Task<bool> OnUpdateItemAsync(T item) => true;
    protected virtual async Task<bool> OnDeleteItemAsync(T item) => true;

    protected virtual async Task OnDataLoadedAsync(DataResult<T> result) { }
    protected virtual async Task OnItemSelectedAsync(T item) { }
    protected virtual async Task OnItemAddedAsync(T item) { }
    protected virtual async Task OnItemUpdatedAsync(T item) { }
    protected virtual async Task OnItemDeletedAsync(T item) { }

    public virtual void Dispose()
    {
        _autoRefreshTimer?.Dispose();
    }
}

public class DataResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public bool HasMore { get; set; }
    public string? NextPageToken { get; set; }
}