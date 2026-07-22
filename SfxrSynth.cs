using System;
using System.Collections.Generic;

namespace EternSynth
{
    public class SfxrSynth
    {
        // Synthesizer parameters (values range from 0.0f to 1.0f unless specified)
        public int wave_type = 0;             // 0=Square, 1=Sawtooth, 2=Sine, 3=Noise, 4=Triangle
        public float p_env_attack = 0.0f;      // Attack Time
        public float p_env_sustain = 0.3f;     // Sustain Time
        public float p_env_punch = 0.0f;       // Sustain Punch
        public float p_env_decay = 0.4f;       // Decay Time
        public float p_base_freq = 0.3f;       // Start Frequency
        public float p_freq_limit = 0.0f;      // Min Frequency
        public float p_freq_ramp = 0.0f;       // Slide
        public float p_freq_dramp = 0.0f;      // Delta Slide
        public float p_vib_strength = 0.0f;    // Vibrato Depth
        public float p_vib_speed = 0.0f;       // Vibrato Speed
        public float p_arp_speed = 0.0f;       // Pitch Jump Repeat Speed
        public float p_arp_mod = 0.0f;         // Pitch Jump Amount
        public float p_duty = 0.5f;            // Square Duty
        public float p_duty_ramp = 0.0f;       // Duty Sweep
        public float p_repeat_speed = 0.0f;    // Repeat Speed
        public float p_pha_offset = 0.0f;      // Phaser Offset
        public float p_pha_ramp = 0.0f;        // Phaser Sweep
        public float p_lpf_freq = 1.0f;        // Low-pass Filter Cutoff
        public float p_lpf_ramp = 0.0f;        // Low-pass Filter Sweep
        public float p_lpf_resonance = 0.0f;   // Low-pass Filter Resonance (Q)
        public float p_hpf_freq = 0.0f;        // High-pass Filter Cutoff
        public float p_hpf_ramp = 0.0f;        // High-pass Filter Sweep
        public float sound_vol = 0.5f;         // Sound Volume (0.0 to 1.0)

        private Random random = new Random();

        public SfxrSynth()
        {
            ResetParams();
        }

        public void ResetParams()
        {
            wave_type = 0;
            p_env_attack = 0.0f;
            p_env_sustain = 0.3f;
            p_env_punch = 0.0f;
            p_env_decay = 0.4f;
            p_base_freq = 0.3f;
            p_freq_limit = 0.0f;
            p_freq_ramp = 0.0f;
            p_freq_dramp = 0.0f;
            p_vib_strength = 0.0f;
            p_vib_speed = 0.0f;
            p_arp_speed = 0.0f;
            p_arp_mod = 0.0f;
            p_duty = 0.5f;
            p_duty_ramp = 0.0f;
            p_repeat_speed = 0.0f;
            p_pha_offset = 0.0f;
            p_pha_ramp = 0.0f;
            p_lpf_freq = 1.0f;
            p_lpf_ramp = 0.0f;
            p_lpf_resonance = 0.0f;
            p_hpf_freq = 0.0f;
            p_hpf_ramp = 0.0f;
            sound_vol = 0.5f;
        }

        // Serialization
        public string GetSettingsString()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return string.Join(",", new string[] {
                wave_type.ToString(),
                p_env_attack.ToString("F4", culture),
                p_env_sustain.ToString("F4", culture),
                p_env_punch.ToString("F4", culture),
                p_env_decay.ToString("F4", culture),
                p_base_freq.ToString("F4", culture),
                p_freq_limit.ToString("F4", culture),
                p_freq_ramp.ToString("F4", culture),
                p_freq_dramp.ToString("F4", culture),
                p_vib_strength.ToString("F4", culture),
                p_vib_speed.ToString("F4", culture),
                p_arp_speed.ToString("F4", culture),
                p_arp_mod.ToString("F4", culture),
                p_duty.ToString("F4", culture),
                p_duty_ramp.ToString("F4", culture),
                p_repeat_speed.ToString("F4", culture),
                p_pha_offset.ToString("F4", culture),
                p_pha_ramp.ToString("F4", culture),
                p_lpf_freq.ToString("F4", culture),
                p_lpf_ramp.ToString("F4", culture),
                p_lpf_resonance.ToString("F4", culture),
                p_hpf_freq.ToString("F4", culture),
                p_hpf_ramp.ToString("F4", culture),
                sound_vol.ToString("F4", culture)
            });
        }

