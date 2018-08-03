using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace AnkiSharp.Models
{
    public class AnkiSharpDynamic : DynamicObject
    {
        #region FIELDS
        Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        #endregion

        #region PROPERTIES
        public object this[string elem]
        {
            get { return _dictionary[elem]; }
            set { _dictionary[elem] = value; }
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
        public AnkiSharpDynamic()
        {
        }
        #endregion

        #region FUNCTIONS
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();

            return _dictionary.TryGetValue(name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _dictionary[binder.Name.ToLower()] = value;

            return true;
        }

        public static bool operator==(AnkiSharpDynamic first, AnkiSharpDynamic second)
        {
            foreach (var pair in first._dictionary)
            {
                if (second._dictionary[pair.Key].ToString() != pair.Value.ToString())
                    return false;
            }

            return true;
        }

        public static bool operator!=(AnkiSharpDynamic first, AnkiSharpDynamic second)
        {
            return !(first == second);
        }

        public T ToObject<T>(T obj = null) where T : class, new()
        {
            T result = obj ?? new T();
            PropertyInfo propertyInfo;

            foreach (var pair in _dictionary)
            {
                    propertyInfo = result.GetType().GetProperty(pair.Key);
                    propertyInfo.SetValue(result, Convert.ChangeType(pair.Value, propertyInfo.PropertyType), null);
            }

            return result;
        }
        #endregion
    }
}
