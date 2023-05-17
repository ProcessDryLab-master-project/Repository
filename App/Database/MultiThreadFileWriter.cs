using System.Collections.Concurrent;

namespace Repository.App.Database
{
    // TODO: This class may be another way of writing to files from multiple threads in a safe way.
    public static class MultiThreadFileWriter
    {
        static ReaderWriterLock metadataLock = new ReaderWriterLock();
        static readonly int milliSecTimeout = 60000; // Timeout after 60 sec
        public static void Write(this string text, string path)
        {
            try
            {
                metadataLock.AcquireWriterLock(milliSecTimeout); // You might wanna change timeout value. Set to int.MaxValue or some val
                Console.WriteLine("Metadata lock set");
                File.WriteAllText(path, text);
            }
            catch (Exception e)
            {
                Console.WriteLine("Write error: " + e.ToString());
            }
            finally
            {
                metadataLock.ReleaseWriterLock();
                Console.WriteLine("Metadata lock released");
            }
        }
    }
}