        public void SetSettingsString(string settings)
        {
            if (string.IsNullOrEmpty(settings)) return;
            string[] parts = settings.Split(',');
            if (parts.Length < 23) return;

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                wave_type = int.Parse(parts[0]);
                p_env_attack = float.Parse(parts[1], culture);
                p_env_sustain = float.Parse(parts[2], culture);
                p_env_punch = float.Parse(parts[3], culture);
                p_env_decay = float.Parse(parts[4], culture);
                p_base_freq = float.Parse(parts[5], culture);
                p_freq_limit = float.Parse(parts[6], culture);
                p_freq_ramp = float.Parse(parts[7], culture);
                p_freq_dramp = float.Parse(parts[8], culture);
                p_vib_strength = float.Parse(parts[9], culture);
                p_vib_speed = float.Parse(parts[10], culture);
                p_arp_speed = float.Parse(parts[11], culture);
                p_arp_mod = float.Parse(parts[12], culture);
                p_duty = float.Parse(parts[13], culture);
                p_duty_ramp = float.Parse(parts[14], culture);
                p_repeat_speed = float.Parse(parts[15], culture);
                p_pha_offset = float.Parse(parts[16], culture);
                p_pha_ramp = float.Parse(parts[17], culture);
                p_lpf_freq = float.Parse(parts[18], culture);
                p_lpf_ramp = float.Parse(parts[19], culture);
                p_lpf_resonance = float.Parse(parts[20], culture);
                p_hpf_freq = float.Parse(parts[21], culture);
                p_hpf_ramp = float.Parse(parts[22], culture);
                if (parts.Length > 23)
                    sound_vol = float.Parse(parts[23], culture);
            }
            catch { }
        }

        // Preset generators
        public void GenerateCoin()
        {
            ResetParams();
            wave_type = 2; // Sine
            p_base_freq = 0.4f + 0.4f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.1f + 0.1f * (float)random.NextDouble();
            p_env_decay = 0.1f + 0.3f * (float)random.NextDouble();
            p_env_punch = 0.3f + 0.3f * (float)random.NextDouble();
            
            // Coin arpeggiator jump
            p_arp_speed = 0.5f + 0.2f * (float)random.NextDouble();
            p_arp_mod = 0.2f + 0.3f * (float)random.NextDouble();
        }

        public void GenerateLaser()
        {
            ResetParams();
            wave_type = random.Next(3); // 0=Square, 1=Saw, 2=Sine
            if (wave_type == 2 && random.Next(2) == 0) wave_type = 0; // Prefer square
            
            p_base_freq = 0.5f + 0.4f * (float)random.NextDouble();
            p_freq_limit = 0.0f + 0.1f * (float)random.NextDouble();
            p_freq_ramp = -0.3f - 0.3f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.1f + 0.15f * (float)random.NextDouble();
            p_env_decay = 0.1f + 0.2f * (float)random.NextDouble();
            p_duty = 0.1f + 0.4f * (float)random.NextDouble();
            p_duty_ramp = -0.05f - 0.1f * (float)random.NextDouble();
        }

        public void GenerateExplosion()
        {
            ResetParams();
            wave_type = 3; // Noise
            if (random.Next(2) == 0)
            {
                p_base_freq = 0.15f + 0.3f * (float)random.NextDouble();
                p_freq_ramp = -0.1f - 0.2f * (float)random.NextDouble();
            }
            else
            {
                p_base_freq = 0.2f + 0.4f * (float)random.NextDouble();
                p_freq_ramp = -0.2f - 0.2f * (float)random.NextDouble();
            }
            p_freq_ramp *= 0.5f;

            p_env_attack = 0.0f;
            p_env_sustain = 0.1f + 0.3f * (float)random.NextDouble();
            p_env_decay = 0.2f + 0.5f * (float)random.NextDouble();

            if (random.Next(2) == 0)
            {
                p_pha_offset = 0.1f + 0.3f * (float)random.NextDouble();
                p_pha_ramp = -0.1f - 0.2f * (float)random.NextDouble();
            }
            
            if (random.Next(3) == 0)
            {
                p_repeat_speed = 0.3f + 0.5f * (float)random.NextDouble();
            }
        }

        public void GeneratePowerup()
        {
            ResetParams();
            wave_type = random.Next(2) == 0 ? 0 : 2; // Square or Sine
            p_base_freq = 0.2f + 0.3f * (float)random.NextDouble();
            p_freq_ramp = 0.2f + 0.3f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.2f + 0.3f * (float)random.NextDouble();
            p_env_decay = 0.3f + 0.3f * (float)random.NextDouble();

            if (random.Next(2) == 0)
            {
                p_vib_strength = 0.1f + 0.4f * (float)random.NextDouble();
                p_vib_speed = 0.2f + 0.4f * (float)random.NextDouble();
            }
        }

        public void GenerateHitHurt()
        {
            ResetParams();
            wave_type = random.Next(3) == 0 ? 0 : 3; // Square or Noise
            p_base_freq = 0.2f + 0.5f * (float)random.NextDouble();
            p_freq_ramp = -0.2f - 0.3f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.0f + 0.1f * (float)random.NextDouble();
            p_env_decay = 0.1f + 0.2f * (float)random.NextDouble();
            
            p_lpf_freq = 0.5f + 0.4f * (float)random.NextDouble();
        }

        public void GenerateJump()
        {
            ResetParams();
            wave_type = 0; // Square
            p_base_freq = 0.3f + 0.3f * (float)random.NextDouble();
            p_freq_ramp = 0.2f + 0.3f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.1f + 0.2f * (float)random.NextDouble();
            p_env_decay = 0.1f + 0.3f * (float)random.NextDouble();
            p_duty = 0.1f + 0.3f * (float)random.NextDouble();
        }

        public void GenerateBlipSelect()
        {
            ResetParams();
            wave_type = random.Next(2); // Square or Saw
            p_base_freq = 0.2f + 0.4f * (float)random.NextDouble();
            p_env_attack = 0.0f;
            p_env_sustain = 0.1f;
            p_env_decay = 0.05f + 0.15f * (float)random.NextDouble();
            p_lpf_freq = 0.6f + 0.3f * (float)random.NextDouble();
        }

        public void GenerateRandom()
        {
            ResetParams();
            wave_type = random.Next(5);
            p_base_freq = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) + 0.5f;
            if (p_base_freq < 0.05f) p_base_freq = 0.05f;
            p_freq_limit = 0.0f;
            p_freq_ramp = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 5.0);
            
            p_env_attack = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) > 0 ? (float)random.NextDouble() * 0.5f : 0.0f;
            p_env_sustain = (float)random.NextDouble() * 0.4f + 0.1f;
            p_env_punch = (float)random.NextDouble() * 0.3f;
            p_env_decay = (float)random.NextDouble() * 0.5f + 0.1f;

            p_vib_strength = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) > 0 ? (float)random.NextDouble() : 0.0f;
            p_vib_speed = (float)random.NextDouble();

            p_arp_speed = (float)random.NextDouble();
            p_arp_mod = (float)random.NextDouble() * 2.0f - 1.0f;

            p_duty = (float)random.NextDouble();
            p_duty_ramp = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0);

            p_repeat_speed = (float)random.NextDouble() * 0.8f;

            p_pha_offset = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) > 0 ? (float)random.NextDouble() : 0.0f;
            p_pha_ramp = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0);

            p_lpf_freq = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) > 0 ? (float)random.NextDouble() : 1.0f;
            p_lpf_ramp = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0);
            p_lpf_resonance = (float)random.NextDouble();

            p_hpf_freq = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0) > 0 ? (float)random.NextDouble() : 0.0f;
            p_hpf_ramp = (float)Math.Pow(random.NextDouble() * 2.0 - 1.0, 3.0);
        }

        public void Mutate()
        {
            if (random.Next(2) == 0) p_base_freq += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_freq_limit += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_freq_ramp += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_freq_dramp += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_env_attack += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_env_sustain += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_env_punch += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_env_decay += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_vib_strength += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_vib_speed += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_arp_speed += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_arp_mod += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_duty += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_duty_ramp += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_repeat_speed += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_pha_offset += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_pha_ramp += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_lpf_freq += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_lpf_ramp += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_lpf_resonance += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_hpf_freq += (float)(random.NextDouble() * 0.1 - 0.05);
            if (random.Next(2) == 0) p_hpf_ramp += (float)(random.NextDouble() * 0.1 - 0.05);

            // Clamp all mutated parameters to valid bounds
            p_base_freq = Math.Max(0.01f, Math.Min(1.0f, p_base_freq));
            p_freq_limit = Math.Max(0.0f, Math.Min(1.0f, p_freq_limit));
            p_freq_ramp = Math.Max(-1.0f, Math.Min(1.0f, p_freq_ramp));
            p_freq_dramp = Math.Max(-1.0f, Math.Min(1.0f, p_freq_dramp));
            p_env_attack = Math.Max(0.0f, Math.Min(1.0f, p_env_attack));
            p_env_sustain = Math.Max(0.0f, Math.Min(1.0f, p_env_sustain));
            p_env_punch = Math.Max(0.0f, Math.Min(1.0f, p_env_punch));
            p_env_decay = Math.Max(0.0f, Math.Min(1.0f, p_env_decay));
            p_vib_strength = Math.Max(0.0f, Math.Min(1.0f, p_vib_strength));
            p_vib_speed = Math.Max(0.0f, Math.Min(1.0f, p_vib_speed));
            p_arp_speed = Math.Max(0.0f, Math.Min(1.0f, p_arp_speed));
            p_arp_mod = Math.Max(-1.0f, Math.Min(1.0f, p_arp_mod));
            p_duty = Math.Max(0.0f, Math.Min(1.0f, p_duty));
            p_duty_ramp = Math.Max(-1.0f, Math.Min(1.0f, p_duty_ramp));
            p_repeat_speed = Math.Max(0.0f, Math.Min(1.0f, p_repeat_speed));
            p_pha_offset = Math.Max(-1.0f, Math.Min(1.0f, p_pha_offset));
            p_pha_ramp = Math.Max(-1.0f, Math.Min(1.0f, p_pha_ramp));
            p_lpf_freq = Math.Max(0.0f, Math.Min(1.0f, p_lpf_freq));
            p_lpf_ramp = Math.Max(-1.0f, Math.Min(1.0f, p_lpf_ramp));
            p_lpf_resonance = Math.Max(0.0f, Math.Min(1.0f, p_lpf_resonance));
            p_hpf_freq = Math.Max(0.0f, Math.Min(1.0f, p_hpf_freq));
            p_hpf_ramp = Math.Max(-1.0f, Math.Min(1.0f, p_hpf_ramp));
        }

        // Method to generate a WAV byte array
        public byte[] GenerateWav()
        {
            List<float> samples = SynthesizeSamples();
            int sampleCount = samples.Count;
            int sampleRate = 44100;
            int bytesPerSample = 2; // 16-bit
            int numChannels = 1; // Mono
            int subchunk2Size = sampleCount * numChannels * bytesPerSample;
            int chunkSize = 36 + subchunk2Size;
            
            byte[] wavBytes = new byte[44 + subchunk2Size];
            
            // RIFF Header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavBytes, 0);
            BitConverter.GetBytes(chunkSize).CopyTo(wavBytes, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavBytes, 8);
            
            // fmt Subchunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavBytes, 12);
            BitConverter.GetBytes(16).CopyTo(wavBytes, 16); // Subchunk1Size
            BitConverter.GetBytes((short)1).CopyTo(wavBytes, 20); // PCM
            BitConverter.GetBytes((short)numChannels).CopyTo(wavBytes, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(wavBytes, 24);
            BitConverter.GetBytes(sampleRate * numChannels * bytesPerSample).CopyTo(wavBytes, 28);
            BitConverter.GetBytes((short)(numChannels * bytesPerSample)).CopyTo(wavBytes, 32);
            BitConverter.GetBytes((short)(bytesPerSample * 8)).CopyTo(wavBytes, 34);
            
            // data Subchunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavBytes, 36);
            BitConverter.GetBytes(subchunk2Size).CopyTo(wavBytes, 40);
            
            int writePos = 44;
            for (int i = 0; i < sampleCount; i++)
            {
                float sample = samples[i];
                if (sample > 1.0f) sample = 1.0f;
                else if (sample < -1.0f) sample = -1.0f;
                
                short pcmSample = (short)(sample * 32767f);
                byte[] temp = BitConverter.GetBytes(pcmSample);
                wavBytes[writePos] = temp[0];
                wavBytes[writePos + 1] = temp[1];
                writePos += 2;
            }
            
            return wavBytes;
        }

        public List<float> SynthesizeSamples()
        {
            var samples = new List<float>();

            // Setup state variables
            double fperiod = 100.0 / (p_base_freq * p_base_freq + 0.001);
            double period = fperiod;
            double maxperiod = 100.0 / (p_freq_limit * p_freq_limit + 0.001);
            
            // Linear scales
            double slide = 1.0 - Math.Pow(p_freq_ramp, 3.0) * 0.01;
            double dslide = -Math.Pow(p_freq_dramp, 3.0) * 0.000001;
            
            double square_duty = 0.5f - p_duty * 0.5f;
            double square_slide = -p_duty_ramp * 0.00005;

            double arp_limit = 0;
            double arp_mod = 0;
            if (p_arp_mod >= 0.0f)
                arp_mod = 1.0 - Math.Pow(p_arp_mod, 2.0) * 0.9;
            else
                arp_mod = 1.0 + Math.Pow(p_arp_mod, 2.0) * 10.0;
            
            if (p_arp_speed < 1.0f)
                arp_limit = (int)(Math.Pow(1.0f - p_arp_speed, 2.0) * 20000 + 32);

            // Filter parameters
            double fltp = 0;
            double fltdp = 0;
            double flt_lowpass = Math.Pow(p_lpf_freq, 3.0) * 0.1;
            double flt_lowpass_sweep = 1.0 + p_lpf_ramp * 0.0001;
            double flt_lowpass_resonance = 1.0 - p_lpf_resonance * 0.9;
            double flt_highpass = Math.Pow(p_hpf_freq, 2.0) * 0.1;
            double flt_highpass_sweep = 1.0 + p_hpf_ramp * 0.0003;

            // Phaser/Flanger parameters
            int phaser_offset = (int)(Math.Pow(p_pha_offset, 2.0) * 1020);
            if (p_pha_offset < 0.0f) phaser_offset = -(int)(Math.Pow(p_pha_offset, 2.0) * 1020);
            double phaser_ramp = p_pha_ramp * 0.00001;
            double phaser_phase = 0;
            float[] phaser_buffer = new float[1024];

            // Envelope parameters
            int env_stage = 0;
            int env_time = 0;
            int env_length_attack = (int)(p_env_attack * p_env_attack * 100000);
            int env_length_sustain = (int)(p_env_sustain * p_env_sustain * 100000);
            int env_length_decay = (int)(p_env_decay * p_env_decay * 100000);
            
            // Prevent division by zero
            if (env_length_attack < 1) env_length_attack = 1;
            if (env_length_sustain < 1) env_length_sustain = 1;
            if (env_length_decay < 1) env_length_decay = 1;
            
            double env_vol = 0;

            // Repeat parameters
            int repeat_limit = 0;
            int repeat_time = 0;
            if (p_repeat_speed > 0.0f)
                repeat_limit = (int)(Math.Pow(1.0f - p_repeat_speed, 2.0) * 20000 + 32);

            // Oscillation variables
            int iphase = 0;
            double phase = 0;
            double fphase = 0;
            double filter_p = 0;
            double filter_dp = 0;
            double filter_h = 0;

            // White noise setup
            float[] noise_samples = new float[32];
            for (int i = 0; i < 32; i++)
                noise_samples[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            bool finished = false;
            int sampleCount = 0;

            // Synthesis loop (generate up to 3 seconds of sound maximum)
            while (sampleCount < 44100 * 3 && !finished)
            {
                sampleCount++;

                // Repeat logic
                if (repeat_limit > 0 && ++repeat_time >= repeat_limit)
                {
                    repeat_time = 0;
                    fperiod = 100.0 / (p_base_freq * p_base_freq + 0.001);
                    period = fperiod;
                    slide = 1.0 - Math.Pow(p_freq_ramp, 3.0) * 0.01;
                    dslide = -Math.Pow(p_freq_dramp, 3.0) * 0.000001;
                }

                // Arpeggiator (pitch jump) logic
                if (arp_limit > 0 && sampleCount >= arp_limit)
                {
                    arp_limit = 0;
                    period *= arp_mod;
                }

                // Frequency slide/acceleration
                slide += dslide;
                period *= slide;
                if (period > maxperiod)
                {
                    period = maxperiod;
                    if (p_freq_limit > 0.0f)
                        finished = true;
                }

                // Vibrato
                double vib_phase = sampleCount * (Math.Pow(p_vib_speed, 2.0) * 0.01);
                double rfperiod = period * (1.0 + Math.Sin(vib_phase) * Math.Pow(p_vib_strength, 2.0) * 0.5);

                int iperiod = (int)rfperiod;
                if (iperiod < 8) iperiod = 8;

                // Duty cycle sweep for square wave
                square_duty += square_slide;
                if (square_duty < 0.0) square_duty = 0.0;
                if (square_duty > 0.5) square_duty = 0.5;

                // Volume envelope stages
                env_time++;
                if (env_stage == 0 && env_time >= env_length_attack)
                {
                    env_stage = 1;
                    env_time = 0;
                }
                else if (env_stage == 1 && env_time >= env_length_sustain)
                {
                    env_stage = 2;
                    env_time = 0;
                }
                else if (env_stage == 2 && env_time >= env_length_decay)
                {
                    finished = true;
                }

                if (env_stage == 0)
                {
                    env_vol = (double)env_time / env_length_attack;
                }
                else if (env_stage == 1)
                {
                    env_vol = 1.0 + (1.0 - (double)env_time / env_length_sustain) * 2.0 * p_env_punch;
                }
                else if (env_stage == 2)
                {
                    env_vol = 1.0 - (double)env_time / env_length_decay;
                }

                // Synthesize base oscillator sample
                double sample = 0;
                
                fphase += 1.0 / rfperiod;
                while (fphase >= 1.0)
                {
                    fphase -= 1.0;
                    if (wave_type == 3) // Noise: regenerate noise samples
                    {
                        for (int i = 0; i < 32; i++)
                            noise_samples[i] = (float)(random.NextDouble() * 2.0 - 1.0);
                    }
                }

                switch (wave_type)
                {
                    case 0: // Square
                        sample = fphase < square_duty ? -0.5 : 0.5;
                        break;
                    case 1: // Sawtooth
                        sample = 1.0 - fphase * 2.0;
                        break;
                    case 2: // Sine
                        sample = Math.Sin(fphase * 2.0 * Math.PI);
                        break;
                    case 3: // Noise
                        sample = noise_samples[(int)(fphase * 32.0)];
                        break;
                    case 4: // Triangle
                        sample = fphase < 0.5 ? (fphase * 4.0 - 1.0) : (3.0 - fphase * 4.0);
                        break;
                }

                // Low-pass/High-pass filter
                double filter_h_prev = filter_h;
                flt_lowpass *= flt_lowpass_sweep;
                if (flt_lowpass < 0.0) flt_lowpass = 0.0;
                if (flt_lowpass > 0.1) flt_lowpass = 0.1;

                if (p_lpf_freq < 1.0f)
                {
                    filter_dp += (sample - filter_p) * flt_lowpass;
                    filter_dp *= flt_lowpass_resonance;
                }
                else
                {
                    filter_p = sample;
                    filter_dp = 0;
                }
                filter_p += filter_dp;

                flt_highpass *= flt_highpass_sweep;
                if (flt_highpass < 0.0) flt_highpass = 0.0;
                if (flt_highpass > 0.1) flt_highpass = 0.1;

                filter_h += filter_p - filter_h_prev;
                filter_h *= (1.0 - flt_highpass);
                double filteredSample = filter_h;

                // Phaser/Flanger effect
                phaser_offset += (int)(phaser_ramp * sampleCount);
                if (phaser_offset < 0) phaser_offset = 0;
                if (phaser_offset > 1023) phaser_offset = 1023;

                phaser_buffer[iphase & 1023] = (float)filteredSample;
                double phaserSample = filteredSample + phaser_buffer[(iphase - phaser_offset + 1024) & 1023] * 0.5;
                iphase++;

                // Apply envelope and master volume
                double finalSample = phaserSample * env_vol * sound_vol;
                samples.Add((float)finalSample);
            }

            return samples;
        }
    }
}
