using Newtonsoft.Json;

namespace Repository.App.Database
{
    public class MetadataObject : IEquatable<MetadataObject?>
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ResourceId { get; set; }
        public string CreationDate { get; set; }
        public ResourceInfo ResourceInfo { get; set; }

        public GenerationTree GenerationTree { get; set; }
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as MetadataObject);
        }

        public bool Equals(MetadataObject? other)
        {
            return other is not null &&
                   //ResourceId == other.ResourceId &&      // ResourceId is the same as the one used for lookup. Shouldn't matter when comparing
                   //CreationDate == other.CreationDate &&  // CreationDate should not matter when comparing
                   EqualityComparer<ResourceInfo>.Default.Equals(ResourceInfo, other.ResourceInfo) &&
                   EqualityComparer<GenerationTree>.Default.Equals(GenerationTree, other.GenerationTree);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceInfo, GenerationTree);
        }
        #endregion equals
    }
    public class ResourceInfo : IEquatable<ResourceInfo?>
    {
        public string ResourceLabel { get; set; }
        public string ResourceType { get; set; } // EventLog, EventStream, Image, ProcessModel, PetriNet, Histogram, Graph, Alignment, etc.
        public string Host { get; set; }
        public string? FileExtension { get; set; }
        public string? StreamTopic { get; set; }
        public string? Description { get; set; }
        public bool Dynamic { get; set; } = false;
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as ResourceInfo);
        }

        public bool Equals(ResourceInfo? other)
        {
            return other is not null &&
                   ResourceLabel == other.ResourceLabel &&
                   ResourceType == other.ResourceType &&
                   Host == other.Host &&
                   FileExtension == other.FileExtension &&
                   StreamTopic == other.StreamTopic &&
                   Description == other.Description &&
                   Dynamic == other.Dynamic;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceLabel, ResourceType, Host, FileExtension, StreamTopic, Description, Dynamic);
        }
        #endregion equals
    }
    public class GenerationTree : IEquatable<GenerationTree?>
    {
        public GeneratedFrom? GeneratedFrom { get; set; }
        public List<Parent>? Parents { get; set; }
        public List<Child>? Children { get; set; }
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as GenerationTree);
        }

        public bool Equals(GenerationTree? other)
        {
            return other is not null &&
                   EqualityComparer<GeneratedFrom?>.Default.Equals(GeneratedFrom, other.GeneratedFrom) &&
                   Parents.ListEquals(other.Parents) &&
                   Children.ListEquals(other.Children);
            //EqualityComparer<List<Parent>?>.Default.Equals(Parents, other.Parents) &&
            //EqualityComparer<List<Child>?>.Default.Equals(Children, other.Children);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GeneratedFrom, Parents, Children);
        }
        #endregion equals
    }
    // TODO: Consider if this should be an object instead, since some of the input may vary and it would be easier when used through Histogram.
    public class GeneratedFrom : IEquatable<GeneratedFrom?>
    {
        public string SourceHost { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? SourceId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? SourceLabel { get; set; }
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as GeneratedFrom);
        }

        public bool Equals(GeneratedFrom? other)
        {
            return other is not null &&
                   SourceHost == other.SourceHost &&
                   SourceId == other.SourceId &&
                   SourceLabel == other.SourceLabel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SourceHost, SourceId, SourceLabel);
        }
        #endregion equals
    }
    public class Parent : IEquatable<Parent?>
    {
        public string ResourceId { get; set; }
        public string UsedAs { get; set; }  // Relates to the "Name" key from Miner config (in key "ResourceInput"). Useful for Miners that take multiple input files to differentiate how it was used.
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as Parent);
        }

        public bool Equals(Parent? other)
        {
            return other is not null &&
                   ResourceId == other.ResourceId &&
                   UsedAs == other.UsedAs;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceId, UsedAs);
        }
        #endregion equals
    }
    public class Child : IEquatable<Child?>
    {
        public string ResourceId { get; set; }
        #region equals
        public override bool Equals(object? obj)
        {
            return Equals(obj as Child);
        }

        public bool Equals(Child? other)
        {
            return other is not null &&
                   ResourceId == other.ResourceId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceId);
        }
        #endregion equals
    }

    #region extensionMethods
    public static class ExtensionClass
    {
        public static bool ListEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1 ?? Enumerable.Empty<T>())
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2 ?? Enumerable.Empty<T>())
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }
        public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1 ?? Enumerable.Empty<T>())
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2 ?? Enumerable.Empty<T>())
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }
    }
    #endregion
}
