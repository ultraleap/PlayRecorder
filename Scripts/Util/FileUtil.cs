using OdinSerializer;
using UnityEngine;
using System.Collections.Generic;

namespace PlayRecorder
{

    public static class FileUtil
    {

        public static Data LoadSingleFile(byte[] bytes)
        {
            try
            {
                Data d = SerializationUtility.DeserializeValue<Data>(bytes, DataFormat.Binary);
                // If the file has no objects recorded then there's nothing to load
                if (d.objects != null && d.objects.Count > 0)
                {
                    return d;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static void SaveDataToFile(string path, string filename, Data data)
        {
            System.IO.Directory.CreateDirectory(path);
            System.IO.File.WriteAllBytes(path + filename + ".bytes", SerializationUtility.SerializeValue(data, DataFormat.Binary));
        }

        // why json for playlists?
        // they're meant to be editable and as they work on name basis, then guid 
        public static List<PlaylistItem> LoadPlaylist(byte[] json)
        {
            return SerializationUtility.DeserializeValue<List<PlaylistItem>>(json, DataFormat.JSON);
        }

        public static void SavePlaylist(string path, List<PlaylistItem> items)
        {
            System.IO.File.WriteAllBytes(path, SerializationUtility.SerializeValue(items, DataFormat.JSON));
        }

    }

}