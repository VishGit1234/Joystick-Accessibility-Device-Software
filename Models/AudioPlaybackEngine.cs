using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Controller.Models
{
    class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;
            
        private AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 1)
        {
            outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount)) { ReadFully = true };
            outputDevice.Init(mixer);
            outputDevice.Play();
            mixer.MixerInputEnded += onSampleFinished;
        }

        private void cleanUpSample(ISampleProvider sample)
        {
            foreach (var keyValuePair in allSounds)
            {
                if (keyValuePair.Value.Equals(sample))
                {
                    allSounds.Remove(keyValuePair.Key);
                    signals.Remove(keyValuePair.Key);
                    pitchBendingSounds.Remove(keyValuePair.Key);
                }
            }
        }

        private void onSampleFinished(object? sender, SampleProviderEventArgs e)
        {
            cleanUpSample(e.SampleProvider);
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            if(input.WaveFormat.Channels == 2 && mixer.WaveFormat.Channels == 1)
            {
                return new StereoToMonoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        private Dictionary<string, SineWaveProvider> signals = new();

        private Dictionary<string, SmbPitchShiftingSampleProvider> pitchBendingSounds = new();

        private Dictionary<string, ISampleProvider> allSounds = new();

        private static string GetKey(ref ControllerInputModel input)
        {
            return input.ControllerName + input.HardwareID.ToString() + input.InputType.ToString();
        }

        /// <summary>
        /// Play a Cached Sound
        /// </summary>
        /// <param name="input"></param>
        public void PlaySound(ControllerInputModel input)
        {
            if (allSounds.ContainsKey(GetKey(ref input))) Stop(input);
            string key = GetKey(ref input);
            var temp = new CachedSoundSampleProvider((CachedSound)input.Sound!);
            AddMixerInput(temp, key);
        }

        /// <summary>
        /// Play signal for a particular amount of time or forever
        /// </summary>
        /// <param name="input"></param>
        /// <param name="hz"></param>
        /// <param name="time"></param>
        public void PlaySound(ControllerInputModel input, int hz, int? time = null)
        {
            string key = GetKey(ref input);
            if (signals.ContainsKey(key))
            {
                signals[key].Frequency = hz;
            }
            else
            {
                signals[key] = (input.Sound as SineWaveProvider)!;
                signals[key].Frequency = hz;
                if (time != null)
                {
                    var offsetSignal = new OffsetSampleProvider(signals[key]);
                    offsetSignal.Take = TimeSpan.FromMilliseconds((double)time);
                    AddMixerInput(offsetSignal, key);
                }
                else
                {
                    AddMixerInput(signals[key], key);
                }
            }
        }

        /// <summary>
        /// Play a sound with adjustable pitch factor (ensure second parameter is cast to float)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pitchFactor"></param>
        public void PlaySound(ControllerInputModel input, float pitchFactor)
        {
            var key = GetKey(ref input);
            if (!allSounds.ContainsKey(key))
            {
                var pitchBendingSound = new SmbPitchShiftingSampleProvider(new CachedSoundSampleProvider((CachedSound)input.Sound!));
                pitchBendingSound.PitchFactor = pitchFactor;
                pitchBendingSounds.Add(key, pitchBendingSound);
                AddMixerInput(pitchBendingSound, key);
            }
            else
            {
                pitchBendingSounds[key].PitchFactor = pitchFactor;
            }
        }

        /// <summary>
        /// Play a sound outside of controller input. 
        /// Make sure you don't run any sounds alongside this.
        /// </summary>
        /// <param name="sound"></param>
        public void PlayCachedSound(CachedSound sound)
        {
            var temp = new CachedSoundSampleProvider(sound);
            AddMixerInput(temp, "Random");
        }

        private void AddMixerInput(ISampleProvider input, string key)
        {
            var temp = ConvertToRightChannelCount(input);
            mixer.AddMixerInput(temp);
            allSounds[key] = temp;
        }

        public void StopAll()
        {
            mixer.RemoveAllMixerInputs();
            allSounds.Clear();
            signals.Clear();
        }

        public void Stop(ControllerInputModel input)
        {
            string key = GetKey(ref input);
            if (allSounds.ContainsKey(key))
            {
                mixer.RemoveMixerInput(allSounds[key]);
                cleanUpSample(allSounds[key]);
            }
        }

        public void Stop(string keyword)
        {
            foreach(var keyValuePair in allSounds)
            {
                if (keyValuePair.Key.Contains(keyword))
                {
                    mixer.RemoveMixerInput(keyValuePair.Value);
                    cleanUpSample(allSounds[keyValuePair.Key]);
                }
            }
        }

        public bool SoundBeingPlayed(ControllerInputModel input)
        {
            return allSounds.ContainsKey(GetKey(ref input));
        }

        public bool NoSounds()
        {
            return allSounds.Count == 0;
        }

        public void Dispose()
        {
            outputDevice.Dispose();
        }

        public static readonly AudioPlaybackEngine Instance = new AudioPlaybackEngine(44100, 2);
    }
}
