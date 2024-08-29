using MessagePack;

namespace IllCardFilter
{
    /*

    The MessagePackObject attribute indicates that this class can be serializ/deserializ with MessagePack.
    !! Variable names are crucial for the serialization/deserialization.
    Open your card with Notepad++ and search "lstInfo" to see what I mean.
    */
    [MessagePackObject(true)]
    public class BlockHeader
    {
        public List<Info> lstInfo { get; set; } = new List<Info>();

        public Info? SearchInfo(string name) => lstInfo.Find(x => x.name == name);

        [MessagePackObject(true)]
        public class Info
        {
            public string? name { get; set; }
            public string? version { get; set; }
            public long pos { get; set; }
            public long size { get; set; }
        }
    }

    /*
    Same as BlockHeader.
    We need only sex, but you can find more params in your case.
    */
    [MessagePackObject(true)]
    public class CharaParameter
    {
        // 0: Male, 1: Female
        public byte sex { get; set; } = 0;
    }

    public enum GameType
    {
        Unknown,
        Koikatu,
        KoikatsuParty,
        KoikatsuPartySpecialPatch,
        EmotionCreators,
        AiSyoujyo,
        KoikatsuSunshine,
        RoomGirl,
        HoneyCome,
        HoneyComeccp,
        SVC
    }

    public enum CardType
    {
        Unknown,
        Chara,
        Clothes,
        Studio
    }

    class Program
    {
        public static string RenameIfFileExist(string path)
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

        private static string GetNewPath(string path, int count)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("The directory name could not be retrieved.");
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            return Path.Combine(directory, $"{fileNameWithoutExtension}{count}{extension}");
        }

        private static (GameType, CardType) GetGameAndCardType(string marker) => marker switch
        {
            "【KoiKatuChara】" => (GameType.Koikatu, CardType.Chara),
            "【KoiKatuClothes】" => (GameType.Koikatu, CardType.Clothes),
            "【KStudio】" => (GameType.Koikatu, CardType.Studio),
            "【KoiKatuCharaS】" => (GameType.KoikatsuParty, CardType.Chara),
            "【KoiKatuCharaSP】" => (GameType.KoikatsuPartySpecialPatch, CardType.Chara),
            "【EroMakeChara】" => (GameType.EmotionCreators, CardType.Chara),
            "【AIS_Chara】" => (GameType.AiSyoujyo, CardType.Chara),
            "【KoiKatuCharaSun】" => (GameType.KoikatsuSunshine, CardType.Chara),
            "【RG_Chara】" => (GameType.RoomGirl, CardType.Chara),
            "【HCChara】" => (GameType.HoneyCome, CardType.Chara),
            "【HCPChara】" => (GameType.HoneyComeccp, CardType.Chara),
            "【SVChara】" => (GameType.SVC, CardType.Chara),
            _ => (GameType.Unknown, CardType.Unknown),
        };

        private static bool ProcessCardData(Stream stream, FileInfo file)
        {
            using var reader = new BinaryReader(stream);
            // 4 bytes before game type,
            // it may looks like NUL NUL NUL DC4 if you open png with Nodepad++.
            int loadProductNo = reader.ReadInt32();
            if (loadProductNo > 100)
            {
                // TODO I found some GameType like 【KStudio】 is not in first IDEN, so we need a new method in that case. But let's just skip it so far.
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: loadProductNo > 100");
                Console.ResetColor();
                // return false;
            }

            string marker = reader.ReadString();
            var (gameType, cardType) = GetGameAndCardType(marker);

            // Skip unknow GameType
            if (gameType == GameType.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"GameType Unknown");
                Console.ResetColor();
                return false;
            };

            string gameTypeFolderName = gameType switch
            {
                GameType.KoikatsuParty => GameType.Koikatu.ToString(),
                GameType.KoikatsuPartySpecialPatch => GameType.Koikatu.ToString(),
                GameType.HoneyComeccp => GameType.HoneyCome.ToString(),
                _ => gameType.ToString(),
            };
            var gameTypeFolder = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), gameTypeFolderName));
            var cardTypeFolder = Directory.CreateDirectory(Path.Combine(gameTypeFolder.FullName, cardType.ToString()));
            string sexFolderName = "";
            if (cardType == CardType.Chara)
            {
                // Read data until the read pointer is at position of blockHeader
                var loadVersion = reader.ReadString();
                var faceLength = reader.ReadInt32();
                if (faceLength > 0)
                {
                    reader.BaseStream.Seek(faceLength, SeekOrigin.Current);
                }
                var count = reader.ReadInt32();
                var bytes = reader.ReadBytes(count);
                var blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(bytes);
                var num2 = reader.ReadInt64();
                var position = reader.BaseStream.Position;
                var info = blockHeader.SearchInfo("Parameter") ?? throw new InvalidOperationException("Info object not found."); ;
                reader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
                byte[] parameterBytes = reader.ReadBytes((int)info.size);
                CharaParameter parameter = MessagePackSerializer.Deserialize<CharaParameter>(parameterBytes);
                sexFolderName = parameter.sex switch
                {
                    0 => "Male",
                    1 => "Female",
                    _ => "Unknown"
                };
                var sexFolder = Directory.CreateDirectory(Path.Combine(cardTypeFolder.FullName, sexFolderName));
            }

            var destinationPath = Path.Combine(cardTypeFolder.FullName, sexFolderName, file.Name);
            destinationPath = RenameIfFileExist(destinationPath);

            reader.Close();
            stream.Close();
            file.MoveTo(destinationPath);
            Console.WriteLine($"Moved file={file.Name} to {destinationPath}");

            return true;
        }

        private static bool ParseCard(FileInfo file)
        {
            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Game data should located beyond the PNG IEND chunk,
                // thus move the read pointer to that position.
                long pngEnd = PngChunkFinder.SearchPngEnd(stream);
                if (PngChunkFinder.IsInvalidPngEnd(pngEnd, stream.Length))
                    throw new InvalidDataException("Invalid PNG end position found.");
                stream.Position = pngEnd;

                return ProcessCardData(stream, file);
            }
        }

        static void Main(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
            // Find all pngs in current and sub folder,
            // Skip output folder(Folder name equals to the name of game).
            var gameTypeNames = new HashSet<string>(Enum.GetNames(typeof(GameType)));
            var files = di.EnumerateFiles("*.png", SearchOption.AllDirectories)
                .Where(s => !gameTypeNames.Any(gameTypeName => s.Directory?.FullName.Contains(gameTypeName) ?? false));

            foreach (var file in files)
            {
                try
                {
                    ParseCard(file);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Parse {file} failed with an error: {e.Message}");
                    Console.ResetColor();
                }
            }
            Console.WriteLine("All jobs done, Press any key to exit...");
            Console.ReadKey();
        }
    }
}