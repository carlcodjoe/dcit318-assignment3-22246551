using System.Text.Json;

// IInventoryEntity is a marker interface: it doesn't do much on its own,
// but it guarantees anything logged by InventoryLogger<T> at least has an
// Id, which is what makes generic logging across different entity types
// possible without the logger needing to know their full shape.
interface IInventoryEntity
{
    int Id { get; }
}

// InventoryItem is a record because inventory snapshots shouldn't mutate
// after they're logged — if the quantity changes, that's a new entry,
// not an edit to history. Positional syntax keeps the declaration compact
// while still giving us immutability and value equality for free.
record InventoryItem(int Id, string Name, int Quantity, DateTime DateAdded) : IInventoryEntity;

// InventoryLogger is generic and constrained to IInventoryEntity so it can
// log any record type that has an Id, not just InventoryItem specifically.
// Persistence is handled with JSON rather than plain text because it
// round-trips structured data (including DateTime) without us having to
// hand-write a custom parser.
class InventoryLogger<T> where T : IInventoryEntity
{
    private List<T> _log = new List<T>();
    private string _filePath;

    public InventoryLogger(string filePath)
    {
        _filePath = filePath;
    }

    public void Add(T item)
    {
        _log.Add(item);
    }

    public List<T> GetAll()
    {
        return _log;
    }

    public void SaveToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(_log, new JsonSerializerOptions { WriteIndented = true });
            using (var writer = new StreamWriter(_filePath))
            {
                writer.Write(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save inventory to file: {ex.Message}");
        }
    }

    public void LoadFromFile()
    {
        try
        {
            using (var reader = new StreamReader(_filePath))
            {
                string json = reader.ReadToEnd();
                var items = JsonSerializer.Deserialize<List<T>>(json);
                _log = items ?? new List<T>();
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"No existing inventory file found at {_filePath}. Starting with an empty log.");
            _log = new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load inventory from file: {ex.Message}");
            _log = new List<T>();
        }
    }
}

// InventoryApp simulates a real usage flow: seed data, persist it, then
// (crucially) prove persistence actually works by loading it back into
// a fresh logger rather than just reading from the same in-memory list.
class InventoryApp
{
    private InventoryLogger<InventoryItem> _logger = new InventoryLogger<InventoryItem>("inventory_log.json");

    public void SeedSampleData()
    {
        _logger.Add(new InventoryItem(1, "Office Chair", 15, DateTime.Now));
        _logger.Add(new InventoryItem(2, "Standing Desk", 8, DateTime.Now));
        _logger.Add(new InventoryItem(3, "Monitor Arm", 25, DateTime.Now));
        _logger.Add(new InventoryItem(4, "Keyboard", 40, DateTime.Now));
        _logger.Add(new InventoryItem(5, "Webcam", 12, DateTime.Now));
    }

    public void SaveData()
    {
        _logger.SaveToFile();
    }

    public void LoadData()
    {
        _logger.LoadFromFile();
    }

    public void PrintAllItems()
    {
        foreach (var item in _logger.GetAll())
        {
            Console.WriteLine($"  ID: {item.Id}, Name: {item.Name}, Quantity: {item.Quantity}, Added: {item.DateAdded:yyyy-MM-dd}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var app = new InventoryApp();
        app.SeedSampleData();
        app.SaveData();
        Console.WriteLine("Data saved to inventory_log.json.");

        // Simulate a brand new session by creating a fresh InventoryApp
        // instance — this guarantees PrintAllItems() below can only be
        // showing data that actually survived the round trip to disk,
        // not data left over in memory from SeedSampleData().
        app = new InventoryApp();
        app.LoadData();

        Console.WriteLine("\nData loaded from file (simulating a new session):");
        app.PrintAllItems();
    }
}