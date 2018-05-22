# AnkiSharp

```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_);

List<AnkiSharp.AnkiItem> list = new List<AnkiSharp.AnkiItem>();

// These items are for basic cards where you have a string on the front and a string on the back
AnkiSharp.AnkiItem item = new AnkiSharp.AnkiItem("Hello", "Bonjour");
AnkiSharp.AnkiItem item2 = new AnkiSharp.AnkiItem("How are you ?", "Comment ca va ?");
AnkiSharp.AnkiItem item3 = new AnkiSharp.AnkiItem("Flower", "fleur");
AnkiSharp.AnkiItem item4 = new AnkiSharp.AnkiItem("House", "Maison");

list.Add(item);
list.Add(item2);
list.Add(item3);
list.Add(item4);

// Creates result.apkg at _PATH_ with the items in list
test.CreateApkgFile("result", list);
```

## TO-DO

- Possibility to inherit from AnkiItem, add more properties and add to the cards
- Possibility to change the card's CSS
- Generate audio With synthetizer? (Need to be careful about different languages)