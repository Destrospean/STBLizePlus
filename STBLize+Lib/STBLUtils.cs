using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public static class STBLUtils
    {
        public const int FourCC = 0x4C425453;

        static IDictionary DecrapifyKeys(this IDictionary entries, string arbitraryMaleSuffix)
        {
            var decrapifiedKeyEntries = new OrderedDictionary();
            foreach (DictionaryEntry entry in entries)
            {   
                decrapifiedKeyEntries[(entry.Key.ToString().EndsWith(arbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(arbitraryMaleSuffix)) : entry.Key.ToString())] = (entry.Value as IDictionary)?.DecrapifyKeys(arbitraryMaleSuffix) ?? entry.Value;
            }
            return decrapifiedKeyEntries;
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

        public static IDictionary ReadStbl(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return ReadStbl(stream);
            }
        }

        public static IDictionary ReadStbl(Stream stream)
        {
            try
            {
                var entries = new OrderedDictionary();
                using (var reader = new BinaryReader(stream, System.Text.Encoding.Unicode))
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
                return entries;
            }
            catch
            {
                throw new System.ArgumentException("File must be a valid STBL");
            }
        }

        public static IDictionary Unflatten(this IDictionary entries)
        {
            string arbitraryMaleSuffix = "{DESTROSPEAN_STBL_MALE_SUFFIX_" + System.Guid.NewGuid() + "}",
            arbitrarySeparator = "{DESTROSPEAN_STBL_SEPARATOR_" + System.Guid.NewGuid() + "}";
            var suffixes = new[]
                {
                    "_Female",
                    "_FemaleFemale",
                    "_MaleFemale"
                };
            IDictionary intermediateEntries = new OrderedDictionary(),
            readyToUnflattenEntries = new OrderedDictionary();
            var keysToReplace = new System.Collections.Generic.List<string>();
            foreach (DictionaryEntry entry in entries)
            {
                var key = entry.Key.ToString().Replace("/", arbitrarySeparator + "/").Replace(":", arbitrarySeparator + ":");
                foreach (var suffix in suffixes)
                {
                    if (key.ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        keysToReplace.Add(key.Substring(0, key.ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())));
                        break;
                    }
                }
                intermediateEntries[key] = entry.Value;
            }
            foreach (DictionaryEntry entry in intermediateEntries)
            {
                if (keysToReplace.Contains(entry.Key.ToString()))
                {
                    readyToUnflattenEntries[entry.Key + arbitrarySeparator + arbitraryMaleSuffix] = entry.Value;
                    continue;
                }
                var hasNoSuffix = true;
                foreach (var suffix in suffixes)
                {
                    if (entry.Key.ToString().ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        readyToUnflattenEntries[entry.Key.ToString().Substring(0, entry.Key.ToString().ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())) + arbitrarySeparator + suffix] = entry.Value;
                        hasNoSuffix = false;
                        break;
                    }
                }
                if (hasNoSuffix)
                {
                    readyToUnflattenEntries[entry.Key] = entry.Value;
                }
            }
            return readyToUnflattenEntries.Unflatten(arbitrarySeparator).DecrapifyKeys(arbitraryMaleSuffix);
        }

        public static IDictionary UnhashKeys(string stblPath, string unhashedStblPath)
        {
            using (var stblStream = File.OpenRead(stblPath))
            {
                using (var unhashedStblStream = File.OpenRead(unhashedStblPath))
                {
                    return UnhashKeys(stblStream, unhashedStblStream);
                }
            }
        }

        public static IDictionary UnhashKeys(Stream stblStream, Stream unhashedStblStream)
        {
            IDictionary entriesWithHashedKeys = ReadStbl(stblStream),
            entriesWithUnhashedKeys = new OrderedDictionary();
            foreach (DictionaryEntry entry in ReadStbl(unhashedStblStream))
            {
                entriesWithUnhashedKeys[entry.Value] = entriesWithHashedKeys[entry.Key];
            }
            return entriesWithUnhashedKeys;
        }

        public static void WriteStbl(string path, IDictionary entries, bool keysAsValues = false)
        {
            using (var stream = File.Create(path))
            {
                WriteStbl(stream, entries, keysAsValues);
            }
        }

        public static void WriteStbl(Stream stream, IDictionary entries, bool keysAsValues = false)
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.Unicode))
            {
                writer.Write(FourCC);
                writer.Write((byte)2);
                writer.Write(new byte[2]);
                writer.Write(entries.Count);
                writer.Write(new byte[6]);
                foreach (DictionaryEntry entry in entries)
                {
                    writer.Write(GetFnv64(entry.Key.ToString()));
                    var value = (keysAsValues ? entry.Key : entry.Value).ToString();
                    writer.Write(value.Length);
                    writer.Write(value.ToCharArray());
                }
            }
        }
    }
}
