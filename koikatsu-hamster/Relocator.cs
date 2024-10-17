namespace koikatsu_hamster
{
    internal class Relocator
    {
        private static string GetNewPath(string path, int count)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("The directory name could not be retrieved.");
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            return Path.Combine(directory, $"{fileNameWithoutExtension}({count}){extension}");
        }

        private static string RenameIfFileExist(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            var count = 1;
            string newPath = GetNewPath(path, count);
            while (File.Exists(newPath))
            {
                count++;
                newPath = GetNewPath(path, count);
            }
            return newPath;
        }

        public static void Move(FileInfo file, string folderPath)
        {
            var fullFolderPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath);
            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
            }
            var fullPath = Path.Combine(fullFolderPath, file.Name);
            var safePath = RenameIfFileExist(fullPath);
            file.MoveTo(safePath);
            Console.WriteLine($"Move file: {file.Name} to {safePath}");
        }
    }
}
