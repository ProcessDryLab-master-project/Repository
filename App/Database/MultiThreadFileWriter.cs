using System.Collections.Concurrent;
using System.Text;

namespace Repository.App.Database
{
    // TODO: This class may be another way of writing to files from multiple threads in a safe way.
    public static class MultiThreadFileWriter
    {
        public static ReaderWriterLock metadataLock = new ReaderWriterLock();
        public static ReaderWriterLock fileLock = new ReaderWriterLock();
        public static readonly int milliSecTimeout = 60000; // Timeout after 60 sec
        public static void Write(this string text, string path)
        {
            try
            {
                metadataLock.AcquireWriterLock(milliSecTimeout); // You might wanna change timeout value. Set to int.MaxValue or some val
                Console.WriteLine("Metadata lock set");
                using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.SetLength(0); // truncate the file
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    fileStream.Write(info, 0, info.Length);
                }
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
        public static void Write(this IFormFile file, string path)
        {
            try
            {
                fileLock.AcquireWriterLock(milliSecTimeout); // You might wanna change timeout value. Set to int.MaxValue or some val
                Console.WriteLine("File lock set");
                using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.SetLength(0); // truncate the file
                    file.CopyTo(fileStream); // write to the file
                }
                //File.WriteAllText(path, text);
            }
            catch (Exception e)
            {
                Console.WriteLine("Write error: " + e.ToString());
            }
            finally
            {
                fileLock.ReleaseWriterLock();
                Console.WriteLine("File lock released");
            }
        }
    }
}
