// A generic repository that can store and retrieve any entity type.
// Instead of writing a separate PatientRepository and PrescriptionRepository
// with near-identical code, we write the storage logic once and let the
// caller decide, via predicates, what "finding" or "removing" actually means
// for their specific type.
class Repository<T>
{
    private List<T> items = new List<T>();

    public void Add(T item)
    {
        items.Add(item);
    }

    public List<T> GetAll()
    {
        return items;
    }

    public T? GetById(Func<T, bool> predicate)
    {
        foreach (var item in items)
        {
            if (predicate(item))
            {
                return item;
            }
        }
        return default;
    }

    public bool Remove(Func<T, bool> predicate)
    {
        var match = items.FirstOrDefault(predicate);
        if (match != null)
        {
            return items.Remove(match);
        }
        return false;
    }
}

class Patient
{
    public int Id { get; }
    public string Name { get; }
    public int Age { get; }
    public string Gender { get; }

    public Patient(int id, string name, int age, string gender)
    {
        Id = id;
        Name = name;
        Age = age;
        Gender = gender;
    }
}

class Prescription
{
    public int Id { get; }
    public int PatientId { get; }
    public string MedicationName { get; }
    public DateTime DateIssued { get; }

    public Prescription(int id, int patientId, string medicationName, DateTime dateIssued)
    {
        Id = id;
        PatientId = patientId;
        MedicationName = medicationName;
        DateIssued = dateIssued;
    }
}

// HealthSystemApp is the orchestration layer: it owns the two repositories
// and the lookup map that lets us jump straight from a patient ID to their
// prescriptions, instead of scanning the whole prescription list every time.
class HealthSystemApp
{
    private Repository<Patient> _patientRepo = new Repository<Patient>();
    private Repository<Prescription> _prescriptionRepo = new Repository<Prescription>();
    private Dictionary<int, List<Prescription>> _prescriptionMap = new Dictionary<int, List<Prescription>>();

    public void SeedData()
    {
        _patientRepo.Add(new Patient(1, "Ama Boateng", 34, "Female"));
        _patientRepo.Add(new Patient(2, "Kwesi Owusu", 45, "Male"));
        _patientRepo.Add(new Patient(3, "Efua Mensah", 28, "Female"));

        _prescriptionRepo.Add(new Prescription(1, 1, "Amoxicillin", DateTime.Now.AddDays(-10)));
        _prescriptionRepo.Add(new Prescription(2, 1, "Paracetamol", DateTime.Now.AddDays(-3)));
        _prescriptionRepo.Add(new Prescription(3, 2, "Ibuprofen", DateTime.Now.AddDays(-7)));
        _prescriptionRepo.Add(new Prescription(4, 3, "Metformin", DateTime.Now.AddDays(-15)));
        _prescriptionRepo.Add(new Prescription(5, 2, "Lisinopril", DateTime.Now.AddDays(-1)));
    }

    public void BuildPrescriptionMap()
    {
        foreach (var prescription in _prescriptionRepo.GetAll())
        {
            if (!_prescriptionMap.ContainsKey(prescription.PatientId))
            {
                _prescriptionMap[prescription.PatientId] = new List<Prescription>();
            }
            _prescriptionMap[prescription.PatientId].Add(prescription);
        }
    }

    public List<Prescription> GetPrescriptionsByPatientId(int patientId)
    {
        if (_prescriptionMap.TryGetValue(patientId, out var prescriptions))
        {
            return prescriptions;
        }
        return new List<Prescription>();
    }

    public void PrintAllPatients()
    {
        Console.WriteLine("All Patients:");
        foreach (var patient in _patientRepo.GetAll())
        {
            Console.WriteLine($"  ID: {patient.Id}, Name: {patient.Name}, Age: {patient.Age}, Gender: {patient.Gender}");
        }
    }

    public void PrintPrescriptionsForPatient(int id)
    {
        var prescriptions = GetPrescriptionsByPatientId(id);
        Console.WriteLine($"Prescriptions for Patient ID {id}:");
        if (prescriptions.Count == 0)
        {
            Console.WriteLine("  No prescriptions found.");
            return;
        }
        foreach (var prescription in prescriptions)
        {
            Console.WriteLine($"  {prescription.MedicationName} (issued {prescription.DateIssued:yyyy-MM-dd})");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var app = new HealthSystemApp();
        app.SeedData();
        app.BuildPrescriptionMap();
        app.PrintAllPatients();

        Console.WriteLine();
        app.PrintPrescriptionsForPatient(2);
    }
}