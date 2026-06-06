using BE.Models;
using BE.Services;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Cryptography;
using System.Text.Json;

namespace BE.Controllers
{
    [Authorize(Roles = "Staff")]
    public class CategoryController : Controller
    {
        private readonly ICategoryServices _categoryService;
        private readonly ITokenServices _tokenServices;
        private readonly IAuditLogsServices _auditLogsServices;
        public CategoryController(ICategoryServices categoryService,ITokenServices tokenServices,IAuditLogsServices auditLogsServices)
        {
            _categoryService = categoryService;
            _auditLogsServices = auditLogsServices;
            _tokenServices = tokenServices;
        }
        [EnableQuery]
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var categories = _categoryService.GetAllCategories();

                if (categories == null || !categories.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No categories found."
                    });
                }

                return Ok(categories);
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
        [HttpGet("api/Category/{id:int}")]
        public async Task<IActionResult> Detail([FromRoute] int id)
        {
            try
            {
                var acc = await _categoryService.GetCategoryDTOByIdAsync(id);
                if (acc == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No category account found."
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
        [HttpPost("api/CreateCategory")]
        public async Task<IActionResult> Post([FromBody] Category acc)
        {   
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {   
                await _categoryService.AddCategoryAsync(acc);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenServices.GetIdFromToken(),
                    UserName = _tokenServices.GetEmailFromToken(),
                    Action = "Post",
                    EntityName = "Category",
                    EntityId = acc.CategoryId.ToString(),
                    BeforeData = "",
                    AfterData = JsonSerializer.Serialize(acc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                return Created(string.Empty, new
                {
                    status = StatusCodes.Status201Created,
                    message = "Success",
                    detail = "Category created successfully.",
                    data = acc
                });
            }
            catch (Exception ex)
            {
                if (acc.ParentCategoryId != null && await _categoryService.GetCategoryByIdAsync((int)acc.ParentCategoryId) == null)
                {
                    acc.ParentCategoryId = null;
                    await _categoryService.AddCategoryAsync(acc);

                    return Created(string.Empty, new
                    {
                        status = StatusCodes.Status201Created,
                        message = "Success",
                        detail = "No parent id found.Category created successfully with no parent id.",
                        data = acc
                    });
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = StatusCodes.Status500InternalServerError,
                    message = "Error",
                    error = "An internal server error has occurred. Please wait and try again later.",
                    detail = ex.Message
                });
            }
        }
        [HttpPut("api/Category/{id:int}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Category acc)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if(acc.ParentCategoryId!=null&&await _categoryService.GetCategoryByIdAsync((int)acc.ParentCategoryId)==null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No parent id found."
                    });
                if (existing == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No system accounts found."
                    });
                var old= JsonSerializer.Serialize(existing, new JsonSerializerOptions
                {
                    WriteIndented = true
                });     
                if (await _categoryService.IsCategoryUsing(id))
                {
                    existing.CategoryName = acc.CategoryName;
                    existing.CategoryDesciption = acc.CategoryDesciption;
                    existing.IsActive = acc.IsActive;
                    await _categoryService.UpdateCategoryAsync(existing);
                    AuditLog al = new AuditLog
                    {
                        UserId = _tokenServices.GetIdFromToken(),
                        UserName = _tokenServices.GetEmailFromToken(),
                        Action = "Put",
                        EntityName = "Category",
                        EntityId = existing.CategoryId.ToString(),
                        BeforeData = old,
                        AfterData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }),
                        Timestamp = DateTime.UtcNow
                    };
                    await _auditLogsServices.CreateAuditLog(al);
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "success.",
                        detail = "Category updated successfully. But the category is using so we can not change ParentCategoryID",
                        data = existing
                    });
                }
                else
                {
                    existing.CategoryName = acc.CategoryName;
                    existing.CategoryDesciption = acc.CategoryDesciption;
                    existing.ParentCategoryId = acc.ParentCategoryId;
                    existing.IsActive = acc.IsActive;
                    await _categoryService.UpdateCategoryAsync(existing);
                    AuditLog al = new AuditLog
                    {
                        UserId = _tokenServices.GetIdFromToken(),
                        UserName = _tokenServices.GetEmailFromToken(),
                        Action = "Put",
                        EntityName = "Category",
                        EntityId = existing.CategoryId.ToString(),
                        BeforeData = old,
                        AfterData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }),
                        Timestamp = DateTime.UtcNow
                    };
                    await _auditLogsServices.CreateAuditLog(al);
                    return Ok(new
                    {
                        status = StatusCodes.Status200OK,
                        message = "success.",
                        detail = "Category updated successfully.",
                        data = existing
                    });
                }
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
        [HttpDelete("api/Category/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var existing = await _categoryService.GetCategoryByIdAsync(id);
                if (await _categoryService.IsCategoryUsing(id))
                    return NotFound(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Unauthorized",
                        error = "This category is in use. You are not allowed to delete."
                    });
                await _categoryService.DeleteCategoryAsync(id);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenServices.GetIdFromToken(),
                    UserName = _tokenServices.GetEmailFromToken(),
                    Action = "Delete",
                    EntityName = "Category",
                    EntityId = existing.CategoryId.ToString(),
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
                    detail = "Delete category successfully.",
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
        [HttpGet("api/Category/CountArticle/{id:int}")]
        public async Task<IActionResult> CountArticle([FromRoute] int id)
        {
            try
            {
                var news = await _categoryService.CountArticlesInCategory(id);
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
        [HttpGet("api/activateCategory")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivateCategory()
        {
            try
            {
                var categories = _categoryService.GetAllActiveCategories();

                if (categories == null || !categories.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No categories found."
                    });
                }

                return Ok(categories);
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
        [HttpGet("api/Category/AllNews/{id:int}")]
        public async Task<IActionResult> NewsForCategory([FromRoute] int id)
        {
            try
            {
                var acc = await _categoryService.GetArticlesInCategory(id);
                if (acc == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No article found."
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
