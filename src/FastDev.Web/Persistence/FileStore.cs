using System;
using System.IO;
using System.Threading.Tasks;

namespace FastDev.Web.Persistence
{
    public class FileStore : IFileStore
    {
        private static readonly int MaxTrials = 5;
        public string StoreLocation { get; }

        private string GetFileLocation(string filename)
        {
            return Path.Combine(StoreLocation, filename);
        }

        public FileStore(string storeLocation)
        {
            StoreLocation = storeLocation;
        }

        public async Task<string> CreateAsync(Stream inputstream)
        {
            // Create store directory if doesn't exist
            Directory.CreateDirectory(StoreLocation);

            var trials = 0;
            while (true)
            {
                var filename = Guid.NewGuid().ToString("N");
                try
                {
                    FileStream fileStream;
                    using (fileStream = File.Open(GetFileLocation(filename), FileMode.CreateNew, FileAccess.Write))
                    {
                        await inputstream.CopyToAsync(fileStream);
                    }
                    return filename;
                }
                catch (IOException)
                {
                    if (++trials >= MaxTrials) throw;
                }
            }
        }

        public Stream Get(string filename, FileMode mode, FileAccess access)
        {
            return File.Open(GetFileLocation(filename), mode, access);
        }

        public void Delete(string filename)
        {
            File.Delete(GetFileLocation(filename));
        }
    }
}
