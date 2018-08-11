# AnkiSharp

:tada: It's finally here! You can create automatically anki cards from your C# application. :tada:

### Nuget

```
PM> Install-Package AnkiSharp -Version 1.1.0
```

### Youtube

- Basic use : https://www.youtube.com/watch?v=UesrtZkuEKg&t=3s

### Basic use
``` csharp
AnkiSharp.Anki test = new AnkiSharp.Anki(_NAME_OF_ANKI_PACKAGE_);

test.AddItem("Hello", "Bonjour");
test.AddItem("How are you ?", "Comment ca va ?");
test.AddItem("Flower", "fleur");
test.AddItem("House", "Maison");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### SetFields
``` csharp
AnkiSharp.Anki test = new AnkiSharp.Anki(_NAME_OF_ANKI_PACKAGE_);

//Permits to set more than two fields 
test.SetFields("English", "Spanish", "French");

test.AddItem("Hello", "Hola", "Bonjour");
test.AddItem("How are you ?", "Como estas?", "Comment ca va ?");
test.AddItem("Flower", "flor", "fleur");
test.AddItem("House", "Casa", "Maison");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### SetCss
``` csharp
AnkiSharp.Anki test = new AnkiSharp.Anki(_NAME_OF_ANKI_PACKAGE_);

//Permits to change the css of your cards by providing it Css file path
test.SetCss(_PATH_OF_CSS_FILE_);

test.AddItem("Hello", "Bonjour");
test.AddItem("How are you ?", "Comment ca va ?");
test.AddItem("Flower", "fleur");
test.AddItem("House", "Maison");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```
### SetFormat
``` csharp
AnkiSharp.Anki test = new AnkiSharp.Anki(_NAME_OF_ANKI_PACKAGE_);

test.SetFields("English", "Spanish", "French");

//Everything before '<hr id=answer>' is the front of the card, everything after is the behind
test.SetFormat("{0} - {1} \\n<hr id=answer>\\n {2}");

test.AddItem("Hello", "Hola", "Bonjour");
test.AddItem("How are you ?", "Como estas?",  "Comment ca va ?");
test.AddItem("Flower", "Flor", "fleur");
test.AddItem("House", "Casa", "Maison");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### Create deck from Apkg file

``` csharp
Anki test = new Anki(_NAME_OF_ANKI_PACKAGE_, new ApkgFile(_PATH_TO_APKG_FILE_)));

// Be careful, keep the same format !
test.AddItem("Fork", "El tenedor", "La fourchette");
test.AddItem("Knife", "El cuchillo", "Le couteau");
test.AddItem("Chopsticks", "Los palillos", "Les baguettes");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### ContainsItem

``` csharp
Anki test = new Anki(_NAME_OF_ANKI_PACKAGE_, new ApkgFile(_PATH_TO_APKG_FILE_));

// Be careful, keep the same fields !
var item = test.CreateAnkiItem(("Fork", "El tenedor", "La fourchette");

if (test.ContainsItem(ankiItem) == false) // will not add if the card is entirely the same (same fields' value)
    test.AddItem(ankiItem);

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### ContainsItem with lambda

``` csharp
Anki test = new Anki(_NAME_OF_ANKI_PACKAGE_, new ApkgFile(_PATH_TO_APKG_FILE_));

var item = test.CreateAnkiItem("Hello", "Bonjour");

if (test.ContainsItem(x => { return Equals(item["FrontSide"], x["FrontSide"]); }) == false) // will not add if front of the card already exists
    test.AddItem(item);

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

### Generate Audio with MediaInfo

``` csharp

MediaInfo info = new MediaInfo()
{
    cultureInfo = new System.Globalization.CultureInfo(_CULTURE_INFO_STRING_),
    field = _FIELD_IN_WHICH_THE_AUDIO_WILL_BE_PLAYED_
};

Anki chineseToFrench = new Anki(_NAME_OF_ANKI_PACKAGE_, info);

...

```

## TO-DO

:ok_hand: = Done

:zzz: = Waiting for you to be developed

- Add more fields to the cards :ok_hand:
- Possibility to change the card's CSS :ok_hand:
- Being able to show what's on the front and on the back of the card :ok_hand:
- Get the words for other apkg files :ok_hand:
- When added from apkg file, copy cards' metadata (when to review them, ...) :ok_hand:
- Copy the revlog entries :ok_hand:
- Optimize CardMetadata and RevLogMetadata struct (doesn't need to be all doubles) :ok_hand:
- If apkg or temp files already exists remove them :ok_hand:
- ContainsItem with lambda as parameter to compare two objects :ok_hand:
- A deck can have different fields for the cards :ok_hand:
- Hint field (only works for importing) :zzz:
- Type field :zzz:
- Special fields :zzz:
- Media and latex :zzz:
- Conditional tags :zzz:
- Cloze tags :zzz:
- sub deck support :zzz:
- synchronize with ankiweb ? :zzz:
- Refactoring :zzz:
- Add images and audio :zzz:
- Generate audio with synthetizer or other tools? :ok_hand:

## Issues

- problem with due during import

## Resources

- [Anki APKG format documentation](http://decks.wikia.com/wiki/Anki_APKG_format_documentation)
- [Database Structure](https://github.com/ankidroid/Anki-Android/wiki/Database-Structure)