# AnkiSharp

:tada: It's finally here! You can create automatically anki cards from your C# application. :tada:

### Basic use
```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_, _NAME_);

test.AddItem("Hello", "Bonjour");
test.AddItem("How are you ?", "Comment ca va ?");
test.AddItem("Flower", "fleur");
test.AddItem("House", "Maison");

test.CreateApkgFile();
```

### SetFields
```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_, _NAME_);

//Permits to set more than two fields 
test.SetFields("English", "Spanish", "French");

test.AddItem("Hello", "Hola", "Bonjour");
test.AddItem("How are you ?", "Como estas?", "Comment ca va ?");
test.AddItem("Flower", "flor", "fleur");
test.AddItem("House", "Casa", "Maison");

test.CreateApkgFile();
```

### SetCss
```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_, _NAME_);

//Permits to change the css of your cards by providing it Css file path
test.SetCss(_PATH_);

test.AddItem("Hello", "Bonjour");
test.AddItem("How are you ?", "Comment ca va ?");
test.AddItem("Flower", "fleur");
test.AddItem("House", "Maison");

test.CreateApkgFile();
```
### SetFormat
```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_, _NAME_);

test.SetFields("English", "Spanish", "French");

//Everything before '<br>' is the front of the card, everything after is the behind
test.SetFormat("{0} - {1} \\n<br>\\n {2}");

test.AddItem("Hello", "Hola", "Bonjour");
test.AddItem("How are you ?", "Como estas?",  "Comment ca va ?");
test.AddItem("Flower", "Flor", "fleur");
test.AddItem("House", "Casa", "Maison");

test.CreateApkgFile();
```

## TO-DO

:ok_hand: = Done

:zzz: = Waiting for you to be developed

- Add more fields to the cards :ok_hand:
- Possibility to change the card's CSS :ok_hand:
- Being able to show what's on the front and on the back of the card :ok_hand:
- Generate audio with synthetizer or other tools? (Need to be careful about different languages) :zzz: