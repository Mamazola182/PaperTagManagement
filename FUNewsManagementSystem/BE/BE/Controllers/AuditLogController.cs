using BE.Services;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace CoreAPI.Controllers


{   [Route("api/AuditLog")]
    [ApiController]
    
    [Authorize(Roles = "Admin")]



    public class AuditLogController : Controller
    {
        private readonly IAuditLogsServices _auditLogsServices;
        public AuditLogController(IAuditLogsServices auditLogsServices)
        {
            _auditLogsServices = auditLogsServices;
        }
        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            try
            {
                var auditlogs = _auditLogsServices.GetAuditLogs();

                if (auditlogs == null || !auditlogs.Any())
                {
                    return NotFound(new
                    {
                        status = StatusCodes.Status404NotFound,
                        message = "Error",
                        error = "No categories found."
                    });
                }

                return Ok(auditlogs);
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
