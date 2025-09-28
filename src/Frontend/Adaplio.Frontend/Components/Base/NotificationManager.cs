using Microsoft.AspNetCore.Components;
using MudBlazor;
using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Components.Base;

public abstract class NotificationManager : ComponentBase, IDisposable
{
    [Inject] protected IErrorHandlingService ErrorHandler { get; set; } = default!;
    [Inject] protected ILogger<NotificationManager> Logger { get; set; } = default!;

    protected List<NotificationItem> Notifications { get; set; } = new();
    protected bool HasUnreadNotifications => Notifications.Any(n => !n.IsRead);
    protected int UnreadCount => Notifications.Count(n => !n.IsRead);

    // Configuration
    protected virtual int MaxNotifications => 50;
    protected virtual TimeSpan NotificationExpiry => TimeSpan.FromDays(7);
    protected virtual bool AutoMarkAsRead => true;
    protected virtual bool EnableToasts => true;

    private Timer? _cleanupTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadNotificationsAsync();
        StartCleanupTimer();
    }

    protected virtual async Task LoadNotificationsAsync()
    {
        try
        {
            var notifications = await OnLoadNotificationsAsync();
            Notifications = notifications.OrderByDescending(n => n.CreatedAt).ToList();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to load notifications", GetType().Name);
        }
    }

    protected virtual async Task AddNotificationAsync(string message, NotificationType type = NotificationType.Info, string? title = null, object? data = null)
    {
        var notification = new NotificationItem
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            Data = data
        };

        await AddNotificationAsync(notification);
    }

    protected virtual async Task AddNotificationAsync(NotificationItem notification)
    {
        try
        {
            // Add to local collection
            Notifications.Insert(0, notification);

            // Trim if exceeding max count
            if (Notifications.Count > MaxNotifications)
            {
                Notifications = Notifications.Take(MaxNotifications).ToList();
            }

            // Show toast if enabled
            if (EnableToasts)
            {
                ShowToast(notification);
            }

            // Persist notification
            await OnNotificationAddedAsync(notification);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to add notification", GetType().Name);
        }
    }

    protected virtual async Task MarkAsReadAsync(string notificationId)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await OnNotificationReadAsync(notification);
            StateHasChanged();
        }
    }

    protected virtual async Task MarkAllAsReadAsync()
    {
        var unreadNotifications = Notifications.Where(n => !n.IsRead).ToList();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (unreadNotifications.Any())
        {
            await OnNotificationsReadAsync(unreadNotifications);
            StateHasChanged();
        }
    }

    protected virtual async Task DeleteNotificationAsync(string notificationId)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            Notifications.Remove(notification);
            await OnNotificationDeletedAsync(notification);
            StateHasChanged();
        }
    }

    protected virtual async Task ClearAllNotificationsAsync()
    {
        var allNotifications = Notifications.ToList();
        Notifications.Clear();

        await OnNotificationsClearedAsync(allNotifications);
        StateHasChanged();
    }

    protected virtual void ShowToast(NotificationItem notification)
    {
        var severity = notification.Type switch
        {
            NotificationType.Success => Severity.Success,
            NotificationType.Warning => Severity.Warning,
            NotificationType.Error => Severity.Error,
            _ => Severity.Info
        };

        var message = string.IsNullOrEmpty(notification.Title)
            ? notification.Message
            : $"{notification.Title}: {notification.Message}";

        ErrorHandler.ShowInfo(message); // This will use the appropriate severity based on the type
    }

    private void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(async _ =>
        {
            await CleanupExpiredNotificationsAsync();
        }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    private async Task CleanupExpiredNotificationsAsync()
    {
        var cutoffDate = DateTime.UtcNow - NotificationExpiry;
        var expiredNotifications = Notifications.Where(n => n.CreatedAt < cutoffDate).ToList();

        if (expiredNotifications.Any())
        {
            foreach (var notification in expiredNotifications)
            {
                Notifications.Remove(notification);
            }

            await OnNotificationsExpiredAsync(expiredNotifications);
            StateHasChanged();
        }
    }

    // Abstract methods to implement
    protected abstract Task<List<NotificationItem>> OnLoadNotificationsAsync();

    // Virtual methods that can be overridden
    protected virtual async Task OnNotificationAddedAsync(NotificationItem notification) { }
    protected virtual async Task OnNotificationReadAsync(NotificationItem notification) { }
    protected virtual async Task OnNotificationsReadAsync(List<NotificationItem> notifications) { }
    protected virtual async Task OnNotificationDeletedAsync(NotificationItem notification) { }
    protected virtual async Task OnNotificationsClearedAsync(List<NotificationItem> notifications) { }
    protected virtual async Task OnNotificationsExpiredAsync(List<NotificationItem> notifications) { }

    public virtual void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

public class NotificationItem
{
    public string Id { get; set; } = "";
    public string? Title { get; set; }
    public string Message { get; set; } = "";
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public object? Data { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}