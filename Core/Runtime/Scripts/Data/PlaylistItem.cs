namespace PlayRecorder
{
    [System.Serializable]
    public class PlaylistItem
    {
        public string name;
        public string path;
        public string guid;

        public PlaylistItem(string name, string path, string guid)
        {
            this.name = name;
            this.path = path;
            this.guid = guid;
        }
    }
}