using BE.DTO;
using BE.Models;
using BE.Services;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using System.Text.Json;

namespace BE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemAccountController : Controller
    {
        private readonly ISystemAccountServices _systemAccountService;
        private readonly ITokenServices _tokenService;
        private readonly IAuditLogsServices _auditLogsServices;
        public SystemAccountController(ISystemAccountServices systemAccountService, ITokenServices tokenService,IAuditLogsServices auditLogsServices)
        {
            _systemAccountService = systemAccountService;
            _tokenService = tokenService;
            _auditLogsServices = auditLogsServices;
        }
        [EnableQuery]
        public IActionResult Get()
        {
            try
            {
                var accounts = _systemAccountService.GetAllSystemAccounts();

                if (accounts == null || !accounts.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No system accounts found."
                    });
                }

                return Ok(accounts);
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
        [HttpGet("api/SystemAccount/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Detail([FromRoute] int id)
        {
            try
            {
                var acc = await _systemAccountService.GetSystemAccountByIdAsync(id);
                if (acc == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No system account found."
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
        public async Task<IActionResult> Post([FromBody] SystemAccount acc)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (await _systemAccountService.GetSystemAccountByEmailAsync(acc.AccountEmail) != null)
            {
                return Conflict(new
                {
                    status = StatusCodes.Status409Conflict,
                    message = "Error",
                    error = "Email already exists."
                });
            }
            try
            {
                acc.AccountId=(short)(_systemAccountService.GetAllSystemAccounts().Max(a => a.AccountId) + 1);
                await _systemAccountService.AddSystemAccountAsync(acc);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Post",
                    EntityName = "SystemAccount",
                    EntityId = acc.AccountId.ToString(),
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
                    detail = "System account created successfully.",
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
        [HttpPut("api/SystemAccount/{id:int}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] SystemAccount acc)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var existing = await _systemAccountService.GetSystemAccountByIdAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No system accounts found."
                    });
                var existingByEmail = await _systemAccountService.GetSystemAccountByEmailAsync(acc.AccountEmail);
                if (existingByEmail!=null&& existingByEmail.AccountId != existing.AccountId)
                {
                    return Conflict(new
                    {
                        status = StatusCodes.Status409Conflict,
                        message = "Error",
                        error = "Email already exists."
                    });
                }
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Put",
                    EntityName = "SystemAccount",
                    EntityId = acc.AccountId.ToString(),
                    BeforeData = JsonSerializer.Serialize(existingByEmail, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    AfterData = JsonSerializer.Serialize(acc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                existing.AccountName = acc.AccountName;
                existing.AccountRole = acc.AccountRole;
                await _systemAccountService.UpdateSystemAccountAsync(existing);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "success.",
                    detail ="System account updated successfully.",
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
        [HttpPut("api/SystemAccount/ChangePassword/{id:int}")]
        public async Task<IActionResult> ChangePassword([FromRoute] int id, [FromBody] ChangePassword chance)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var existing = await _systemAccountService.GetSystemAccountByIdAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No system accounts found."
                    });
                if(existing.AccountPassword != chance.oldPassword)
                    return BadRequest(new
                    {
                        status = StatusCodes.Status400BadRequest,
                        message = "Error",
                        error = "Old password is incorrect."
                    });
                if(chance.newPassword != chance.confirmPassword)
                    return BadRequest(new
                    {
                        status = StatusCodes.Status400BadRequest,
                        message = "Error",
                        error = "New password and confirm password do not match."
                    });
                var old = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                existing.AccountPassword = chance.newPassword;
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Put",
                    EntityName = "SystemAccount",
                    EntityId = existing.AccountId.ToString(),
                    BeforeData = old,
                    AfterData = JsonSerializer.Serialize(existing, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }),
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogsServices.CreateAuditLog(al);
                await _systemAccountService.UpdateSystemAccountAsync(existing);
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "success.",
                    detail = "System account updated successfully.",
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
        [HttpDelete("api/SystemAccount/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                if(await _systemAccountService.IsAcountCreateNew(id))
                    return NotFound(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        message = "Unauthorized",
                        error = "This account was created a news article. You are not allowed to delete."
                    });
                var exist=await _systemAccountService.GetSystemAccountByIdAsync(id);
                await _systemAccountService.DeleteSystemAccountAsync(id);
                AuditLog al = new AuditLog
                {
                    UserId = _tokenService.GetIdFromToken(),
                    UserName = _tokenService.GetEmailFromToken(),
                    Action = "Delete",
                    EntityName = "SystemAccount",
                    EntityId = exist.AccountId.ToString(),
                    BeforeData = JsonSerializer.Serialize(exist, new JsonSerializerOptions
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
                    detail = "Delete system account successfully.",
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
