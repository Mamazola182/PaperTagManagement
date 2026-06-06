using BE.Models;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly FunewsManagementContext _context;
        public NotificationController(INotificationService notificationService,FunewsManagementContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        // 🔹 Lấy AccountId từ JWT claims
        private int GetAccountId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("User email not found in token.");
            var account = _context.SystemAccounts.FirstOrDefault(a => a.AccountEmail == email);
            return account.AccountId;
        }

        // GET: api/Notification/recent
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] int count = 10)
        {
            try
            {
                int accountId = GetAccountId();

                var notifications = await _notificationService.GetRecentNotifications(accountId, count);

                var result = notifications.Select(n => new
                {
                    id = n.NotificationId,
                    type = n.Type,
                    title = n.Title,
                    message = n.Message,
                    relatedId = n.RelatedId,
                    relatedType = n.RelatedType,
                    isRead = n.IsRead,
                    createdDate = n.CreatedDate,
                    timeAgo = GetTimeAgo(n.CreatedDate)
                });

                return Ok(new { success = true, data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/Notification/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                int accountId = GetAccountId();
                var count = await _notificationService.GetUnreadCount(accountId);
                return Ok(new { success = true, count });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/Notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                int accountId = GetAccountId();
                await _notificationService.MarkAsRead(id, accountId);
                return Ok(new { success = true, message = "Notification marked as read" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/Notification/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                int accountId = GetAccountId();
                await _notificationService.MarkAllAsRead(accountId);
                return Ok(new { success = true, message = "All notifications marked as read" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";

            var timeSpan = DateTime.Now - dateTime.Value;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            else
                return dateTime.Value.ToString("dd/MM/yyyy");
        }
    }
}
