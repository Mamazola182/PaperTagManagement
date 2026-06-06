using BE.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace CoreAPI.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly FunewsManagementContext _context;
        private static readonly ConcurrentDictionary<string, int> _connections = new();

        public NotificationHub(FunewsManagementContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                // 🔍 Debug: In ra tất cả claims
                var claims = Context.User?.Claims.ToList();
                Console.WriteLine("=== ALL CLAIMS ===");
                if (claims != null && claims.Any())
                {
                    foreach (var claim in claims)
                    {
                        Console.WriteLine($"  Type: {claim.Type}, Value: {claim.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("  ⚠️ NO CLAIMS FOUND!");
                }

                // Thử nhiều cách lấy email
                var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value
                         ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value
                         ?? Context.User?.FindFirst("email")?.Value
                         ?? Context.User?.Identity?.Name;

                Console.WriteLine($"📧 Extracted Email: {email}");

                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _context.SystemAccounts
                        .FirstOrDefaultAsync(a => a.AccountEmail == email);

                    if (user != null)
                    {
                        _connections[Context.ConnectionId] = user.AccountId;
                        Console.WriteLine($"✅ User {user.AccountId} ({email}) connected. Total connections: {_connections.Count}");

                        // Gửi thông báo xác nhận kết nối
                        await Clients.Caller.SendAsync("Connected", new
                        {
                            accountId = user.AccountId,
                            message = "Connected successfully"
                        });
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Không tìm thấy account cho email: {email}");
                        await Clients.Caller.SendAsync("ConnectionError", "Account not found");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Không có claim email trong JWT.");
                    await Clients.Caller.SendAsync("ConnectionError", "No email in token");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in OnConnectedAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out int accountId))
            {
                Console.WriteLine($"❌ User {accountId} disconnected. Remaining: {_connections.Count}");
            }

            if (exception != null)
            {
                Console.WriteLine($"Disconnection error: {exception.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Method để test connection
        public async Task TestConnection()
        {
            var accountId = _connections.TryGetValue(Context.ConnectionId, out var id) ? id : 0;
            await Clients.Caller.SendAsync("TestResponse", new
            {
                accountId,
                connectionId = Context.ConnectionId,
                message = "Test successful"
            });
        }

        public static async Task SendNewArticleNotification(
            IHubContext<NotificationHub> hubContext,
            int creatorAccountId,
            string articleTitle,
            string articleId,
            string authorName)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "new_article",
                    title = "Bài viết mới",
                    message = $"{authorName} vừa đăng bài viết mới: \"{articleTitle}\"",
                    relatedId = articleId,
                    author = authorName,
                    timeAgo = "Vừa xong",
                    createdAt = DateTime.UtcNow,
                    isRead = false
                };

                var targetConnections = _connections
                    .Where(c => c.Value != creatorAccountId)
                    .Select(c => c.Key)
                    .ToList();

                Console.WriteLine($"📢 Total connections: {_connections.Count}");
                Console.WriteLine($"📢 Creator AccountId: {creatorAccountId}");
                Console.WriteLine($"📢 Target connections: {targetConnections.Count}");

                foreach (var conn in _connections)
                {
                    Console.WriteLine($"   - ConnectionId: {conn.Key}, AccountId: {conn.Value}");
                }

                if (targetConnections.Any())
                {
                    await hubContext.Clients
                        .Clients(targetConnections)
                        .SendAsync("ReceiveNotification", notification);

                    Console.WriteLine($"✅ Notification sent to {targetConnections.Count} users.");
                }
                else
                {
                    Console.WriteLine("⚠️ No target connections found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending notification: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}