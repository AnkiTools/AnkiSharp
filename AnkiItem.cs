using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnkiSharp
{
    public class AnkiItem
    {
        public string Front { get; }
        public string Back { get; }

        public AnkiItem(string front, string back)
        {
            Front = front;
            Back = back;
        }
    }
}
