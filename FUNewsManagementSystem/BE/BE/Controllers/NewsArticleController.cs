using Azure;
using BE.Models;
using BE.Services;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using System.Security.Cryptography;
namespace BE.Controllers
{
    [Authorize(Roles = "Staff")]
    public class NewsArticleController : Controller
    {
        private readonly INewArticleServices _newsArticleService;
        private readonly ITagServices _tagService;
        private readonly ISystemAccountServices _systemAccountService;
        private readonly INotificationService _notificationService;
        private readonly ITokenServices _tokenService;
        private readonly IAuditLogsServices _auditLogsServices;
        public NewsArticleController(INewArticleServices newsArticleService, ITagServices tagService, ISystemAccountServices systemAccountService, INotificationService notificationService, ITokenServices tokenService, IAuditLogsServices auditLogsServices)
        {
            _newsArticleService = newsArticleService;
            _tagService = tagService;
            _systemAccountService = systemAccountService;
            _notificationService = notificationService;
            _tokenService = tokenService;
            _auditLogsServices = auditLogsServices;
        }
        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            try
            {
                var news = _newsArticleService.GetAllNewsArticles();

                if (news == null || !news.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No news found."
                    });
                }

                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }
        [HttpGet("api/NewsArticle/{id}")]
        public async Task<IActionResult> Detail([FromRoute] string id)
        {
            try
            {
                var news = await _newsArticleService.GetNewsArticleByIdAsync(id);
                if (news == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No news found."
                    });
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = news
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NewsArticle news)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Error",
                        error = "Token is missing email information."
                    });
                }
                var createdUser = await _systemAccountService.GetSystemAccountByEmailAsync(email);
                news.CreatedById = (short)createdUser.AccountId;
                news.CreatedDate = DateTime.Now;
                if (news.NewsStatus == null)
                {
                    news.NewsStatus = true;
                }

                var allNews = await _newsArticleService.GetAllNewsArticles().ToListAsync();
                int nextId = 1;

                if (allNews != null && allNews.Any())
                {
                    nextId = allNews
                        .AsEnumerable()
                        .Select(n => int.TryParse(n.NewsArticleId ?? "0", out var id) ? id : 0)
                        .Max() + 1;
                }

                news.NewsArticleId = nextId.ToString();

                if (news.Tags != null && news.Tags.Any())
                {
                    var existingTagIds = news.Tags.Select(t => t.TagId).ToList();
                    var existingTags = await _tagService.GetAllTags()
                        .Where(t => existingTagIds.Contains(t.TagId))
                        .ToListAsync();

                    news.Tags = existingTags;
                }

                await _newsArticleService.AddNewsArticleAsync(news);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Post",
                    EntityName = "NewsArticle",
                    EntityId = news.NewsArticleId,
                    BeforeData = "",
                    AfterData = JsonSerializer.Serialize(news, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                await _notificationService.CreateNewArticleNotification(
                    createdUser.AccountId,
                    news
                );
                return Created(string.Empty, new
                {
                    status = StatusCodes.Status201Created,
                    message = "Success",
                    detail = "News article created successfully.",
                    data = news
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }

        [HttpPut("api/NewsArticle/{id}")]
        public async Task<IActionResult> Put([FromRoute] string id, [FromBody] NewsArticle news)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Error",
                        error = "Token is missing email information."
                    });
                }
                var updatedUser = await _systemAccountService.GetSystemAccountByEmailAsync(email);
                var existing = await _newsArticleService.GetNewsArticleByIdAsync(id);
                var old = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                if (existing == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No news article found."
                    });
                existing.NewsTitle = news.NewsTitle;
                existing.Headline = news.Headline;
                existing.NewsContent = news.NewsContent;
                existing.NewsSource = news.NewsSource;
                existing.CategoryId = news.CategoryId;
                existing.NewsStatus = news.NewsStatus;
                existing.UpdatedById = (short)updatedUser.AccountId;
                existing.ModifiedDate = DateTime.Now;
                existing.Tags.Clear();

                if (news.Tags != null && news.Tags.Any())
                {
                    var tagIds = news.Tags.Select(t => t.TagId).ToList();
                    foreach (var tagId in tagIds)
                    {
                        var dbTags = await _tagService.GetTagByIdAsync(tagId);
                        existing.Tags.Add(dbTags);
                    }
                }
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Put",
                    EntityName = "NewsArticle",
                    EntityId = existing.NewsArticleId,
                    BeforeData = old,
                    AfterData = JsonSerializer.Serialize(news, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                await _newsArticleService.UpdateNewsArticleAsync(existing);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success.",
                    detail = "News article updated successfully.",
                    data = existing
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }

        [HttpDelete("api/NewsArticle/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            try
            {
                var existing = await _newsArticleService.GetNewsArticleByIdAsync(id);
                await _newsArticleService.DeleteNewsArticleAsync(id);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Delete",
                    EntityName = "NewsArticle",
                    EntityId = existing.NewsArticleId,
                    BeforeData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    AfterData = "",
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    detail = "Delete news artile successfully.",
                }); ;
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }


    }
}
