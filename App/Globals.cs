namespace Repository.App
{
    public class Globals
    {
        public static object FileAccessLock = new object(); // Locks writes to files. Source: https://stackoverflow.com/questions/33627449/replace-line-in-file-during-multiple-threads
    }
}
