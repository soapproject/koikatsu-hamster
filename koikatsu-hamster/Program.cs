/*
    This program organizes images by game type into corresponding folders.
    It scans a directory and its subdirectories for PNG files, while excluding folders that match specific game types defined in the GameType enum.
*/

using koikatsu_hamster;
namespace KoikatsuHamster
{
    class Program
    {
        static IEnumerable<FileInfo> FindPngFiles(string directoryPath)
        {
            var di = new DirectoryInfo(directoryPath);

            // Create a HashSet from the GameType enum to skip game folders
            var gameTypeNames = new HashSet<string>(Enum.GetNames(typeof(GameType)));

            // Find all PNGs in the current directory and subdirectories while skipping game folders
            return di.EnumerateFiles("*.png", SearchOption.AllDirectories)
                .Where(s => s.Directory?.FullName is not null
                            && !gameTypeNames.Any(gameTypeName => s.Directory.FullName.Contains(gameTypeName)));
        }

        static void Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            var pngFiles = FindPngFiles(Directory.GetCurrentDirectory());
            var searchTerm = args.Length > 0 ? args[0] : null;
            foreach (var file in pngFiles)
            {
                try
                {
                    var path = Parser.ParseCard(file, searchTerm);
                    //Console.WriteLine($"Path: {path}");
                    if (path == null)
                    {
                        continue;
                    }
                    Relocator.Move(file, path);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failed to handle {file} with an error: {e.Message}");
                    Console.ResetColor();
                }
            }
            Console.WriteLine("All jobs done, Press any key to exit...");
            Console.ReadKey();
        }
    }
}
