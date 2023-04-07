namespace Repository.App.Database
{
    public interface IFileDatabase
    {
        /// <summary>
        /// If metadataObject already exists, it updates the file, otherwise it adds a new metadata file
        /// </summary>
        /// <param name="metadataObject"></param>
        void UpdateMetadata(MetadataObject metadataObject);
        Dictionary<string, MetadataObject> GetMetadataDict();
        MetadataObject? GetMetadataObjectById(string key);
        //IResult UpdateMetadataObject(IDictionary<string, string> keyValuePairs, string resourceId);
        bool ContainsKey(string key);
    }
}