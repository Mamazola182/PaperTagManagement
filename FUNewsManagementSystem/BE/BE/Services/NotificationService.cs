using BE.Models;
using CoreAPI.Hubs;
using CoreAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
namespace CoreAPI.Services
{
    public class NotificationService:INotificationService
    {
        private readonly FunewsManagementContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            FunewsManagementContext context,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<Notification>> GetRecentNotifications(int accountId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.AccountId == accountId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCount(int accountId)
        {
            return await _context.Notifications
                .Where(n => n.AccountId == accountId && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsRead(string notificationId, int accountId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.AccountId == accountId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(int accountId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.AccountId == accountId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateNewArticleNotification(int creatorAccountId, NewsArticle article)
        {
            // Get all active users except the creator
            var targetUsers = await _context.SystemAccounts
                .Where(a => a.AccountId != creatorAccountId)
                .Select(a => a.AccountId)
                .ToListAsync();

            var notifications = new List<Notification>();
            var now = DateTime.Now;

            foreach (var userId in targetUsers)
            {
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    AccountId = userId,
                    Type = "new_article",
                    Title = "Bài viết mới",
                    Message = $"Có bài viết mới: \"{article.NewsTitle}\"",
                    RelatedId = article.NewsArticleId,
                    RelatedType = "article",
                    IsRead = false,
                    CreatedDate = now,
                    ModifiedDate = now
                };

                notifications.Add(notification);
            }

            if (notifications.Any())
            {
                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();
            }

            // Send real-time notification via SignalR
            var author = await _context.SystemAccounts
                .FirstOrDefaultAsync(a => a.AccountId == creatorAccountId);

            await NotificationHub.SendNewArticleNotification(
                _hubContext,
                creatorAccountId,
                article.NewsTitle,
                article.NewsArticleId,
                author?.AccountName ?? "Người dùng"
            );
        }
    }
}
