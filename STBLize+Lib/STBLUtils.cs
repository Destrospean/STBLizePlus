using System.IO;

namespace Destrospean.STBLizePlus
{
    public static class STBLUtils
    {
        static void WriteEntryPair(BinaryWriter writer, string key, string value)
        {
            writer.Write(GetFnv64(key));
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        public static ulong GetFnv64(string value)
        {
            var hash = 0xCBF29CE484222325;
            foreach (var b in System.Text.Encoding.UTF8.GetBytes(value.ToLowerInvariant()))
            {
                hash *= 0x100000001B3;
                hash &= 0xFFFFFFFFFFFFFFFF;
                hash ^= b;
            }
            return hash;
        }

        public static System.Collections.IDictionary ReadStbl(string path)
        {
            try
            {
                var entries = new System.Collections.Specialized.OrderedDictionary();
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(input, System.Text.Encoding.Unicode))
                    {
                        reader.ReadBytes(7);
                        var count = reader.ReadInt32();
                        reader.ReadBytes(6);
                        for (var i = 0; i < count; i++)
                        {
                            var id = reader.ReadUInt64();
                            entries[id] = new string(reader.ReadChars(reader.ReadInt32()));
                        }
                    }
                }
                return entries;
            }
            catch
            {
                throw new System.ArgumentException("File must be a valid STBL");
            }
        }

        public static void WriteStbl(string path, System.Collections.IDictionary entries, bool keysAsValues = false)
        {
            using (var output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(output, System.Text.Encoding.Unicode))
                {
                    writer.Write(new byte[]
                        {
                            83,
                            84,
                            66,
                            76
                        });
                    writer.Write(new byte[]
                        {
                            2
                        });
                    writer.Write(new byte[2]);
                    writer.Write(entries.Count);
                    writer.Write(new byte[6]);
                    foreach (System.Collections.DictionaryEntry entry in entries)
                    {
                        WriteEntryPair(writer, entry.Key.ToString(), keysAsValues ? entry.Key.ToString() : entry.Value.ToString());
                    }
                }
            }
        }
    }
}
