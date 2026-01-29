using ETicaret.Models;

namespace ETicaret.Services
{
    public interface INotificationService
    {
        event Action? OnChange;
        IReadOnlyList<NotificationModel> Notifications { get; }
        void ShowInfo(string message);
        void ShowSuccess(string message);
        void ShowDanger(string message);
        void ShowWarning(string message);
        void StartClosing(Guid id);
        void Remove(Guid id);
        void Clear();
    }

    public class NotificationService : INotificationService
    {
        private readonly List<NotificationModel> _notifications = new();

        public event Action? OnChange;

        public IReadOnlyList<NotificationModel> Notifications => _notifications.AsReadOnly();

        public void ShowInfo(string message) => Show(message, NotificationType.Info);
        public void ShowSuccess(string message) => Show(message, NotificationType.Success);
        public void ShowDanger(string message) => Show(message, NotificationType.Danger);
        public void ShowWarning(string message) => Show(message, NotificationType.Warning);

        private void Show(string message, NotificationType type)
        {
            var notification = new NotificationModel
            {
                Message = message,
                Type = type
            };

            _notifications.Add(notification);
            OnChange?.Invoke();

            // 5 saniye sonra otomatik kaldır
            _ = StartClosingAfterDelay(notification.Id, 5000);
        }

        private async Task StartClosingAfterDelay(Guid id, int delay)
        {
            await Task.Delay(delay);
            StartClosing(id);

            await Task.Delay(300);
            Remove(id);
        }

        public void StartClosing(Guid id)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                notification.IsClosing = true;
                OnChange?.Invoke();
            }
        }

        public void Remove(Guid id)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                _notifications.Remove(notification);
                OnChange?.Invoke();
            }
        }

        public void Clear()
        {
            _notifications.Clear();
            OnChange?.Invoke();
        }
    }
}
