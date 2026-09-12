// Student holds the parsed, valid data for one record. GetGrade() is kept
// on the model itself rather than as a free-floating function, since
// "what grade does this score earn" is fundamentally a property of the
// student's own data.
class Student
{
    public int Id { get; }
    public string FullName { get; }
    public int Score { get; }

    public Student(int id, string fullName, int score)
    {
        Id = id;
        FullName = fullName;
        Score = score;
    }

    public string GetGrade()
    {
        if (Score >= 80 && Score <= 100) return "A";
        if (Score >= 70 && Score <= 79) return "B";
        if (Score >= 60 && Score <= 69) return "C";
        if (Score >= 50 && Score <= 59) return "D";
        return "F";
    }
}

// Thrown specifically when the score field exists but isn't a valid
// integer (e.g. "eighty" instead of "80") — distinct from a missing
// field entirely, since the fix for each is different.
class InvalidScoreFormatException : Exception
{
    public InvalidScoreFormatException(string message) : base(message) { }
}

// Thrown when a line doesn't have the expected number of comma-separated
// values at all (e.g. only ID and name, no score).
class MissingFieldException : Exception
{
    public MissingFieldException(string message) : base(message) { }
}

class StudentResultProcessor
{
    public List<Student> ReadStudentsFromFile(string inputFilePath)
    {
        var students = new List<Student>();

        using (var reader = new StreamReader(inputFilePath))
        {
            string? line;
            int lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; // skip blank lines rather than treating them as bad data
                }

                var fields = line.Split(',');

                if (fields.Length != 3)
                {
                    throw new MissingFieldException($"Line {lineNumber} does not have exactly 3 fields: \"{line}\"");
                }

                int id = int.Parse(fields[0].Trim());
                string fullName = fields[1].Trim();

                if (!int.TryParse(fields[2].Trim(), out int score))
                {
                    throw new InvalidScoreFormatException($"Line {lineNumber} has a non-numeric score: \"{fields[2].Trim()}\"");
                }

                students.Add(new Student(id, fullName, score));
            }
        }

        return students;
    }

    public void WriteReportToFile(List<Student> students, string outputFilePath)
    {
        using (var writer = new StreamWriter(outputFilePath))
        {
            foreach (var student in students)
            {
                writer.WriteLine($"{student.FullName} (ID: {student.Id}): Score = {student.Score}, Grade = {student.GetGrade()}");
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        string inputPath = "students.txt";
        string outputPath = "grading_report.txt";

        var processor = new StudentResultProcessor();

        try
        {
            List<Student> students = processor.ReadStudentsFromFile(inputPath);
            processor.WriteReportToFile(students, outputPath);
            Console.WriteLine($"Report generated successfully: {outputPath}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Input file not found: {ex.Message}");
        }
        catch (InvalidScoreFormatException ex)
        {
            Console.WriteLine($"Invalid score format: {ex.Message}");
        }
        catch (MissingFieldException ex)
        {
            Console.WriteLine($"Missing field: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}