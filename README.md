# AnkiSharp

:tada: It's finally here! You can create automatically anki cards from your C# application. :tada:

```
AnkiSharp.Anki test = new AnkiSharp.Anki(_PATH_, _NAME_);

test.AddItem("Hello", "Bonjour");
test.AddItem("How are you ?", "Comment ca va ?");
test.AddItem("Flower", "fleur");
test.AddItem("House", "Maison");

test.CreateApkgFile();
```
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


## TO-DO

:ok_hand: = Done
:zzz: = Waiting for you to be developed

- Add more fields to the cards :ok_hand:
- Being able to show what's on the front and on the back of the card :zzz:
- Possibility to change the card's CSS :zzz:
- Generate audio With synthetizer? (Need to be careful about different languages) :zzz: