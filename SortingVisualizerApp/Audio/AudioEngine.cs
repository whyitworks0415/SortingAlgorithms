using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Audio;

public sealed class AudioEngine : IDisposable
{
    private const int SampleRate = 48000;

    private readonly WaveOutEvent _waveOut;
    private readonly VoiceMixerSampleProvider _mixer;

    public int ActiveVoices => _mixer.ActiveVoices;
    public int DroppedVoices => _mixer.DroppedVoices;

    public AudioEngine()
    {
        _mixer = new VoiceMixerSampleProvider(SampleRate);

        _waveOut = new WaveOutEvent
        {
            DesiredLatency = 20,
            NumberOfBuffers = 2
        };

        _waveOut.Init(new SampleToWaveProvider(_mixer));
        _waveOut.Play();
    }

    public void PlayTriggers(ReadOnlySpan<AudioTrigger> triggers, int triggerCount, RuntimeControls controls)
    {
        if (triggerCount <= 0 || !controls.AudioEnabled)
        {
            return;
        }

        _mixer.ConfigureProcessing(
            normalizationEnabled: controls.AudioNormalizationEnabled,
            limiterEnabled: controls.AudioLimiterEnabled);

        var maxTones = Math.Clamp(controls.MaxAudioEventsPerFrame, 1, 5);
        var profile = controls.SonificationProfile;
        var stereo = controls.AudioStereoPanEnabled;
        var scheduled = 0;

        scheduled += ScheduleByPriority(
            triggers,
            triggerCount,
            controls,
            profile,
            stereo,
            maxTones,
            SortEventType.Swap,
            SortEventType.Write,
            SortEventType.PointSwap,
            SortEventType.BucketMove);

        if (scheduled < maxTones)
        {
            scheduled += ScheduleByPriority(
                triggers,
                triggerCount,
                controls,
                profile,
                stereo,
                maxTones - scheduled,
                SortEventType.MarkPivot,
                SortEventType.PointKeyComputed,
                SortEventType.OrderUpdate,
                SortEventType.PassStart);
        }

        if (scheduled < maxTones)
        {
            ScheduleByPriority(
                triggers,
                triggerCount,
                controls,
                profile,
                stereo,
                maxTones - scheduled,
                SortEventType.Compare,
                SortEventType.CharCompare);
        }
    }

    private int ScheduleByPriority(
        ReadOnlySpan<AudioTrigger> triggers,
        int triggerCount,
        RuntimeControls controls,
        SonificationProfile profile,
        bool stereoEnabled,
        int budget,
        params SortEventType[] types)
    {
        if (budget <= 0)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < triggerCount && count < budget; i++)
        {
            var trigger = triggers[i];
            if (!types.Contains(trigger.Type))
            {
                continue;
            }

            var frequency = MapFrequency(
                trigger.Value,
                Math.Max(1, trigger.MaxValue),
                controls.AudioMinFrequency,
                controls.AudioMaxFrequency);
            var durationMs = Math.Clamp(
                controls.ToneDurationMs
                * DurationScaleForType(trigger.Type)
                * ProfileDurationScale(profile),
                20.0f,
                55.0f);
            var amplitude = AmplitudeForType(trigger.Type, controls.AudioVolume, profile);
            var attackMs = Math.Clamp(controls.AttackMs * ProfileAttackScale(profile), 2.0f, 8.0f);
            var decayMs = Math.Clamp(controls.DecayMs * ProfileDecayScale(profile), 8.0f, 40.0f);
            var releaseMs = Math.Clamp(controls.ReleaseMs * ProfileReleaseScale(profile), 5.0f, 24.0f);
            var pan = stereoEnabled ? Math.Clamp(trigger.Pan, -1.0f, 1.0f) : 0.0f;

            _mixer.ScheduleVoice(
                frequency,
                durationMs,
                amplitude,
                controls.Waveform,
                attackMs: attackMs,
                decayMs: decayMs,
                releaseMs: releaseMs,
                priority: PriorityOf(trigger.Type, profile),
                polyphonyCap: Math.Clamp(controls.PolyphonyCap, 4, 32),
                pan: pan);

            count++;
        }

