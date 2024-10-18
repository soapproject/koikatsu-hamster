using MessagePack;

namespace koikatsu_hamster
{
    internal enum GameType
    {
        Unknown,
        Koikatu,
        KoikatsuSunshine,
        HoneyCome,
        SVC,
        EmotionCreators,
        AiSyoujyo,
        RoomGirl
    }

    internal enum CardType
    {
        Unknown,
        Character,
        Coordinate,
        Studio
    }

    [MessagePackObject(true)]
    public class CharaParameter
    {
        // 0: Male, 1: Female
        public byte sex { get; set; } = 0;
        public string firstname { get; set; } = "";
        public string lastname { get; set; } = "";
        [IgnoreMember]
        public string fullname => lastname + " " + firstname;
    }

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

    internal class Parser
    {
        private static long SearchForPngEnd(Stream stream)
        {
            byte[] IENDChunk = { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
            const int bufferSize = 4096;
            var origPos = stream.Position;

            var buffer = new byte[bufferSize];
            int read;

            var endChunkFirstByte = IENDChunk[0];

            while ((read = stream.Read(buffer, 0, bufferSize)) > 0)
            {
                for (var bufferIndex = 0; bufferIndex < read; bufferIndex++)
                {
                    // Check the first byte of the IEND chunk
                    if (buffer[bufferIndex] != endChunkFirstByte)
                        continue;

                    var flag = true;

                    // Check the other bytes of the IEND chunk
                    for (var chunkIndex = 1; chunkIndex < IENDChunk.Length; chunkIndex++)
                    {
                        bufferIndex++;

                        // Handle overlap (search between two buffers)
                        if (bufferIndex >= bufferSize)
                        {
                            if ((read = stream.Read(buffer, 0, bufferSize)) < bufferSize)
                                return -1;

                            bufferIndex = 0;
                        }

                        // Break if bytes do not match
                        if (buffer[bufferIndex] != IENDChunk[chunkIndex])
                        {
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                    {
                        // Move back by the last buffer size, then add the next position of bufferIndex to calculate the PNG end position
                        var result = stream.Position - bufferSize + bufferIndex + 1;
                        stream.Position = origPos;
                        return result;
                    }
                }
            }

            return -1;
        }

        private static (GameType, CardType) ParseMarker(BinaryReader reader)
        {
            var marker = reader.ReadString();
            //Console.WriteLine($"Marker: {marker}");
            return marker switch
            {
                "【KoiKatuChara】" => (GameType.Koikatu, CardType.Character),
                "【KoiKatuCharaS】" => (GameType.Koikatu, CardType.Character),
                "【KoiKatuCharaSP】" => (GameType.Koikatu, CardType.Character),
                "【KoiKatuClothes】" => (GameType.Koikatu, CardType.Coordinate),
                "【KoiKatuCharaSun】" => (GameType.KoikatsuSunshine, CardType.Character),
                "【HCChara】" => (GameType.HoneyCome, CardType.Character),
                "【HCPChara】" => (GameType.HoneyCome, CardType.Character),
                "【SVChara】" => (GameType.SVC, CardType.Character),
                "【SVClothes】" => (GameType.SVC, CardType.Coordinate),
                //"【EroMakeChara】" => (GameType.EmotionCreators, CardType.Character),
                //"【AIS_Chara】" => (GameType.AiSyoujyo, CardType.Character),
                //"【AIS_Clothes】" => (GameType.AiSyoujyo, CardType.Coordinate),
                //"【RG_Chara】" => (GameType.RoomGirl, CardType.Character),
                _ => (GameType.Unknown, CardType.Unknown),
            };
        }

        private static CharaParameter ParseCharaParameter(BinaryReader reader)
        {
            // Read loadVersion (We don't need it, just to move the reader forward)
            _ = reader.ReadString();
            var faceLength = reader.ReadInt32();
            if (faceLength > 0)
            {
                reader.BaseStream.Seek(faceLength, SeekOrigin.Current);
            }
            var count = reader.ReadInt32();
            var bytes = reader.ReadBytes(count);
            var blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(bytes);
            // Read num2 (We don't need it, just to move the reader forward)
            _ = reader.ReadInt64();
            var position = reader.BaseStream.Position;
            var info = blockHeader.SearchInfo("Parameter") ?? throw new InvalidOperationException("Info object not found."); ;
            reader.BaseStream.Seek(position + info.pos, SeekOrigin.Begin);
            byte[] parameterBytes = reader.ReadBytes((int)info.size);

            CharaParameter parameter = MessagePackSerializer.Deserialize<CharaParameter>(parameterBytes);
            return parameter;
        }

        public static string? ParseCard(FileInfo file, string? searchTerm)
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read);
            long pngEndPosition = SearchForPngEnd(stream);
            if (pngEndPosition == -1)
            {
                throw new Exception("PNG end chunk not found");
            }
            // Console.WriteLine($"pngEndPosition: {pngEndPosition}");
            stream.Position = pngEndPosition;

            using var reader = new BinaryReader(stream);

            // Read ProductNo (We don't need it, just to move the reader forward)
            _ = reader.ReadInt32();

            // Read Marker
            var (gameType, cardType) = ParseMarker(reader);
            //Console.WriteLine($"GameType: {gameType}, CardType: {cardType}");
            if (gameType == GameType.Unknown)
            {
                //Console.WriteLine($"GameType unknown for file: {file.FullName}");
                return null;
            }

            // This is not a CharCard, so we don't need to parse other info
            if (cardType != CardType.Character)
            {
                return Path.Combine(gameType.ToString(), cardType.ToString());
            }

            var parameter = ParseCharaParameter(reader);
            //Console.WriteLine($"CharaParameter - Gender: {parameter.sex}, FullName: {parameter.fullname}");

            var gender = parameter.sex switch
            {
                0 => "Male",
                1 => "Female",
                _ => "Unknown",
            };

            // If searchTerm is provided, perform a fuzzy match with fullname
            if (!string.IsNullOrEmpty(searchTerm) && parameter.fullname.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Path.Combine(gameType.ToString(), gender, searchTerm);
            }

            return Path.Combine(gameType.ToString(), gender);
        }
    }
}
