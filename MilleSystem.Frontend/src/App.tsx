import { useState, useEffect } from 'react'
import './App.css'

// Definicja typu dla ksiπøki zgodna z modelem z backend'u
interface Book {
    id: string;
    title: string;
    author: string;
    year: number;
    genre: string;
}

function App() {
    const [books, setBooks] = useState<Book[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);

    // Funkcja do pobierania danych
    const fetchBooks = async () => {
        try {
            setLoading(true);
            setError(null);

            // URL do Service 2 - endpoint GetAll
            const response = await fetch('http://localhost:5002/Book/GetAll');

            if (!response.ok) {
                throw new Error(`Error: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();
            setBooks(data);

            // Zapisujemy dane w localStorage, øeby mÛc je odczytaÊ po odúwieøeniu (F5)
            localStorage.setItem('books', JSON.stringify(data));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'An unknown error occurred');
            console.error('Error fetching books:', err);
        } finally {
            setLoading(false);
        }
    };

    // Wykonaj przy pierwszym renderowaniu
    useEffect(() => {
        // Sprawdü najpierw localStorage
        const savedBooks = localStorage.getItem('books');
        if (savedBooks) {
            setBooks(JSON.parse(savedBooks));
        }

        // Pobierz úwieøe dane z API
        fetchBooks();
    }, []);

    return (
        <div className="container">
            <h1>Books List</h1>

            {loading && <p className="loading">Loading books...</p>}

            {error && <div className="error">Error: {error}</div>}

            <div className="books-container">
                {books.length > 0 ? (
                    <table className="books-table">
                        <thead>
                            <tr>
                                <th>Title</th>
                                <th>Author</th>
                                <th>Year</th>
                                <th>Genre</th>
                            </tr>
                        </thead>
                        <tbody>
                            {books.map(book => (
                                <tr key={book.id}>
                                    <td>{book.title}</td>
                                    <td>{book.author}</td>
                                    <td>{book.year}</td>
                                    <td>{book.genre}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                ) : !loading && !error ? (
                    <p>No books found.</p>
                ) : null}
            </div>

            <button onClick={fetchBooks} disabled={loading}>
                {loading ? 'Loading...' : 'Refresh Books'}
            </button>
        </div>
    )
}

export default App