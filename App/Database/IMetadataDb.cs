using System.Collections.Concurrent;
using Repository.App.Entities;

namespace Repository.App.Database
{
    public interface IMetadataDb
    {
        void MetadataWrite(MetadataObject metadataObject);
        void UpdateDynamicResourceTime(string resourceId);
        Dictionary<string, MetadataObject> GetMetadataDict();
        MetadataObject? GetMetadataObjectById(string key);
        bool ContainsKey(string key);
    }
}