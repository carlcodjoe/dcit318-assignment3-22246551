// Transaction is modeled as a record rather than a class because a completed
// transaction shouldn't change after the fact — records give us that
// immutability plus built-in value-based equality for free.
record Transaction(int Id, DateTime Date, decimal Amount, string Category);

// ITransactionProcessor defines a contract for anything that can "process"
// a transaction. Different payment rails handle a transaction differently
// (different fees, different confirmation messages), so each implementer
// below reflects that instead of just printing the same line.
interface ITransactionProcessor
{
    void Process(Transaction transaction);
}

class BankTransferProcessor : ITransactionProcessor
{
    public void Process(Transaction transaction)
    {
        Console.WriteLine($"[Bank Transfer] Processed GHS {transaction.Amount:F2} for {transaction.Category}. Funds will reflect in 1-2 business days.");
    }
}

class MobileMoneyProcessor : ITransactionProcessor
{
    public void Process(Transaction transaction)
    {
        Console.WriteLine($"[Mobile Money] Processed GHS {transaction.Amount:F2} for {transaction.Category}. Instant confirmation sent to wallet.");
    }
}

class CryptoWalletProcessor : ITransactionProcessor
{
    public void Process(Transaction transaction)
    {
        Console.WriteLine($"[Crypto Wallet] Processed GHS {transaction.Amount:F2} for {transaction.Category}. Awaiting blockchain confirmation.");
    }
}

// Account is the general-purpose base. Balance has a protected setter
// because only Account itself (or a subclass) should ever be allowed
// to change it directly — external code should only affect balance
// through ApplyTransaction, never by assigning it outright.
class Account
{
    public string AccountNumber { get; }
    public decimal Balance { get; protected set; }

    public Account(string accountNumber, decimal initialBalance)
    {
        AccountNumber = accountNumber;
        Balance = initialBalance;
    }

    // virtual so specialized account types (like SavingsAccount) can
    // enforce their own rules around what happens when a transaction
    // is applied, instead of being locked into this default behavior.
    public virtual void ApplyTransaction(Transaction transaction)
    {
        Balance -= transaction.Amount;
    }
}

// Sealed because a savings account is meant to be the final word on this
// specific behavior — we don't want someone accidentally creating a
// "SuperSavingsAccount" that quietly breaks the insufficient-funds check.
sealed class SavingsAccount : Account
{
    public SavingsAccount(string accountNumber, decimal initialBalance)
        : base(accountNumber, initialBalance)
    {
    }

    public override void ApplyTransaction(Transaction transaction)
    {
        if (transaction.Amount > Balance)
        {
            Console.WriteLine($"Insufficient funds for transaction {transaction.Id} ({transaction.Category}, GHS {transaction.Amount:F2}).");
            return;
        }

        Balance -= transaction.Amount;
        Console.WriteLine($"Transaction {transaction.Id} applied. Updated balance: GHS {Balance:F2}");
    }
}

// FinanceApp wires everything together: it owns the account, runs each
// transaction through its assigned processor, then applies it to the
// account and keeps a running record of everything that happened.
class FinanceApp
{
    private List<Transaction> _transactions = new List<Transaction>();

    public void Run()
    {
        var savings = new SavingsAccount("SA-22246551", 1000m);

        var t1 = new Transaction(1, DateTime.Now, 150.00m, "Groceries");
        var t2 = new Transaction(2, DateTime.Now, 300.00m, "Utilities");
        var t3 = new Transaction(3, DateTime.Now, 2000.00m, "Entertainment"); // deliberately exceeds balance to test the guard

        ITransactionProcessor mobileMoney = new MobileMoneyProcessor();
        ITransactionProcessor bankTransfer = new BankTransferProcessor();
        ITransactionProcessor cryptoWallet = new CryptoWalletProcessor();

        mobileMoney.Process(t1);
        savings.ApplyTransaction(t1);

        bankTransfer.Process(t2);
        savings.ApplyTransaction(t2);

        cryptoWallet.Process(t3);
        savings.ApplyTransaction(t3);

        _transactions.Add(t1);
        _transactions.Add(t2);
        _transactions.Add(t3);
    }
}

class Program
{
    static void Main(string[] args)
    {
        FinanceApp app = new FinanceApp();
        app.Run();
    }
}