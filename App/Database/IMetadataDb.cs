using System.Collections.Concurrent;
using Repository.App.Entities;

namespace Repository.App.Database
{
    public interface IMetadataDb
    {
        /// <summary>
        /// If metadataObject already exists, it updates the file, otherwise it adds a new metadata file
        /// </summary>
        /// <param name="metadataObject"></param>
        void MetadataWrite(MetadataObject metadataObject);
        void UpdateDynamicResourceTime(string resourceId);
        Dictionary<string, MetadataObject> GetMetadataDict();
        MetadataObject? GetMetadataObjectById(string key);
        bool ContainsKey(string key);

        //void WriteFile(string path, IFormFile file);
        //StreamContent ReadFile(string path);
        //ConcurrentDictionary<string, object> activeFiles { get; set; }
    }
}