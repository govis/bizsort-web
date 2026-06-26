using System;
using System.Text.Json.Serialization;

namespace BizSrt.Api.Model.Group
{
    public static class Extensions
    {
        public static void DeserializeChildren<T, TKey>(this NodeRef<TKey>[] nodes)
            where T : NodeRef<TKey>, new()
            where TKey : IComparable
        {
            nodes.DeserializeChildren<T, TKey>(null);
        }

        public static void DeserializeChildren<T, TKey>(this NodeRef<TKey>[] nodes, NodeRef<TKey> parent) 
            where T : NodeRef<TKey>, new()
            where TKey : IComparable
        {
            foreach (var n in nodes)
            {
                if (parent != null)
                    n.SetParent(parent.Id, parent);

                if (n.HasChildren)
                {
                    if (n.Children != null)
                        n.Children.DeserializeChildren<T, TKey>(n);
                    else
                        n.Children = new T[] { new T { Name = "..." } };
                }
            }
        }

        public static void SetParent<T>(this NodeRef<T>[] folders, T parentId, NodeRef<T> parent)
            where T: IComparable
        {
            foreach (var childFolder in folders)
            {
                childFolder.SetParent(parentId, parent);
            }
        }

        public static void SetParent<T>(this NodeRef<T> folder, T parentId, NodeRef<T> parent)
            where T : IComparable
        {
            if (folder.ParentId.CompareTo(default(T)) <= 0)
                folder.ParentId = parentId;
            else if (folder.ParentId.CompareTo(parentId) != 0)
                throw new ArgumentException(string.Format("Parent folder mismatch: {0} != {1}", folder.ParentId, parentId));
            if (parentId.CompareTo(default(T)) > 0 && parent != null)
                folder.Parent = parent;
        }
    }

    public interface IChildren<T>
    {
        T[] Children { get; set; }
    }

    public interface IChild<T>
    {
        T Parent { get; set; }
    }

    public enum DisplayType : byte
    {
        Name = 0,
        Text = 1,
        Path = 2
    }
    


    public enum NodeType : byte
    {
        Super = 1,
        Class = 2
    }
    
    public class IdName<T> : BizSrt.Api.Model.IdName<T>, BizSrt.Api.Model.IEntityRef<T>
    {
        [JsonPropertyName("nodeType")]
        public NodeType NodeType { get; set; }

        [JsonPropertyName("hasChildren")]
        public bool HasChildren { get; set; }
    }
    
    public class NodeRef<T> : IdName<T>
        where T: IComparable
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsRootFolder { get { return Id.CompareTo(default(T)) == 0; } }

        [JsonPropertyName("parentId")]
        public T ParentId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public NodeRef<T> Parent { get; internal set; }

        [JsonPropertyName("hasChildren")]
        public bool HasChildren { get; set; }

        [JsonPropertyName("children")]
        public NodeRef<T>[] Children { get; set; }
    }
    
    public class Node<T> : IdName<T>
    {
        [JsonPropertyName("parent")]
        public Node<T> Parent { get; set; }

        [JsonPropertyName("children")]
        public Node<T>[] Children { get; set; }
    }
    
    public class Autocomplete<T> : IdName<T>
    {
        [JsonPropertyName("path")]
        public string[] Path { get; set; }

        [JsonPropertyName("nodeType")]
        public NodeType NodeType { get; set; }

        [JsonPropertyName("hasChildren")]
        public bool HasChildren { get; set; }
    }

    public interface IEntityId<T>
    {
        T Id { get; set; }
    }

    public interface IEntityRef<T> : IEntityId<T>
    {
        string Name { get; set; }
    }

    [Flags]
    public enum SubType : byte
    {
        None = 0,
        Siblings = 1,
        Children = 2,
        GrandChildren = 4,
        Option1 = 8
    }
}
