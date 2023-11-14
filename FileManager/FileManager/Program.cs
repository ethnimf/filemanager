using System;
using System.IO;
using System.Linq;
namespace FileManager;
public static class FileManager
{
    private static DriveInfo[] drives;
    private static string currentPath;

    static FileManager()
    {
        drives = DriveInfo.GetDrives();
        currentPath = null;
    }

    public static void ShowDrives()
    {
        Console.WriteLine("Доступный диск:");
        foreach (var drive in drives)
        {
            Console.WriteLine($"{drive.Name} - {drive.DriveFormat} - {drive.TotalFreeSpace / (1024 * 1024 * 1024)} GB free out of {drive.TotalSize / (1024 * 1024 * 1024)} GB");
        }
    }

    public static void ExploreDrive(string driveLetter)
    {
        currentPath = driveLetter + ":\\";
        ExploreFolder(currentPath);
    }
    public static string GetCurrentPath()
    {
        return currentPath;
    }
    public static void ExploreFolder(string path)
    {
        currentPath = path;

        Console.WriteLine($"Текущий путь: {currentPath}");

        try
        {
            bool exit = false;

            do
            {
                var directories = Directory.GetDirectories(currentPath);
                var files = Directory.GetFiles(currentPath);

                Console.WriteLine("Папки:");
                for (int i = 0; i < directories.Length; i++)
                {
                    try
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(directories[i]);
                        Console.WriteLine($"[{i + 1}] {Path.GetFileName(directories[i])} - Created: {dirInfo.CreationTime}, Size: {GetFolderSize(directories[i]) / 1024} KB");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"[{i + 1}] Access denied for {Path.GetFileName(directories[i])}");
                    }
                }

                Console.WriteLine("Файлы:");
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(files[i]);
                        Console.WriteLine($"[{i + 1 + directories.Length}] {Path.GetFileName(files[i])} - Created: {fileInfo.CreationTime}, Size: {fileInfo.Length / 1024} KB");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"[{i + 1 + directories.Length}] Access denied for {Path.GetFileName(files[i])}");
                    }
                }

                Console.WriteLine("\nВведите номер папки или файла, который нужно открыть (0 для возврата назад, Esc для выбора):");

                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    // Go up one level
                    if (currentPath.Length > 3)
                    {
                        currentPath = Directory.GetParent(currentPath).FullName;
                    }
                    else
                    {
                        ShowDrives();
                        Console.Write("Выберете диск: ");
                        string driveLetter = Console.ReadLine()?.ToUpper();

                        if (!string.IsNullOrEmpty(driveLetter) && driveLetter.Length == 1 && char.IsLetter(driveLetter[0]))
                        {
                            currentPath = driveLetter + ":\\";
                        }
                        else
                        {
                            Console.WriteLine("Неверная буква диска.");
                            exit = true;
                        }
                    }
                }
                else
                {
                    string input = key.KeyChar.ToString() + Console.ReadKey().KeyChar.ToString();

                    if (int.TryParse(input, out int choice) && choice >= 0 && choice <= directories.Length + files.Length)
                    {
                        if (choice == 0)
                        {
                            // Go back
                            if (currentPath.Length > 3)
                            {
                                currentPath = Directory.GetParent(currentPath).FullName;
                            }
                            else
                            {
                                ShowDrives();
                                Console.Write("Выберете диск: ");
                                string driveLetter = Console.ReadLine()?.ToUpper();

                                if (!string.IsNullOrEmpty(driveLetter) && driveLetter.Length == 1 && char.IsLetter(driveLetter[0]))
                                {
                                    currentPath = driveLetter + ":\\";
                                }
                                else
                                {
                                    Console.WriteLine("Неверная буква диска.");
                                    exit = true;
                                }
                            }
                        }
                        else
                        {
                            if (choice <= directories.Length)
                            {
                                string selectedPath = directories[choice - 1];
                                currentPath = selectedPath;
                            }
                            else
                            {
                                int selectedFileIndex = choice - 1 - directories.Length;
                                string selectedFilePath = files[selectedFileIndex];

                                Console.WriteLine($"Вы хотите открыть файл? (Y/N)");
                                ConsoleKeyInfo response = Console.ReadKey();

                                if (response.Key == ConsoleKey.Y)
                                {
                                    FileManager.LaunchFile(selectedFilePath);
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Неверный выбор.");
                    }
                }
            } while (!exit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при просмотре папок: {ex.Message}");
        }
    }


    private static long GetFolderSize(string path)
    {
        return CalculateFolderSize(path).Item1;
    }

    private static (long size, DateTime creationTime) CalculateFolderSize(string folderPath)
    {
        long size = 0;
        DateTime creationTime = DateTime.MinValue;

        string[] files = Directory.GetFiles(folderPath);
        string[] subdirectories = Directory.GetDirectories(folderPath);

        foreach (string file in files)
        {
            FileInfo fileInfo = new FileInfo(file);
            size += fileInfo.Length;

            if (fileInfo.CreationTime > creationTime)
            {
                creationTime = fileInfo.CreationTime;
            }
        }

        foreach (string subdirectory in subdirectories)
        {
            var subdirectoryInfo = CalculateFolderSize(subdirectory);

            size += subdirectoryInfo.size;

            if (subdirectoryInfo.creationTime > creationTime)
            {
                creationTime = subdirectoryInfo.creationTime;
            }
        }

        return (size, creationTime);
    }


    public static void LaunchFile(string filePath)
    {
        try
        {
            Console.WriteLine($"Запуск {filePath}");

            if (File.Exists(filePath))
            {
                if (IsValidFilePath(filePath))
                {
                    string extension = Path.GetExtension(filePath);

                    string associatedProgram = GetAssociatedProgram(extension);

                    if (string.IsNullOrEmpty(associatedProgram) && extension.ToLower() == ".txt"
                        || extension.ToLower() == ".xml"
                        || extension.ToLower() == ".json") 
                    {
                        associatedProgram = "notepad.exe";
                    }

                    if (string.IsNullOrEmpty(associatedProgram) && extension.ToLower() == ".docx")
                    {
                        associatedProgram = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE"; 
                    }

                    if (!string.IsNullOrEmpty(associatedProgram))
                    {
                        System.Diagnostics.Process.Start(associatedProgram, filePath);
                    }
                    else
                    {
                        Console.WriteLine($"No associated program found for {extension} files. Using default system application.");
                        System.Diagnostics.Process.Start(filePath);
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid file path: {filePath}");
                }
            }
            else
            {
                Console.WriteLine($"File {filePath} does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching file: {ex.Message}");
        }
    }



    private static bool IsValidFilePath(string filePath)
    {
        try
        {
            Path.GetFullPath(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }



    private static string GetAssociatedProgram(string fileExtension)
    {
        string keyName = $"HKEY_CLASSES_ROOT\\{fileExtension}";

        object fileType = Microsoft.Win32.Registry.GetValue(keyName, "", null);

        if (fileType != null)
        {
            object associatedProgram = Microsoft.Win32.Registry.GetValue($"HKEY_CLASSES_ROOT\\{fileType}\\shell\\open\\command", "", null);

            if (associatedProgram != null)
            {
                return associatedProgram.ToString();
            }
        }

        return null;
    }

}

public static class ArrowManager
{
    public static int ShowMenu(string[] options)
    {
        int selectedIndex = 0;

        ConsoleKeyInfo key;
        do
        {
            Console.Clear();
            Console.WriteLine("Используйте клавиши со стрелками для навигации и Enter для выбора:");

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                    Console.Write("-> ");

                Console.WriteLine(options[i]);
            }

            key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                    break;

                case ConsoleKey.DownArrow:
                    selectedIndex = Math.Min(options.Length - 1, selectedIndex + 1);
                    break;
            }
        } while (key.Key != ConsoleKey.Enter);

        return selectedIndex;
    }
    public static int ShowDriveMenu(DriveInfo[] drives)
    {
        int selectedIndex = 0;

        ConsoleKeyInfo key;
        do
        {
            Console.Clear();
            Console.WriteLine("Используйте клавиши со стрелками для навигации и Enter для выбора:");

            for (int i = 0; i < drives.Length; i++)
            {
                if (i == selectedIndex)
                    Console.Write("-> "); 

                Console.WriteLine($"{drives[i].Name} - {drives[i].DriveFormat} - {drives[i].TotalFreeSpace / (1024 * 1024 * 1024)} GB free out of {drives[i].TotalSize / (1024 * 1024 * 1024)} GB");
            }

            key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                    break;

                case ConsoleKey.DownArrow:
                    selectedIndex = Math.Min(drives.Length - 1, selectedIndex + 1);
                    break;
            }
        } while (key.Key != ConsoleKey.Enter);

        return selectedIndex;
    }
}
class Program
{
    static void Main()
    {
        FileManager.ShowDrives();

        string[] menuOptions = { "Explore", "Launch", "Exit" };

        int selectedOption;
        do
        {
            selectedOption = ArrowManager.ShowMenu(menuOptions);

            switch (selectedOption)
            {
                case 0:
                    ExploreOption();
                    break;

                case 1:
                    LaunchOption();
                    break;
            }

        } while (selectedOption != menuOptions.Length - 1);
    }

    static void ExploreOption()
    {
        Console.Clear();
        FileManager.ShowDrives();

        DriveInfo[] drives = DriveInfo.GetDrives();
        int selectedDriveIndex = ArrowManager.ShowDriveMenu(drives);

        Console.Clear(); 
        FileManager.ExploreDrive(drives[selectedDriveIndex].Name.Substring(0, 1));

        Console.ReadKey(); 
    }

    static void LaunchOption()
    {
        Console.Clear();

        Console.Write("Введите полный путь к файлу для запуска: ");
        string filePath = Console.ReadLine();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            FileManager.LaunchFile(filePath);
            Console.ReadKey(); 
        }
        else
        {
            Console.WriteLine("Неверный путь.");
            Console.ReadKey();
        }
    }
}
