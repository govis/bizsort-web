using System.Linq;
using System;
using BizSrt.Data;
using BizSrt.Model;
using BizSrt.Api.Data.Cache;

namespace BizSrt.Data
{
    public partial class Master
    {
        public static class Dictionary
        {
            public static BizSrt.Model.Dictionary Get(DictionaryType type)
            {
                if (type > 0)
                {
                    if (type == DictionaryType.ProductAttributeType)
                    {
                        var attributeTypes = LegacyCache.Dictionary.Get<BizSrt.Model.Product.Attribute.Type>(type);
                        return new BizSrt.Model.Dictionary { Items = attributeTypes.Where(at => at.Primitive).ToArray() };
                    }
                    else
                    {
                        return new BizSrt.Model.Dictionary { Items = LegacyCache.Dictionary[type] };
                    }
                }
                else
                {
                    throw new ArgumentException("InvalidInput");
                }
            }
        }

        BizSrt.Model.DictionaryItem[] GetItems(DictionaryType type)
        {
            if (type > 0)
            {
                return LegacyCache.Dictionary[type];
            }
            else
            {
                throw new ArgumentException("InvalidInput");
            }
        }
    }
}
