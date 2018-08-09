using System.Globalization;
using System.Media;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;

namespace AnkiSharp.Helpers
{
    internal static class SynthetizerHelper
    {
        internal static void CreateAudio(string path, string text, CultureInfo cultureInfo)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                synth.SetOutputToWaveFile(path,
                  new SpeechAudioFormatInfo(32000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

                PromptBuilder builder = new PromptBuilder(cultureInfo);
                builder.AppendText(text);
                
                synth.Speak(builder);
            }
        }

    }
}
