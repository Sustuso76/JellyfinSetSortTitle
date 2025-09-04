using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    static SqliteConnection connection = null;
    static List<string> allFiles = new List<string>();
    static List<string> errors = new List<string>();
    static string numberType = null;
    static string jellifinSortNumberType = "D10";
    public static void Main(string[] args)
    {
        //args = new string[] { "-s", "library.db", "/media/NAS1/NAS1/Libri/Comics/Julia/Julia/", "Julia ", "001" };
        //args = new string[] { "-s", "library.db", @"C:\Develop\JellyfinSetSortTitle\JellyfinSetSortTitle\bin\Debug\net9.0", "Julia ", "006" };

        //Show arguments
        Console.WriteLine("Arguments:");
        foreach (var arg in args)
        {
            Console.WriteLine(arg);
        }

        //Check arguments
        if (!CheckArguments(args))
            return;

        //Check number type
        numberType = CheckNumberType(args[4]);
        Console.WriteLine($"Number type: {numberType}");

        //Check file and folder existence
        CheckFileAndFolderExistence(args[1], args[2]);

        //Connect to database
        ConnectToDatabase(args[1]);

        //Find all files in directory
        allFiles = FindAllFileInDirectory(args[2]);
        //allFiles = new List<string>() { @"/media/NAS1/NAS1/Libri/Comics/Julia/Julia/Julia 006 - Jerry è Sparito (Sergio Bonelli Editore, 1999-03).cbz" };

        //Set sort title
        try
        {
            SetSortTitle(args[3], int.Parse(args[4]));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error setting sort title");
            Console.WriteLine($"{ex.Message}");
        }

        //DisconnectFromDatabase
        DisconnectFromDatabase();

        //Show errors
        if (errors.Count > 0)
        {
            Console.WriteLine("Errors:");
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }
        }
        else
        {
            Console.WriteLine("No errors found");
        }

    }

    /// <summary>
    /// CheckArguments
    /// </summary>
    /// <param name="args"></param>
    public static bool CheckArguments(string[] args)
    {
        Console.WriteLine("Check arguments");

        if (args.Length == 1 && args[0] == "-h")
        {
            ShowHelp();
            return false;
        }

        if (!IsInRange(args.Length, 4, 5))
        {
            Console.WriteLine("Please provide command and all parameters");
            return false;
        }
        Console.WriteLine("Check arguments : OK");
        return true;
    }

    /// <summary>
    /// CheckNumberType
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string CheckNumberType(string number)
    {
        Console.WriteLine($"Check Number Type : {number}");

        if (Regex.IsMatch(number, @"^\d{1}$"))
            return "D1";
        else if (Regex.IsMatch(number, @"^\d{2}$"))
            return "D2";
        else if (Regex.IsMatch(number, @"^\d{3}$"))
            return "D3";
        else if (Regex.IsMatch(number, @"^\d{4}$"))
            return "D4";
        else if (Regex.IsMatch(number, @"^\d{5}$"))
            return "D5";
        else Console.WriteLine($"Wrong start number");
        throw new Exception("Wrong start number");
    }

    /// <summary>
    /// ShowHelp
    /// </summary>
    public static void ShowHelp()
    {
        Console.WriteLine("Usage: JellyfinSetSortTitle command <Jellyfin database path> <Comics path> <Comics prefix ForcedSortName> <Start number>");
        Console.WriteLine("Command : ");
        Console.WriteLine(" -h : Show this help ");
        Console.WriteLine(" -s : Set sort title in Jellyfin database");
        Console.WriteLine();
        Console.WriteLine("Example: JellyfinSetSortTitle -s /var/lib/jellyfin/data/library.db /media/comic/Spiderman Spiderman 001");
    }

    /// <summary>
    /// CheckFileAndFolderExistence
    /// </summary>
    /// <param name="dbPath"></param>
    /// <param name="comicsPath"></param>
    public static void CheckFileAndFolderExistence(string dbPath, string comicsPath)
    {
        Console.WriteLine("CheckFileAndFolderExistence");
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"Database file not found: {dbPath}");
            return;
        }
        if (!Directory.Exists(comicsPath))
        {
            Console.WriteLine($"Comics folder not found: {comicsPath}");
            return;
        }
    }

    /// <summary>
    /// ConnectToDatabase
    /// </summary>
    /// <param name="dbPath"></param>
    public static void ConnectToDatabase(string dbPath)
    {
        Console.WriteLine("ConnectToDatabase");
        connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
    }

    /// <summary>
    /// DisconnectFromDatabase
    /// </summary>
    public static void DisconnectFromDatabase()
    {
        Console.WriteLine("DisconnectFromDatabase");
        if (connection != null)
        {
            connection.Close();
            connection = null;
        }
    }

    /// <summary>
    /// FindAllFileInDirectory
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <returns></returns>
    public static List<string> FindAllFileInDirectory(string directoryPath)
    {
        Console.WriteLine("FindAllFileInDirectory");
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".cbz") || s.EndsWith(".cbr")).OrderBy(s => s).ToList();
        Console.WriteLine($"Number of files {files.Count}");
        return files;
    }

    public static void SetSortTitle(string baseForcedSortName, int startNumber)
    {
        Console.Write("Set Sort Title ");
        using (var progress = new ProgressBar())
        {
            var currentNumber = startNumber;

            //string checkQuery = $"SELECT * FROM TypedBaseItems WHERE Path = @path";
            //var checkCommand = new SqliteCommand(checkQuery, connection);
            //var updateQuery = $"UPDATE TypedBaseItems SET ForcedSortName = @forced_sort_name, SortName = @sort_name WHERE Path = @path";
            //var updateCommand = new SqliteCommand(updateQuery, connection);
            var maxNumber = 5000;
            var oldCurrentNumber = -1;
            foreach (var file in allFiles)
            {
                //Debug
                //Console.WriteLine($"Processing file: {file} with number {currentNumber}");

                //File must contain the baseForcedSortName and start number
                var match = false;
                oldCurrentNumber = currentNumber;
                while (!match && currentNumber < maxNumber)
                {

                    if (!file.Contains(baseForcedSortName.TrimStart().TrimEnd()) || !file.Contains(currentNumber.ToString(numberType)))
                    {
                        currentNumber++;
                    }
                    else
                    {
                        match = true;
                    }
                }

                if (match)
                {

                    //Check if file exists in database
                    var checkQuery = $"SELECT * FROM TypedBaseItems WHERE Path = @path";
                    var checkCommand = new SqliteCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@path", file);

                    var readerCheck = checkCommand.ExecuteReader();
                    if (!readerCheck.HasRows)
                    {
                        errors.Add($"File {file} does not exsist in database");
                        continue;
                    }

                    //Check if file exists multiple times in database
                    var count = 0;
                    while (readerCheck.Read())
                    {
                        count++;
                    }
                    if (count > 1)
                    {
                        errors.Add($"File {file} exists multiple times in database");
                        continue;
                    }
                    var updateQuery = "UPDATE TypedBaseItems SET ForcedSortName = @forced_sort_name, SortName = @sort_name WHERE Path = @path";
                    var updateCommand = new SqliteCommand(updateQuery, connection);
                    //Update sort title
                    updateCommand.Parameters.AddWithValue("@sort_name", baseForcedSortName + " " + currentNumber.ToString(jellifinSortNumberType));
                    updateCommand.Parameters.AddWithValue("@forced_sort_name", baseForcedSortName + " " + currentNumber.ToString(numberType));
                    updateCommand.Parameters.AddWithValue("@path", file);

                    var rowInserted = updateCommand.ExecuteNonQuery();

                    //Debug
                    //Console.WriteLine($"Processing file: {file} with number {currentNumber} DONE!");

                    currentNumber++;
                    progress.Report((double)currentNumber / 100);
                    readerCheck.Close();
                }
                else
                {
                    errors.Add($"File {file} does not contain the baseForcedSortName {baseForcedSortName}");
                    currentNumber = oldCurrentNumber;
                    progress.Report((double)currentNumber / 100);
                }
            }
        }
    }

    #region General
    public static bool IsInRange(int value, int min, int max) => (uint)(value - min) <= (uint)(max - min);
    #endregion

    #region Progress Bar
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int blockCount = 10;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar()
        {
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                    percent,
                    animation[animationIndex++ % animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }

    }
    #endregion
}