namespace AnkiSharp.Models
{
    public class Field
    {
        #region FIELDS
        private int _ord;
        #endregion

        #region PROPERTIES
        public string Name;
        public bool Rtl = false;
        public bool Sticky = false;
        public string Media = "[]";
        public string Font;
        public int Size;
        #endregion

        #region CTOR
        public Field(string name, string font = "Arial", int size = 12)
        {
            Name = name;
            Font = font;
            Size = size;
        }
        #endregion

        #region FUNCTIONS
        public void SetOrd(int ord)
        {
            _ord = ord;
        }

        public string ToJSON()
        {
            return "{\"name\": \"" + Name + "\", \"rtl\": " + Rtl.ToString().ToLower() + ", \"sticky\": " + Sticky.ToString().ToLower() + ", \"media\": " + Media + ", \"ord\": " + _ord + ", \"font\": \"" + Font + "\", \"size\": " + Size + "}";
        }

        public override string ToString()
        {
            return "{{" + Name + "}}";
        }
        #endregion
    }
}
