using OdinSerializer;
using UnityEngine;

namespace PlayRecorder
{

    public static class FileUtil
    {

        public static Data LoadSingleFile(byte[] bytes, string name)
        {
            try
            {
                Data d = SerializationUtility.DeserializeValue<Data>(bytes, DataFormat.Binary);
                if (d.objects != null)
                {
                    return d;
                }
                else
                {
                    Debug.LogError(name + " is an invalid recording file and has been ignored and removed.");
                    return null;
                }
            }
            catch
            {
                Debug.LogError(name + " is an invalid recording file and has been ignored and removed.");
                return null;
            }
        }

        public static void SaveDataToFile(string path, string filename, Data data)
        {
            System.IO.Directory.CreateDirectory(path);
            System.IO.File.WriteAllBytes(path + filename + ".bytes", SerializationUtility.SerializeValue(data, DataFormat.Binary));
        }


    }

}