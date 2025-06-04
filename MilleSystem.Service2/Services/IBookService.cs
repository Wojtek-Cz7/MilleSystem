using MilleSystem.Service2.Models;

namespace MilleSystem.Service2.Services;

public interface IBookService
{
    Task<List<Book>> GetAllBooksAsync();
}