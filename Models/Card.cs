using System;
using System.Collections.Generic;

namespace AnkiSharp.Models
{
    internal class Card
    {
        internal long Id { private set; get; }
        internal string Query { private set; get; } 

        public Card(Queue<CardMetadata> cardsMetadatas, Note note, string id_deck)
        {
            Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var mod = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            if (cardsMetadatas.Count != 0)
            {
                CardMetadata metadata = cardsMetadatas.Dequeue();
                Query = @"INSERT INTO cards VALUES(" + metadata.id + ", " + note.Id + ", " + id_deck +
                        ", " + "0, " + metadata.mod + ", -1, " + metadata.type + ", " + metadata.queue +
                        ", " + metadata.due + ", " + metadata.ivl + ", " + metadata.factor + ", " + metadata.reps +
                        ", " + metadata.lapses + ", " + metadata.left + ", " + metadata.odue + ", " + metadata.odid + ", 0, '');";
            }
            else
                Query = @"INSERT INTO cards VALUES(" + Id + ", " + note.Id + ", " + id_deck + ", " + "0, " +
                        mod + ", -1, 0, 0, " + note.Id + ", 0, 0, 0, 0, 0, 0, 0, 0, '');";
        }
    }
}