        return count;
    }

    private static int PriorityOf(SortEventType type, SonificationProfile profile)
    {
        var basePriority = type switch
        {
            SortEventType.Swap or SortEventType.Write or SortEventType.PointSwap or SortEventType.BucketMove => 3,
            SortEventType.MarkPivot or SortEventType.PointKeyComputed or SortEventType.OrderUpdate or SortEventType.PassStart => 2,
            _ => 1
        };

        if (profile == SonificationProfile.CompareHeavy && type is SortEventType.Compare or SortEventType.CharCompare)
        {
            basePriority += 1;
        }

        return basePriority;
    }

    private static float DurationScaleForType(SortEventType type)
    {
        return type switch
        {
            SortEventType.Swap => 1.15f,
            SortEventType.Write => 1.0f,
            SortEventType.PointSwap => 1.0f,
            SortEventType.BucketMove => 0.95f,
            SortEventType.MarkPivot => 1.1f,
            _ => 0.82f
        };
    }

    private static float AmplitudeForType(SortEventType type, float master, SonificationProfile profile)
    {
        var scale = type switch
        {
            SortEventType.Swap => 1.0f,
            SortEventType.Write => 0.8f,
            SortEventType.PointSwap => 0.9f,
            SortEventType.BucketMove => 0.85f,
            SortEventType.PointKeyComputed => 0.7f,
            SortEventType.OrderUpdate => 0.7f,
            SortEventType.CharCompare => 0.45f,
            SortEventType.MarkPivot => 0.75f,
            _ => 0.45f
        };

        scale *= ProfileAmplitudeScale(profile, type);
        return Math.Clamp(master * scale, 0.0f, 1.0f);
    }

    private static float ProfileDurationScale(SonificationProfile profile)
    {
        return profile switch
        {
            SonificationProfile.Percussive => 0.72f,
            SonificationProfile.Soft => 1.35f,
            _ => 1.0f
        };
    }

    private static float ProfileAttackScale(SonificationProfile profile)
    {
        return profile switch
        {
            SonificationProfile.Percussive => 0.7f,
            SonificationProfile.Soft => 1.2f,
            _ => 1.0f
        };
    }

    private static float ProfileDecayScale(SonificationProfile profile)
    {
        return profile switch
        {
            SonificationProfile.Percussive => 0.75f,
            SonificationProfile.Soft => 1.5f,
            _ => 1.0f
        };
    }

    private static float ProfileReleaseScale(SonificationProfile profile)
    {
        return profile switch
        {
            SonificationProfile.Percussive => 0.7f,
            SonificationProfile.Soft => 1.35f,
            _ => 1.0f
        };
    }

    private static float ProfileAmplitudeScale(SonificationProfile profile, SortEventType type)
    {
        return profile switch
        {
            SonificationProfile.CompareHeavy when type is SortEventType.Compare or SortEventType.CharCompare => 1.45f,
            SonificationProfile.Percussive => 0.9f,
            SonificationProfile.Soft => 0.78f,
            _ => 1.0f
        };
    }

    private static float MapFrequency(int value, int maxValue, float fMin, float fMax)
    {
        fMin = Math.Max(20.0f, fMin);
        fMax = Math.Max(fMin + 1.0f, fMax);

        var normalized = Math.Clamp(value / (float)Math.Max(1, maxValue), 0.0f, 1.0f);
        return fMin * MathF.Pow(fMax / fMin, normalized);
    }

    public void Dispose()
    {
        _waveOut.Stop();
        _waveOut.Dispose();
    }

    private sealed class VoiceMixerSampleProvider : ISampleProvider
    {
        private sealed class Voice
        {
            public required float Frequency;
            public required float Amplitude;
            public required WaveformType Waveform;
            public required int Priority;
            public required int TotalSamples;
            public required int AttackSamples;
            public required int DecaySamples;
            public required int SustainSamples;
            public required int ReleaseSamples;
            public required float SustainLevel;
            public required float Pan;
            public required long CreatedAt;

            public int SampleIndex;
            public double Phase;
            public double PhaseIncrement;
        }

        private const float TargetRms = 0.22f;
        private readonly object _voiceLock = new();
        private readonly List<Voice> _voices = new(32);
        private readonly WaveFormat _waveFormat;
        private long _voiceCounter;

        private int _activeVoices;
        private int _droppedVoices;
        private bool _normalizationEnabled = true;
        private bool _limiterEnabled = true;
        private float _smoothedRms = 0.2f;

        public int ActiveVoices => Volatile.Read(ref _activeVoices);
        public int DroppedVoices => Volatile.Read(ref _droppedVoices);

        public WaveFormat WaveFormat => _waveFormat;

        public VoiceMixerSampleProvider(int sampleRate)
        {
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels: 2);
        }

        public void ConfigureProcessing(bool normalizationEnabled, bool limiterEnabled)
        {
            _normalizationEnabled = normalizationEnabled;
            _limiterEnabled = limiterEnabled;
        }

        public void ScheduleVoice(
            float frequency,
            float durationMs,
            float amplitude,
            WaveformType waveform,
            float attackMs,
            float decayMs,
            float releaseMs,
            int priority,
            int polyphonyCap,
            float pan)
        {
            var totalSamples = Math.Max(1, (int)(_waveFormat.SampleRate * (durationMs / 1000.0f)));
            var attack = Math.Max(1, (int)(_waveFormat.SampleRate * (attackMs / 1000.0f)));
            var decay = Math.Max(1, (int)(_waveFormat.SampleRate * (decayMs / 1000.0f)));
            var release = Math.Max(1, (int)(_waveFormat.SampleRate * (releaseMs / 1000.0f)));

            if (attack + decay + release >= totalSamples)
            {
                var scale = (totalSamples - 1.0f) / Math.Max(1.0f, attack + decay + release);
                attack = Math.Max(1, (int)(attack * scale));
                decay = Math.Max(1, (int)(decay * scale));
                release = Math.Max(1, totalSamples - attack - decay - 1);
            }

            var sustain = Math.Max(0, totalSamples - attack - decay - release);

            var voice = new Voice
            {
                Frequency = Math.Clamp(frequency, 20.0f, 20000.0f),
                Amplitude = Math.Clamp(amplitude, 0.0f, 1.0f),
                Waveform = waveform,
                Priority = priority,
                TotalSamples = totalSamples,
                AttackSamples = attack,
                DecaySamples = decay,
                SustainSamples = sustain,
                ReleaseSamples = release,
                SustainLevel = 0.62f,
                Pan = Math.Clamp(pan, -1.0f, 1.0f),
                CreatedAt = Interlocked.Increment(ref _voiceCounter),
                SampleIndex = 0,
                Phase = 0.0,
                PhaseIncrement = (2.0 * Math.PI * frequency) / _waveFormat.SampleRate
            };

            lock (_voiceLock)
            {
                if (_voices.Count >= polyphonyCap)
                {
                    var victimIndex = FindVictimIndex(priority);
                    if (victimIndex < 0)
                    {
                        Interlocked.Increment(ref _droppedVoices);
                        return;
                    }

                    _voices.RemoveAt(victimIndex);
                }

                _voices.Add(voice);
            }
        }

        private int FindVictimIndex(int newPriority)
        {
            var victim = -1;
            var victimPriority = int.MaxValue;
            var oldestCreated = long.MaxValue;

            for (var i = 0; i < _voices.Count; i++)
            {
                var v = _voices[i];
                if (v.Priority > newPriority)
                {
                    continue;
                }

                if (v.Priority < victimPriority || (v.Priority == victimPriority && v.CreatedAt < oldestCreated))
                {
                    victim = i;
                    victimPriority = v.Priority;
                    oldestCreated = v.CreatedAt;
                }
            }

            return victim;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            if (count <= 0)
            {
                return 0;
            }

            var frames = count / 2;
            if (frames <= 0)
            {
                return count;
            }

            lock (_voiceLock)
            {
                for (var frame = 0; frame < frames; frame++)
                {
                    float mixedL = 0.0f;
                    float mixedR = 0.0f;

                    for (var i = _voices.Count - 1; i >= 0; i--)
                    {
                        var voice = _voices[i];
                        if (voice.SampleIndex >= voice.TotalSamples)
                        {
                            _voices.RemoveAt(i);
                            continue;
                        }

                        var env = Envelope(voice);
                        var waveform = EvaluateWaveform((float)voice.Phase, voice.Waveform);
                        var sample = waveform * env * voice.Amplitude;
                        var (leftGain, rightGain) = PanGains(voice.Pan);

                        mixedL += sample * leftGain;
                        mixedR += sample * rightGain;

                        voice.Phase += voice.PhaseIncrement;
                        voice.SampleIndex++;
                    }

                    var rmsInstant = MathF.Sqrt((mixedL * mixedL + mixedR * mixedR) * 0.5f);
                    _smoothedRms = _smoothedRms * 0.995f + rmsInstant * 0.005f;

                    var gain = _normalizationEnabled
                        ? Math.Clamp(TargetRms / Math.Max(0.02f, _smoothedRms), 0.35f, 1.9f)
                        : 1.0f;
                    mixedL *= gain;
                    mixedR *= gain;

                    if (_limiterEnabled)
                    {
                        mixedL = SoftLimit(mixedL);
                        mixedR = SoftLimit(mixedR);
                    }
                    else
                    {
                        mixedL = Math.Clamp(mixedL, -1.0f, 1.0f);
                        mixedR = Math.Clamp(mixedR, -1.0f, 1.0f);
                    }

                    var o = offset + frame * 2;
                    buffer[o] = mixedL;
                    buffer[o + 1] = mixedR;
                }

                Interlocked.Exchange(ref _activeVoices, _voices.Count);
            }

            if ((count & 1) != 0)
            {
                buffer[offset + count - 1] = 0.0f;
            }

            return count;
        }

        private static (float Left, float Right) PanGains(float pan)
        {
            var clamped = Math.Clamp(pan, -1.0f, 1.0f);
            var left = MathF.Sqrt(0.5f * (1.0f - clamped));
            var right = MathF.Sqrt(0.5f * (1.0f + clamped));
            return (left, right);
        }

        private static float SoftLimit(float x)
        {
            const float threshold = 0.92f;
            var ax = MathF.Abs(x);
            if (ax <= threshold)
            {
                return x;
            }

            var excess = ax - threshold;
            var compressed = threshold + excess / (1.0f + excess * 6.0f);
            return MathF.Sign(x) * MathF.Min(1.0f, compressed);
        }

        private static float Envelope(Voice voice)
        {
            var idx = voice.SampleIndex;
            if (idx < voice.AttackSamples)
            {
                return idx / (float)voice.AttackSamples;
            }

            idx -= voice.AttackSamples;
            if (idx < voice.DecaySamples)
            {
                var t = idx / (float)voice.DecaySamples;
                return 1.0f + (voice.SustainLevel - 1.0f) * t;
            }

            idx -= voice.DecaySamples;
            if (idx < voice.SustainSamples)
            {
                return voice.SustainLevel;
            }

            idx -= voice.SustainSamples;
            if (voice.ReleaseSamples <= 0)
            {
                return 0.0f;
            }

            var releaseT = idx / (float)Math.Max(1, voice.ReleaseSamples);
            return voice.SustainLevel * Math.Clamp(1.0f - releaseT, 0.0f, 1.0f);
        }

        private static float EvaluateWaveform(float phase, WaveformType waveform)
        {
            return waveform switch
            {
                WaveformType.Square => MathF.Sign(MathF.Sin(phase)),
                WaveformType.Triangle => (2.0f / MathF.PI) * MathF.Asin(MathF.Sin(phase)),
                _ => MathF.Sin(phase)
            };
        }
    }
}
