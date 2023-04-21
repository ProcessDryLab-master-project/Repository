using System.Collections.Concurrent;

namespace Repository.App.Database
{
    // TODO: This class may be another way of writing to files from multiple threads in a safe way.
    public class MultiThreadFileWriter
    {
        private static ConcurrentQueue<string> _textToWrite = new ConcurrentQueue<string>();
        private CancellationTokenSource _source = new CancellationTokenSource();
        private CancellationToken _token;
        private static string path;

        public MultiThreadFileWriter()
        {
            _token = _source.Token;
            // This is the task that will run
            // in the background and do the actual file writing
            Task.Run(WriteToFile, _token);
        }

        /// The public method where a thread can ask for a line
        /// to be written.
        public void WriteLine(string line, string path)
        {
            path = path;
            _textToWrite.Enqueue(line);
        }

        /// The actual file writer, running
        /// in the background.
        private async void WriteToFile()
        {
            while (true)
            {
                if (_token.IsCancellationRequested)
                {
                    return;
                }
                using (StreamWriter w = File.AppendText(path))
                {
                    while (_textToWrite.TryDequeue(out string textLine))
                    {
                        await w.WriteLineAsync(textLine);
                    }
                    w.Flush();
                    Thread.Sleep(100);
                }
            }
        }
    }
}
