using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace MilleSystem.Service1.Controllers;

[ApiController]
[Route("[controller]")]
public class BookController : ControllerBase
{
    private static readonly List<Book> Books =
    [
        new Book { Id = Guid.NewGuid(), Title = "Diuna", Author = "Frank Herbert", Year = 1965, Genre = "Fantasy" },

        new Book { Id = Guid.NewGuid(), Title = "W³adca Pierœcieni", Author = "J.R.R. Tolkien", Year = 1954, Genre = "Fantasy" },

        new Book { Id = Guid.NewGuid(), Title = "1984", Author = "George Orwell", Year = 1949, Genre = "Dystopia" },

        new Book { Id = Guid.NewGuid(), Title = "Mistrz i Ma³gorzata", Author = "Michai³ Bu³hakow", Year = 1967, Genre = "Powieœæ fantastyczna" },

        new Book { Id = Guid.NewGuid(), Title = "Solaris", Author = "Stanis³aw Lem", Year = 1961, Genre = "Science Fiction" }
    ];

    private readonly ILogger<BookController> _logger;
    private static int _requestCounter = 0;
    private static readonly object _counterLock = new object();

    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<IEnumerable<Book>>> _taskResults = new();
    
    private static readonly ConcurrentDictionary<Guid, bool> _taskStatus = new();

    public static List<Book> BooksCollection => Books;

    public BookController(ILogger<BookController> logger)
    {
        _logger = logger;
    }
  
    [HttpGet("GetAll", Name = "GetAllBooks")]
    public IActionResult GetAll()
    {
        int currentRequestNumber;
        
        lock (_counterLock)
        {
            _requestCounter++;
            currentRequestNumber = _requestCounter;
        }
        
        _logger.LogInformation($"Getting all books. Request number: {currentRequestNumber}");
        
        if (currentRequestNumber % 10 == 0)
        {
            _logger.LogWarning($"Returning error for request number {currentRequestNumber} (every 10th request)");
            return StatusCode(500, "Service error - request failed!");
        }

        Guid taskId = Guid.NewGuid();

        var taskCompletionSource = new TaskCompletionSource<IEnumerable<Book>>();
        _taskResults[taskId] = taskCompletionSource;
        _taskStatus[taskId] = false;
 
        Task.Run(async () => 
        {
            try
            {
                _logger.LogInformation($"Starting long processing for task {taskId}");
                await Task.Delay(60000);

                taskCompletionSource.SetResult(BooksCollection);
                _taskStatus[taskId] = true;
                _logger.LogInformation($"Processing completed for task {taskId}");
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
                _logger.LogError(ex, $"Error during processing task {taskId}");
            }
        });

        return Ok(new { TaskId = taskId });
    }    

    [HttpGet("Status/{taskId}", Name = "GetTaskStatus")]
    public IActionResult GetStatus(Guid taskId)
    {
        if (!_taskResults.ContainsKey(taskId))
        {
            return NotFound($"Task with ID {taskId} not found");
        }
        
        bool isCompleted = _taskStatus.GetValueOrDefault(taskId, false);
        
        return Ok(new { TaskId = taskId, IsCompleted = isCompleted });
    }    

    [HttpGet("Result/{taskId}", Name = "GetTaskResult")]
    public async Task<IActionResult> GetResult(Guid taskId)
    {
        if (!_taskResults.TryGetValue(taskId, out var taskCompletionSource))
        {
            return NotFound($"Task with ID {taskId} not found");
        }
        
        if (!_taskStatus.GetValueOrDefault(taskId, false))
        {
            return BadRequest($"Task {taskId} is still processing. Use the Status endpoint to check readiness.");
        }
        
        try
        {
            var result = await taskCompletionSource.Task;
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving result: {ex.Message}");
        }
    }
}

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
}