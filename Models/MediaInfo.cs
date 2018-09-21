using System;
using System.Globalization;
using System.Speech.AudioFormat;

namespace AnkiSharp
{
    public class MediaInfo
    {
        public CultureInfo cultureInfo;
        public string field;
        public string extension = ".wav";
        public SpeechAudioFormatInfo audioFormat = new SpeechAudioFormatInfo(8000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
    }
}
