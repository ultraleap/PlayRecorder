using OdinSerializer;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        public static string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
#endif

    }

}