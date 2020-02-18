# AnkiSharp

[![NuGet](https://img.shields.io/nuget/v/AnkiSharp.svg)](https://www.nuget.org/packages/AnkiSharp)
[![NuGet](https://img.shields.io/nuget/dt/AnkiSharp.svg)](https://www.nuget.org/packages/AnkiSharp)

:tada: It's finally here! You can create automatically anki cards from your C# application. :tada:

### Contribute

#### Other languages

I'm looking for some developers to develop the Java, Javascript and Python version, If you are interested in it please contact me here : https://clement-jean.github.io/contact/

#### C#

You can find a to do list here : [To do list](https://github.com/AnkiTools/AnkiSharp/projects/1)

### Youtube

- Basic use : https://www.youtube.com/watch?v=UesrtZkuEKg&t=3s
- Automatic audio creation : https://www.youtube.com/watch?v=uG-NWQGlYvM

### Debug

AnkiSharp is using SourceLink (https://github.com/dotnet/sourcelink). It helps you debugging your program using this nuget.

If you encounter an exception coming from Ankisharp:

	- Put a breakpoint on the function which as a problem
	- Once on the breakpoint, click on the 'Step Into' button (F11)

Then you will be redirected in the AnkiSharp nuget code. Finally, please report the issue and start contributing.

### Tutorial and documentation

This repo as a wiki : [Wiki](https://github.com/AnkiTools/AnkiSharp/wiki). You can look at any function individually or follow the guide tour by click on the next link at the end of each page.

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

//Permits to change the css of your cards by providing it a css string
test.SetCss(_CSS_CONTENT_);

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

Anki ankiObject = new Anki(_NAME_OF_ANKI_PACKAGE_, info);

...

```

### Audio quality

The current audio has a samples per second of 8000, 16 bits per sample and is in mono channel. If you would like to change it you can do it like this (be aware that the quality quickly increase or decrease the size of your deck):

``` csharp

MediaInfo info = new MediaInfo()
{
    cultureInfo = new System.Globalization.CultureInfo(_CULTURE_INFO_STRING_),
    field = _FIELD_IN_WHICH_THE_AUDIO_WILL_BE_PLAYED_,
	audioFormat = new SpeechAudioFormatInfo(_SAMPLES_PER_SECOND_, _BITS_PER_SAMPLE_, _AUDIO_CHANNEL_)
};

Anki ankiObject = new Anki(_NAME_OF_ANKI_PACKAGE_, info);

...

```

### Hint fields

``` csharp
Anki test = new Anki(_NAME_OF_ANKI_PACKAGE_);

test.SetFields("Front", "hint:Hint", "Back");
test.SetFormat("{0} - {1} \\n<hr id=answer(.*?)>\\n {2}");

test.AddItem("好的", "ok", "d'accord");

test.CreateApkgFile(_PATH_FOR_ANKI_FILE_);
```

## Resources

- [Anki APKG format documentation](http://decks.wikia.com/wiki/Anki_APKG_format_documentation)
- [Database Structure](https://github.com/ankidroid/Anki-Android/wiki/Database-Structure)
