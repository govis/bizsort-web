using BizSrt.Foundation.Cache;
using BizSrt.Foundation.Cache;
using BizSrt.Model;
using BizSrt.Model.Group;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BizSrt.Data
{
    internal class CategoriesCache : TreeCache<short, CachedCategory.NameKey, CachedCategory, byte, BizSrt.Data.AppDbContext>
    {
        internal CategoriesCache()
            : base(
            (short categoryId) =>
            {
                using (var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext())
                {
                    var cq = (from c in dc.Categories
                              where c.Parent == categoryId //c.Parent != null && c.Parent.Value == categoryId
                              orderby c.SortOrder, c.Name
                              select new { c.Id, c.Name, c.ServiceType, c.ProductType, c.TransactionType, c.Industry, c.QualifyingParent, NAICSCode = c.NAICSCode != null ? c.NAICSCode.Value : 0, c.SortOrder }).AsEnumerable();
                    return cq.Select(ct => new CachedCategory(ct.Id, ct.Name, ct.ServiceType, ct.Industry, ct.ProductType, ct.TransactionType, categoryId, (ct.QualifyingParent != null && ct.QualifyingParent.HasValue ? ct.QualifyingParent.Value : (short)0)) { NAICSCode = ct.NAICSCode, SortOrder = ct.SortOrder }).ToArray();
                }
            }, (short categoryId) =>
            {
                using (var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext())
                {
                    var ct = (from c in dc.Categories
                              where c.Id == categoryId
                              select new { c.Id, c.Name, c.ServiceType, c.ProductType, c.TransactionType, c.Industry, c.Parent, c.QualifyingParent, NAICSCode = c.NAICSCode != null ? c.NAICSCode.Value : 0, c.SortOrder }).SingleOrDefault();
                    return ct != null ? new CachedCategory(ct.Id, ct.Name, ct.ServiceType, ct.Industry, ct.ProductType, ct.TransactionType, (ct.Parent != null && ct.Parent.HasValue ? ct.Parent.Value : (short)-1), (ct.QualifyingParent != null && ct.QualifyingParent.HasValue ? ct.QualifyingParent.Value : (short)0)) { NAICSCode = ct.NAICSCode, SortOrder = ct.SortOrder } : null;
                }
            }, null, null, null) {}


        public BizSrt.Model.Group.NodeRef<short>[] GetChildren(short parentCategory, short lookupCategory)
        {
            return GetChildren(parentCategory, lookupCategory, 0, category => new BizSrt.Model.Group.NodeRef<short>
            {
                Id = category.Id,
                Name = category.Name,
                HasChildren = HasChildren(category.Id, 0)
            }, this);
        }

        public override void Drop(short key)
        {
            base.Drop(key);
            BizSrt.Data.Cache.LegacyCache.CategorySearch.Reset();
        }

        public void Drop(short[] categories, IEnumerable<short> parents)
        {
            foreach (var category in categories)
                base.Drop(category);
            foreach (var parent in parents)
                DropGroup(parent);
            BizSrt.Data.Cache.LegacyCache.CategorySearch.Reset();
        }
    }

    internal class CachedCategory : CachedNode<short>
    {
        #region Part Cache
        /*[Flags]
        public enum PartType : ushort
        {
            UnwoundChildren = 1,
            All = 65535
        }

        public void Reset(PartType type)
        {
            if ((type & PartType.UnwoundChildren) > 0)
            {
                this.unwoundChildren = null;
            }
        }
        short[] unwoundChildren;
        public short[] UnwoundChildren
        {
            get
            {
                return base.GetArray<short, short>(ref unwoundChildren, this.Id, (category) =>
                {
                    using (var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext())
                    {
                        return (from cu in dc.Categories_Unwound
                                where cu.Parent == category
                                select cu.Child).ToArray();
                    }
                });
            }
        }*/
        #endregion

        public struct NameKey
        {
        }

        #region Cache
        CachedCategoryProductAttribute[] attributes;
        internal CachedCategoryProductAttribute[] getAttributes(int category, Func<int, CachedCategoryProductAttribute[]> fetchMethod)
        {
            return GetArray(ref attributes, category, fetchMethod);
        }

        internal BizSrt.Model.Category Config
        {
            get
            {
                var categoryAttributes = getAttributes(_id, (category) =>
                {
                    using (var dc = BizSrt.Data.Cache.LegacyCache.GetDbContext())
                    {
                        return (from cpa in dc.CategoryProductAttributes.Where(categoryProductAttribute => categoryProductAttribute.Category == category).AsEnumerable()
                                    //where cpa.Category == category
                                select new CachedCategoryProductAttribute { Name = cpa.Name, Type = cpa.Type, Requirement = cpa.Requirement, Group = cpa.Group, DefaultValue = cpa.DefaultValue, ValueOptions = (string.IsNullOrWhiteSpace(cpa.ValueOptions) ? null : cpa.ValueOptions.Split(';')) }).ToArray();
                    }
                });

                var q = from ca in categoryAttributes
                        join at in BizSrt.Data.Cache.LegacyCache.Dictionary.Get<Model.Product.Attribute.Type>(BizSrt.Model.DictionaryType.ProductAttributeType) on ca.Type equals at.ItemKey
                        select new BizSrt.Model.Category.ProductAttribute { Type = at.ItemKey, Name = ca.Name, EditorType = at.EditorType, ValueType = at.ValueType, DefaultValue = ca.DefaultValue ?? at.DefaultValue, ValueOptions = ca.ValueOptions ?? at.ValueOptions, Requirement = (Model.Product.Attribute.Requirement)ca.Requirement };

                var productAttributes = q.ToArray();
                return new BizSrt.Model.Category { Service = ServiceType, Product = ProductType, Transaction = TransactionType, Industry = Industry, ProductAttributes = productAttributes.Length > 0 ? productAttributes : null };
            }
        }
        #endregion

        long _serviceType;
        public long ServiceType
        {
            get { return _serviceType; }
        }

        short _productType;
        public short ProductType
        {
            get { return _productType; }
        }

        long _industry;
        public long Industry
        {
            get { return _industry; }
        }

        short _transactionType;
        public short TransactionType
        {
            get { return _transactionType; }
        }

        protected internal CachedCategory(short id, string name, long serviceType, long industry, short productType, short transactionType, short parent, short qualifyingParent)
        {
            _id = id;
            _name = name;
            if (serviceType > 0)
                _type |= 1;
            if (_productType > 0)
                _type |= 2;
            _serviceType = serviceType;
            _industry = industry;
            _productType = productType;
            _transactionType = transactionType;
            _parentKey = parent;
            _qualifyingParent = qualifyingParent;
        }

        public override CachedNode<short, byte> Parent
        {
            get
            {
                return ParentKey >= 0 ? BizSrt.Data.Cache.LegacyCache.Categories[ParentKey] : null;
            }
        }

        public string QualifiedName
        {
            get
            {
                var path = AutocompletePath(0);
                return path != null && path.Length > 0 ? Name + " in " + path[0] : Name;
            }
        }

        public CachedCategory QualifyingScope
        {
            get
            {
                if (QualifyingParentKey != 0)
                {
                    return QualifyingParentKey == Id ? this : QualifyingParent;
                }
                else if (ParentKey > 0)
                    return ((CachedCategory)Parent).QualifyingScope;
                else
                    return this;
            }
        }

        protected short _qualifyingParent;
        public short QualifyingParentKey
        {
            get { return _qualifyingParent; }
        }
        public CachedCategory QualifyingParent
        {
            get { return _qualifyingParent > 0 ? BizSrt.Data.Cache.LegacyCache.Categories[_qualifyingParent] : null; }
        }

        public int NAICSCode
        {
            get;
            set;
        }

        public override string DisplayText
        {
            get
            {
                return (ParentKey == -1 ? "Unspecified" : Name);
            }
        }

        public override string DisplayPath
        {
            get
            {
                return (ParentKey > 0 ? Parent.DisplayPath + "\\" : string.Empty) + DisplayText;
            }
        }

        public string[] AutocompletePath(short scope)
        {
            
            CachedCategory parent;
            if (QualifyingParentKey == 0 || QualifyingParentKey == Id)
            {
                parent = QualifyingScope;
                if (parent == this)
                    parent = null;
            }
            else
                parent = QualifyingParent;
            /*var path = new List<string>();
            while (parent != null && parent.Id != scope)
            {
                path.Add(parent.Name);
                parent = (CachedCategory)parent.Parent;
            }
            return path.Count > 0 ? path.ToArray() : null;*/
            
            //Return first qualifying parent
            if (parent != null && parent.Id != scope)
                return new string[] { parent.Name };
            else
                return null;
        }

        public override BizSrt.Model.Group.Node<short> ParentNode(byte type)
        {
            if (ParentKey >= 0)
            {
                var parents = this.GetPath(null).Where(c => c.Id != _id).Select(c => c.Id);
                var parentCategory = BizSrt.Data.Cache.LegacyCache.Categories[ParentKey];
                return new BizSrt.Model.Group.Node<short> { Id = ParentKey, Name = parentCategory.Name, NodeType = parentCategory.NodeType(type), Parent = parentCategory.ParentNode(type) };
            }
            else
                return null;
        }

        public override bool OfType(byte type)
        {
            //if (type == 0)
                return true;
            /*else if (((type & (byte)global::Model.Category.MemberType.Company) > 0 && _companyType > 0) ||
                    ((type & (byte)global::Model.Category.MemberType.Product) > 0 && _productType > 0))
                return true;
            else
                return false;*/
        }

        public override NodeType NodeType(byte type)
        {
            var isClass = false;
            if (type == 0)
                isClass = _serviceType > 0 || _productType > 0;
            else if (((type & (byte)BizSrt.Model.Category.MemberType.Company) > 0 && _serviceType > 0) ||
                    ((type & (byte)BizSrt.Model.Category.MemberType.Product) > 0 && _productType > 0))
                isClass = true;

            return isClass ? BizSrt.Model.Group.NodeType.Class : BizSrt.Model.Group.NodeType.Super;
        }

        internal Category ToModel(BizSrt.Model.Group.DisplayType type)
        {
            return ToModel<Category>(type);
        }
    }

    internal class CachedCategoryProductAttribute
    {
        public string Name
        {
            get;
            set;
        }

        public short Type
        {
            get;
            set;
        }

        public byte Requirement
        {
            get;
            set;
        }

        public string Group
        {
            get;
            set;
        }

        public string DefaultValue
        {
            get;
            set;
        }

        public string[] ValueOptions
        {
            get;
            set;
        }
    }
}
