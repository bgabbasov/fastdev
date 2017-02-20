using System.IO;
using System.Threading.Tasks;

namespace FastDev.Web.Persistence
{
    public interface IFileStore
    {
        Task<string> CreateAsync(Stream inputstream);
        Stream Get(string filename, FileMode mode, FileAccess access);
        void Delete(string filename);
    }
}