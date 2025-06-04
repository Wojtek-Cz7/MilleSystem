using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MilleSystem.Service2.Controllers;

[ApiController]
[Route("[controller]")]
public class BookController : ControllerBase
{
    private readonly ILogger<BookController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _service1BaseUrl;

    private class TaskResponse
    {
        public Guid TaskId { get; set; }
    }

    private class TaskStatusResponse
    {
        public Guid TaskId { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class Book
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
    }

    public BookController(ILogger<BookController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        // TO-DO to configuration
        _service1BaseUrl = "http://localhost:5001";
    }

    [HttpGet("GetAll", Name = "GetAllBooks")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Starting request to Service 1");

            // Krok 1: Zainicjuj zadanie w Serwisie 1
            var initResponse = await _httpClient.GetAsync($"{_service1BaseUrl}/Book/GetAll");
            
            if (!initResponse.IsSuccessStatusCode)
            {
                var errorContent = await initResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Error from Service 1: {errorContent}");
                return StatusCode((int)initResponse.StatusCode, $"Error from Service 1: {errorContent}");
            }

            // Deserializuj odpowiedŸ, aby uzyskaæ TaskId
            var responseContent = await initResponse.Content.ReadAsStringAsync();
            var taskInfo = JsonSerializer.Deserialize<TaskResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (taskInfo == null || taskInfo.TaskId == Guid.Empty)
            {
                _logger.LogError("Invalid task ID received from Service 1");
                return BadRequest("Invalid response from Service 1");
            }

            Guid taskId = taskInfo.TaskId;
            _logger.LogInformation($"Task initiated with ID: {taskId}");

            // Krok 2: Sprawdzaj status zadania, a¿ bêdzie gotowe
            bool isCompleted = false;
            while (!isCompleted)
            {
                _logger.LogInformation($"Checking status of task {taskId}");
                
                var statusResponse = await _httpClient.GetAsync($"{_service1BaseUrl}/Book/Status/{taskId}");
                if (!statusResponse.IsSuccessStatusCode)
                {
                    var errorContent = await statusResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Error checking task status: {errorContent}");
                    return StatusCode((int)statusResponse.StatusCode, $"Error checking task status: {errorContent}");
                }

                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                var statusInfo = JsonSerializer.Deserialize<TaskStatusResponse>(statusContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (statusInfo != null && statusInfo.IsCompleted)
                {
                    isCompleted = true;
                    _logger.LogInformation($"Task {taskId} is completed");
                }
                else
                {
                    _logger.LogInformation($"Task {taskId} is still processing, waiting 5 seconds");
                    await Task.Delay(5000); // Czekaj 5 sekund przed nastêpnym sprawdzeniem
                }
            }

            // Krok 3: Pobierz wynik zadania
            var resultResponse = await _httpClient.GetAsync($"{_service1BaseUrl}/Book/Result/{taskId}");
            if (!resultResponse.IsSuccessStatusCode)
            {
                var errorContent = await resultResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Error fetching task result: {errorContent}");
                return StatusCode((int)resultResponse.StatusCode, $"Error fetching task result: {errorContent}");
            }

            var resultContent = await resultResponse.Content.ReadAsStringAsync();
            var books = JsonSerializer.Deserialize<List<Book>>(resultContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation($"Successfully retrieved {books?.Count ?? 0} books from Service 1");
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request to Service 1");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}