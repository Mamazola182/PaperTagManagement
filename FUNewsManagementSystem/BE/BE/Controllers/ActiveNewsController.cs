using BE.DTO;
using BE.Models;
using BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.EntityFrameworkCore;

namespace BE.Controllers
{
    public class ActiveNewsController : Controller
    {
        private readonly INewArticleServices _newsArticleService;
        private readonly ITagServices _tagService;
        private readonly ISystemAccountServices _systemAccountService;
        public ActiveNewsController(INewArticleServices newsArticleService, ITagServices tagService, ISystemAccountServices systemAccountService)
        {
            _newsArticleService = newsArticleService;
            _tagService = tagService;
            _systemAccountService = systemAccountService;
        }
        [EnableQuery]
        public IActionResult Get()
        {
            try
            {
                var news = _newsArticleService.GetActiveNews();

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
        [EnableQuery]
        public SingleResult<ActiveNews> Get([FromODataUri] string key)
        {
            var result = _newsArticleService.GetActiveNews().Where(n => n.NewsArticleId == key);
            return SingleResult.Create(result);
        }
    }
}
