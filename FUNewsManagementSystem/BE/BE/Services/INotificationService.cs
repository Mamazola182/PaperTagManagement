using BE.Models;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetRecentNotifications(int accountId, int count = 10);
        Task<int> GetUnreadCount(int accountId);
        Task MarkAsRead(string notificationId, int accountId);
        Task MarkAllAsRead(int accountId);
        Task CreateNewArticleNotification(int creatorAccountId, NewsArticle article);
    }
}
