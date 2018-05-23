using System;
using System.Collections.Generic;
using System.Linq;

namespace AnkiSharp
{
    public class FieldList : List<Field>
    {
        #region FUNCTIONS
        public new void Add(Field field)
        {
            field.SetOrd(Count);
            base.Add(field);
        }

        public string ToJSON()
        {
            var json = from field in base.FindAll(x => x != null)
                       select field.ToJSON();

            return String.Join(",\n", json.ToArray());
        }

        public override string ToString()
        {
            return String.Join("\\n<br>\\n", (object[])ToArray());
        }
        #endregion
    }
}
