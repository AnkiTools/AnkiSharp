using System.Collections.Generic;
using System.Dynamic;

namespace AnkiSharp
{
    public class AnkiItem : DynamicObject
    {
        #region FIELDS
        Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        #endregion

        #region PROPERTIES
        public object this[string elem]
        {
            get { return _dictionary[elem]; }
        }
        
        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }
        #endregion

        #region CTOR
        public AnkiItem(FieldList fields, params string[] properties)
        {
            for (int i = 0; i < properties.Length; ++i)
            {
                _dictionary[fields[i].Name] = properties[i];
            }
        }
        #endregion

        #region FUNCTIONS
        public override bool TryGetMember(
        GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();
            
            return _dictionary.TryGetValue(name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _dictionary[binder.Name.ToLower()] = value;
            
            return true;
        }
        #endregion
    }
}
