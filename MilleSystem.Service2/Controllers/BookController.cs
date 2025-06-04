using Microsoft.AspNetCore.Mvc;
using MilleSystem.Service2.Services;

namespace MilleSystem.Service2.Controllers;

[ApiController]
[Route("[controller]")]
public class BookController : ControllerBase
{
    private readonly ILogger<BookController> _logger;
    private readonly IBookService _bookService;

    public BookController(ILogger<BookController> logger, IBookService bookService)
    {
        _logger = logger;
        _bookService = bookService;
    }

    [HttpGet("GetAll", Name = "GetAllBooks")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var books = await _bookService.GetAllBooksAsync();
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting books");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}