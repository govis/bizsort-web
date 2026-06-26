using System.Text.Json.Serialization;

namespace BizSrt.Api.Model.List
{
    public class DirectorySliceInput<T> : SliceInput
    {
        [JsonPropertyName("category")]
        public short Category
        {
            get;
            set;
        }

        [JsonPropertyName("location")]
        public int Location
        {
            get;
            set;
        }

        [JsonPropertyName("skip")]
        public T[] Skip
        {
            get;
            set;
        }
    }
}
