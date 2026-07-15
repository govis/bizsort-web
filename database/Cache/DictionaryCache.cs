using System.Linq;
using BizSrt.Model;
using BizSrt.Model.Product;
using BizSrt.Data.Entities;

namespace BizSrt.Data.Cache
{
    public delegate BizSrt.Model.DictionaryItem[] FetchDictionary(BizSrt.Model.DictionaryType type);

    internal class DictionaryCache : BizSrt.Data.Cache.Dictionary
    {
        internal DictionaryCache()
            : base((BizSrt.Model.DictionaryType type) =>
            {
                using (var dc = LegacyCache.GetDbContext())
                {
                    switch (type)
                    {
                        case BizSrt.Model.DictionaryType.SecurityProfile:
                            return null;
                        case BizSrt.Model.DictionaryType.ServiceType:
                            return (from bt in dc.ServiceTypes.Where(bt => bt.Id > 0).OrderBy(bt => bt.Id).AsEnumerable()
                                    select new BizSrt.Model.ServiceType { ItemKey = bt.Id, ItemText = bt.Type }).ToArray();
                        case BizSrt.Model.DictionaryType.TransactionType:
                            return (from tt in dc.TransactionTypes.OrderBy(tt => tt.Id).AsEnumerable()
                                    select new BizSrt.Model.TransactionType { ItemKey = tt.Id, ItemText = tt.Type }).ToArray();
                        case BizSrt.Model.DictionaryType.Industry:
                            return (from i in dc.Industries.OrderBy(i => i.Id).AsEnumerable()
                                    select new BizSrt.Model.Industry { ItemKey = i.Id, ItemText = i.Type }).ToArray();
                        case BizSrt.Model.DictionaryType.ProductType:
                            return (from pt in dc.ProductTypes.Where(pt => pt.Id > 0).OrderBy(pt => pt.Id).AsEnumerable()
                                    select new BizSrt.Model.ProductType { ItemKey = pt.Id, ItemText = pt.Type }).ToArray();
                        case BizSrt.Model.DictionaryType.ProductAttributeType:
                            return (from pat in dc.ProductAttributeTypes.OrderBy(pat => pat.Id).AsEnumerable()
                                    select new BizSrt.Model.Product.Attribute.Type { ItemKey = (byte)pat.Id, Primitive = pat.Primitive, Name = pat.Name, EditorType = pat.EditorType, ValueType = pat.ValueType, DefaultValue = pat.DefaultValue, ValueOptions = (string.IsNullOrWhiteSpace(pat.ValueOptions) ? null : pat.ValueOptions.Split(';')) }).ToArray();
                        case BizSrt.Model.DictionaryType.Currency:
                            return (from c in dc.Currencies.OrderBy(c => c.Id).AsEnumerable()
                                    select new BizSrt.Model.Currency { ItemKey = c.Id, ItemText = c.ISOCode, CountryPriceFormat = c.CountryPriceFormat, PriceFormat = c.PriceFormat }).ToArray();
                        default:
                            return null;
                    }
                }
            })
        { }
    }
}
