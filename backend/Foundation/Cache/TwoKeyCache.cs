using System;
using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;

using System;

namespace BizSrt.Api.Foundation.Cache
{
    public delegate TKey1 FetchKey<TKey1, TKey2>(TKey2 key, bool exists);

    public class ByteKey
    {
        //public ByteKey(byte[] value)
        //{
        //    this._key = value;
        //}

        public virtual byte[] Value
        {
            get;
            protected set;
        }

        public override int GetHashCode()
        {
            //return Convert.ToBase64String(Value).GetHashCode();
            int hashCode = Value[0].GetHashCode();
            for (int i = 1; i < Value.Length; i++)
                hashCode ^= Value[i].GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return equals(obj as byte[]);
        }

        protected bool equals(byte[] value)
        {
            if (value != null && Value.Length == value.Length)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    if (Value[i] != value[i])
                        return false;
                }

                return true;
            }

            return false;
        }

        /*public bool ZeroValue
        {
            get
            {
                for (var i = 0; i <= Value.Length; i++)
                {
                    if (Value[i] > 0)
                        return false;
                }
                return true;
            }
        }*/
    }

    [Flags]
    public enum TwoKeySuppress
    {
        None = 0,
        Create = 1,
        CreateNotAllowed = 2
    }

    public class TwoKeyCache<TKey1, TKey2, TValue, TModel> : ReadOneCache<TKey1, TValue> 
        where TKey1 : IComparable
        where TModel : DbContext
    {
        protected TKey1 zero = default(TKey1);

        protected FetchKey<TKey1, TKey2> fetchKeyMethod;
        protected Func<TModel> modelMethod;
        protected Func<TModel, TKey2, object, IKey<TKey1>> insertMethod;

        ConcurrentDictionary<TKey2, TKey1> _keyMap;

        public TwoKeyCache(FetchOne<TValue, TKey1> fetchOneMethod, FetchKey<TKey1, TKey2> fetchKeyMethod, Func<TModel, TKey2, object, IKey<TKey1>> insertMethod, Func<TModel> modelMethod)
            : base(fetchOneMethod)
        {
            this.fetchKeyMethod = fetchKeyMethod;
            this.insertMethod = insertMethod;
            this.modelMethod = modelMethod;

            if (fetchKeyMethod != null)
                _keyMap = new ConcurrentDictionary<TKey2, TKey1>();
        }

        public virtual TKey1 this[TKey2 key2]
        {
            get
            {
                return this[key2, TwoKeySuppress.None, null];
            }
            set
            {
                _keyMap.AddOrUpdate(key2, value, (k2, k1) => { return value; });
            }
        }

        public TKey1 this[TKey2 key2, TwoKeySuppress suppress, object data]
        {
            get
            {
                TKey1 key1;
                if (!_keyMap.TryGetValue(key2, out key1))
                {
                    key1 = fetchKeyMethod(key2, false);
                    if ((suppress & TwoKeySuppress.Create) == 0)
                    {
                        if ((key1 == null || key1.CompareTo(zero) == 0))
                        {
                            using (var dc = modelMethod())
                            {
                                var entity = insertMethod(dc, key2, data);
                                try
                                {
                                    dc.SaveChanges();
                                    key1 = Created(dc, entity, data);
                                }
                                catch (DbUpdateException ex)
                                {
                                    var sqlex = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
                                    if (sqlex != null && sqlex.Number == 2601 || sqlex.Number == 2627) //Cannot insert duplicate key / Index constraint violation
                                        key1 = fetchKeyMethod(key2, true);
                                    else
                                        throw;
                                }
                            }

                            _keyMap.TryAdd(key2, key1);
                        }
                    }
                    else if (key1 == null || key1.CompareTo(zero) == 0 && (suppress & TwoKeySuppress.CreateNotAllowed) == 0)
                        throw new InvalidOperationException("Operation Failed");
                    else if (key1 == null)
                        key1 = zero;
                }
                return key1;
            }
        }

        protected virtual TKey1 Created(TModel dc, IKey<TKey1> entity, object data)
        {
            return entity.Key;
        }

        public void Drop(TKey2 key2)
        {
            TKey1 key1;
            _keyMap.TryRemove(key2, out key1);
        }

        public bool Contains(TKey2 key2, out TKey1 key1)
        {
            return _keyMap.TryGetValue(key2, out key1);
        }
    }

    public class TwoKeyExpirationCache<TKey1, TKey2, TValue, TModel> : TwoKeyCache<TKey1, TKey2, TValue, TModel>
        where TKey1 : IComparable
        where TValue : IExpirationItem
        where TModel : DbContext
    {

        public TwoKeyExpirationCache(FetchOne<TValue, TKey1> fetchOneMethod, FetchKey<TKey1, TKey2> fetchKeyMethod, Func<TModel, TKey2, object, IKey<TKey1>> insertMethod, Func<TModel> modelMethod, int threshold)
            : base(fetchOneMethod, fetchKeyMethod, insertMethod, modelMethod)
        {
            if (threshold > 0)
                _manager = new Manager<TKey1, TValue>(this, threshold);
        }
    }
}
