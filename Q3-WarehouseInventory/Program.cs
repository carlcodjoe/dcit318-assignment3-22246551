// IInventoryItem is the contract every stockable product must satisfy.
// Quantity has a setter because stock levels change constantly (restocks,
// sales), unlike Id and Name which are fixed once an item is created.
interface IInventoryItem
{
    int Id { get; }
    string Name { get; }
    int Quantity { get; set; }
}

class ElectronicItem : IInventoryItem
{
    public int Id { get; }
    public string Name { get; }
    public int Quantity { get; set; }
    public string Brand { get; }
    public int WarrantyMonths { get; }

    public ElectronicItem(int id, string name, int quantity, string brand, int warrantyMonths)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        Brand = brand;
        WarrantyMonths = warrantyMonths;
    }
}

class GroceryItem : IInventoryItem
{
    public int Id { get; }
    public string Name { get; }
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; }

    public GroceryItem(int id, string name, int quantity, DateTime expiryDate)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        ExpiryDate = expiryDate;
    }
}

// Custom exceptions carry a message constructor so the caller can explain
// exactly what went wrong (which ID, which operation) rather than getting
// a generic "something failed" exception type with no context.
class DuplicateItemException : Exception
{
    public DuplicateItemException(string message) : base(message) { }
}

class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string message) : base(message) { }
}

class InvalidQuantityException : Exception
{
    public InvalidQuantityException(string message) : base(message) { }
}

// InventoryRepository is constrained to IInventoryItem so it can only ever
// hold things that actually behave like stock items. Using a Dictionary
// keyed by Id means duplicate-check and lookup are both fast, instead of
// scanning a list every time.
class InventoryRepository<T> where T : IInventoryItem
{
    private Dictionary<int, T> _items = new Dictionary<int, T>();

    public void AddItem(T item)
    {
        if (_items.ContainsKey(item.Id))
        {
            throw new DuplicateItemException($"An item with ID {item.Id} already exists in inventory.");
        }
        _items[item.Id] = item;
    }

    public T GetItemById(int id)
    {
        if (!_items.ContainsKey(id))
        {
            throw new ItemNotFoundException($"No item found with ID {id}.");
        }
        return _items[id];
    }

    public void RemoveItem(int id)
    {
        if (!_items.ContainsKey(id))
        {
            throw new ItemNotFoundException($"Cannot remove item with ID {id}: not found.");
        }
        _items.Remove(id);
    }

    public List<T> GetAllItems()
    {
        return _items.Values.ToList();
    }

    public void UpdateQuantity(int id, int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new InvalidQuantityException($"Quantity cannot be negative. Attempted to set item {id} to {newQuantity}.");
        }
        var item = GetItemById(id);
        item.Quantity = newQuantity;
    }
}

// WareHouseManager coordinates both inventory types. The generic methods
// here (PrintAllItems, IncreaseStock, RemoveItemById) work with either
// repository type without needing to be duplicated for electronics vs
// groceries — that's the whole point of the constraint on T.
class WareHouseManager
{
    private InventoryRepository<ElectronicItem> _electronics = new InventoryRepository<ElectronicItem>();
    private InventoryRepository<GroceryItem> _groceries = new InventoryRepository<GroceryItem>();

    public void SeedData()
    {
        _electronics.AddItem(new ElectronicItem(1, "Wireless Mouse", 50, "Logitech", 12));
        _electronics.AddItem(new ElectronicItem(2, "Laptop Charger", 30, "Dell", 6));
        _electronics.AddItem(new ElectronicItem(3, "Bluetooth Speaker", 20, "JBL", 24));

        _groceries.AddItem(new GroceryItem(1, "Rice (5kg)", 100, DateTime.Now.AddMonths(8)));
        _groceries.AddItem(new GroceryItem(2, "Cooking Oil (2L)", 60, DateTime.Now.AddMonths(6)));
        _groceries.AddItem(new GroceryItem(3, "Canned Tomatoes", 80, DateTime.Now.AddMonths(12)));
    }

    public void PrintAllItems<T>(InventoryRepository<T> repo) where T : IInventoryItem
    {
        foreach (var item in repo.GetAllItems())
        {
            Console.WriteLine($"  ID: {item.Id}, Name: {item.Name}, Quantity: {item.Quantity}");
        }
    }

    public void IncreaseStock<T>(InventoryRepository<T> repo, int id, int quantity) where T : IInventoryItem
    {
        try
        {
            var item = repo.GetItemById(id);
            repo.UpdateQuantity(id, item.Quantity + quantity);
            Console.WriteLine($"Stock increased for item {id}. New quantity: {item.Quantity}");
        }
        catch (ItemNotFoundException ex)
        {
            Console.WriteLine($"Could not increase stock: {ex.Message}");
        }
        catch (InvalidQuantityException ex)
        {
            Console.WriteLine($"Could not increase stock: {ex.Message}");
        }
    }

    public void RemoveItemById<T>(InventoryRepository<T> repo, int id) where T : IInventoryItem
    {
        try
        {
            repo.RemoveItem(id);
            Console.WriteLine($"Item {id} removed successfully.");
        }
        catch (ItemNotFoundException ex)
        {
            Console.WriteLine($"Could not remove item: {ex.Message}");
        }
    }

    public InventoryRepository<ElectronicItem> Electronics => _electronics;
    public InventoryRepository<GroceryItem> Groceries => _groceries;
}

class Program
{
    static void Main(string[] args)
    {
        var manager = new WareHouseManager();
        manager.SeedData();

        Console.WriteLine("Grocery Items:");
        manager.PrintAllItems(manager.Groceries);

        Console.WriteLine("\nElectronic Items:");
        manager.PrintAllItems(manager.Electronics);

        Console.WriteLine("\n--- Testing exception handling ---");

        // 1. Add a duplicate item (same ID as an existing electronic item)
        try
        {
            manager.Electronics.AddItem(new ElectronicItem(1, "Duplicate Mouse", 10, "Generic", 3));
        }
        catch (DuplicateItemException ex)
        {
            Console.WriteLine($"Error adding item: {ex.Message}");
        }

        // 2. Remove a non-existent item
        manager.RemoveItemById(manager.Groceries, 99);

        // 3. Update with an invalid (negative) quantity
        try
        {
            manager.Groceries.UpdateQuantity(1, -5);
        }
        catch (InvalidQuantityException ex)
        {
            Console.WriteLine($"Error updating quantity: {ex.Message}");
        }
    }
}