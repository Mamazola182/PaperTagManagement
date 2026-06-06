using BE.Models;
using BE.Services;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace BE.Controllers
{
    [Authorize]
    public class TagController : Controller
    {
         private readonly ITagServices _tagService;
        private readonly ITokenServices _tokenService;
        private readonly IAuditLogsServices _auditLogService;
        public TagController(ITagServices tagService, ITokenServices tokenService,IAuditLogsServices auditLogsServices)
        {
            _tagService = tagService;
            _tokenService = tokenService;
            _auditLogService = auditLogsServices;
        }
        [EnableQuery]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            try
            {
                var tags = _tagService.GetAllTags();

                if (tags == null || !tags.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No tags found."
                    });
                }

                return Ok(tags);
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
        [HttpGet("api/Tag/{id:int}")]
        public async Task<IActionResult> Detail([FromRoute] int id)
        {
            try
            {
                var acc = await _tagService.GetTagByIdAsync(id);
                if (acc == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No tag found."
                    });
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = acc
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
        public async Task<IActionResult> Post([FromBody] Tag acc)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                acc.TagId=(short)( _tagService.GetAllTags().Max(a=>a.TagId)+1);
                await _tagService.AddTagAsync(acc);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Post",
                    EntityName = "Tag",
                    EntityId = acc.TagId.ToString(),
                    BeforeData = "",
                    AfterData = JsonSerializer.Serialize(acc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogService.CreateAuditLog(al);
                return Created(string.Empty, new
                {
                    status = StatusCodes.Status201Created,
                    message = "Success",
                    detail = "Tag created successfully.",
                    data = acc
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
        [HttpPut("api/Tag/{id:int}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Tag acc)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var existing = await _tagService.GetTagByIdAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No tag found."
                    });
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Put",
                    EntityName = "Tag",
                    EntityId = acc.TagId.ToString(),
                    BeforeData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    AfterData = JsonSerializer.Serialize(acc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogService.CreateAuditLog(al);
                existing.TagName = acc.TagName;
                existing.Note = acc.Note;
                await _tagService.UpdateTagAsync(existing);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success.",
                    detail ="Tag updated successfully.",
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
        [HttpDelete("api/Tag/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                if(await _tagService.IsTagUsingAsync(id))
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Unauthorized",
                        error = "This tags was used. You are not allowed to delete."
                    });
                }
                var existing= await _tagService.GetTagByIdAsync(id);
                await _tagService.DeleteTagAsync(id);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Delete",
                    EntityName = "Tag",
                    EntityId = existing.TagId.ToString(),
                    BeforeData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    AfterData = "",
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogService.CreateAuditLog(al);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    detail = "Delete tag successfully.",
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
        [HttpGet("api/Tag/AllNews/{id:int}")]
        public async Task<IActionResult> NewsForTag([FromRoute] int id)
        {
            try
            {
                var acc = await _tagService.GetAllNewsForTag(id);
                if (acc == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No tag found."
                    });
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "Success",
                    data = acc
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
    }
}
