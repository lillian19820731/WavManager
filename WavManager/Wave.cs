using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SASR
{
    public class Wave
    {
        public float Frequency = 0.0f;
        public float Offset = 0.0f;
        public float Duration = 0.0f;
        public float Amplitude = 0.0f;
        public float Phase = 0.0f;
        public float Ending => Offset + Duration;


        public Wave(float offset, float duration, float frequency, float amplitude)
        {
            this.Frequency = frequency;
            this.Offset = offset;
            this.Duration = duration;
            this.Amplitude = amplitude;
        }

    }


    public class Channel
    {
        public List<Wave> Waves = new List<Wave>();
        public int ChannelID = -1;
        public float LatestEnd = 0.0f;
        public float Cursor = 0.0f;

        public Channel(int channelID)
        {
            this.ChannelID = channelID;
        }
        public void AddWave(float offset, float duration, float frequency, float amplitude, float timeFrame)
        {
            if (this.LatestEnd < offset)
            {
                this.Waves.Add(new Wave(this.LatestEnd, offset - this.LatestEnd, 0, 0));
            }
            if((offset + duration >timeFrame) && (offset < timeFrame))
            {
                this.Waves.Add(new Wave(offset, timeFrame - offset, frequency, amplitude));
                this.Waves.Add(new Wave(0, duration - timeFrame + offset, frequency, amplitude));
            }
            else if (offset >=timeFrame)
            {
                this.Waves.Add(new Wave(offset - timeFrame, duration, frequency, amplitude));
            }
            else
            {
                this.Waves.Add(new Wave(offset, duration, frequency, amplitude));
            }
            this.LatestEnd = offset + duration;
        }

        public void FormatChannel(float timeFrame)
        {
            if(this.LatestEnd < timeFrame)
            {
                this.Waves.Add(new Wave(LatestEnd, timeFrame - LatestEnd, 0, 0));
            }
        }

        public void ResetChannel(float timeFrame)
        {
            if(this.Waves.Count > 0)
            {
                Wave w = Waves[0];
                if(w.Offset + w.Duration > timeFrame )
                {
                    this.Waves.RemoveAt(0);
                    Wave n = new Wave(w.Offset, timeFrame - w.Offset, w.Frequency, w.Amplitude);
                    n.Phase = w.Phase;
                    this.Waves.Add(n);
                }
                this.Waves.Add(new Wave(0,w.Duration - timeFrame + w.Offset,w.Offset,w.Amplitude));
            }
        }
    }

    public class ChannelManager
    {
        public void filter(List<Channel> channels)
        {
            foreach(Channel  c  in channels)
            {
                if (c.Waves.Count ==0)
                {
                    channels.Remove(c);
                }
            }
        }

        public void ResetChannels(List<Channel> channels,float timeFrame)
        {
            foreach (Channel c in channels)
            {
                c.ResetChannel(timeFrame);
            }
        }

        public void  FormatChannels(List<Channel> channels, float timeFrame)
        {
            foreach (Channel c in channels)
            {
                c.FormatChannel(timeFrame);
            }
        }
    }

    public class DataGenerator
    {
        public int FS = 44100;
        public short MaxAmplidute = short.MaxValue;
        public List<short> outData = new List<short>();
        public float GetYFrom(float t,Wave myWave,float sampleRate)
        {
            float y = 0.0f;
            float u = 0.0f;
            if(myWave.Frequency != 0)
            {
                u = sampleRate / myWave.Frequency;
                myWave.Phase += (float)((2* Math.PI)/u);
                y = (float)(myWave.Amplitude * Math.Sin((double)(myWave.Phase)));
            }
            return y;
        }

        protected void GenerateWaveData(List<Channel> channels,float timeFrame,float sampleRate,List<short> data)
        {
            for(int t = 0;t< timeFrame * FS; t++)
            {
                float y = 0;
                Wave w = null;
                List<int> cns = new List<int>();
                foreach (Channel c in channels)
                {
                    float p = 0;
                    if (c.Waves.Count>0)
                    {
                        w = c.Waves[0];
                        if((t+1)/FS - w.Offset>=w.Duration)
                        {
                            if(w.Frequency != 0)
                            {
                                p = w.Phase;
                            }
                            c.Waves.RemoveAt(0);
                            if(c.Waves.Count > 0)
                            {
                                w = c.Waves[0];
                                w.Phase = p;
                            }
                            else
                            {
                                cns.Add(c.ChannelID);
                            }
                        }
                    }
                    if(w != null)
                    {
                        y += this.GetYFrom(t - w.Offset * FS, w, sampleRate);
                    }

                }
                data.Add((short)(y * MaxAmplidute));
                cns.ForEach(cn => channels.RemoveAll(c => c.ChannelID == cn));
            }
        }

        public List<short> ParseFile(string path,float timeFrame)
        {
            string s = string.Empty;
            List<Channel> channels = new List<Channel>();
            List<int> cns = new List<int>();
            DataGenerator dg = new DataGenerator();
            using (StreamReader sr = new StreamReader(path))
            {
                while ((s = sr.ReadLine()) != null)
                {
                    if (!s.StartsWith("#"))
                    {
                        string[] ss = s.Split(' ');
                        if (ss.Length == 5)
                        {
                            if (FloatParse(ss[1]) > timeFrame)
                            {
                                foreach (Channel c in channels)
                                {
                                    c.FormatChannel(timeFrame);
                                }
                                dg.GenerateWaveData(channels, timeFrame, FS, outData);
                            }
                        }
                        int cn = IntParse(ss[0]);
                        if (cn == -1)
                        {
                            foreach (Channel c in channels)
                            {
                                c.AddWave(0.0f, FloatParse(ss[2]), 0.0f, 0.0f, timeFrame);
                            }
                        }
                        else if (cn >= 0)
                        {
                            if (!cns.Contains(cn))
                            {
                                cns.Add(cn);
                                channels.Add(new Channel(cn));
                            }
                            foreach (Channel c in channels)
                            {
                                if (c.ChannelID == cn)
                                {
                                    c.AddWave(FloatParse(ss[1]), FloatParse(ss[2]), FloatParse(ss[3]), FloatParse(ss[4]) * MaxAmplidute, timeFrame);
                                }
                            }
                        }
                    }
                }

                while (channels.Count>0)
                {
                    dg.GenerateWaveData(channels, timeFrame, FS, outData);
                }
            }
            return outData;
        }

        public void WriteData(string path, List<short> outData)
        {
            using (WAVFile WAVFile = new WAVFile())
            {
                WAVFile.Create(path, false, FS, 16);
                WAVFile.AppendSamples(outData);
            }
        }

        float FloatParse(string s) => float.TryParse(s, out var f) ? f : 0.0f;
        int IntParse(string s) => int.TryParse(s, out var f) ? f : -1;
        public Dictionary<int,float> GenerateTones(float baseTone,int count)
        {
            Dictionary<int, float> tones = new Dictionary<int, float>();
            float t = 0.0f;
            for(int i=1;i <= count;i++)
            {
                if(i%3 == 0)
                {
                    if(i != 1)
                    {
                        t *= 9 / 8;
                    }
                }
                else if(i%3 ==1)
                {
                    t *= 10 / 9;
                }
                else
                {
                    t *= 16 / 15;
                }
                tones.Add(i, t);
            }
            return tones;
        }
    }


}
