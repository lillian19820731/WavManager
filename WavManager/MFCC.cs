using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SASR
{
    public class MelBankInfo
    {
        public float[,] filter;
        public int nfilters;
        public int nfft;
        public int low;
        public int high;
    }

    public class DctInfo
    {
        public float[,] coeff;
        public int dctlen;
    }

    public class MFCC
    {

        #region Fields
        public MelBankInfo MBI = new MelBankInfo();

        public float[] FilterWindow = null;
        public float[] LifterWindow = null;
        public float PreEmphasiseFactor = DSPTools.DefaultPreEmphasiseFactor;
        public DctInfo dct = new DctInfo();
        #endregion
        #region Methods
        public MFCC(int sample_rate=8000,int frame_length=512, int nfft=512, int low=0, int high=4000, int nfilters = 24, int ndcts=12)
        {
            this.MBI.nfft = nfft;
            this.MBI.low = low;
            this.MBI.high = high;
            this.MBI.nfilters = nfilters;
            this.dct.dctlen = ndcts;
            int valid_nfft = nfft / 2 + 1;

            DSPTools.DctCoeff(ndcts, nfilters,this.dct.coeff= new float[ndcts,nfilters]);

            DSPTools.MelBank(sample_rate, nfft, low, high, nfilters, this.MBI.filter = new float[nfilters, valid_nfft]);//Mel滤波器系数	   

            DSPTools.CreateHammingWindow(this.FilterWindow = new float[frame_length]);//加窗

            DSPTools.CreateLiftWindow(this.LifterWindow = new float[this.dct.dctlen]);
        }
        public float[] ProcessFrame(float[] frame)
        {
            DSPTools.PreEmphasise(frame, this.PreEmphasiseFactor);

            DSPTools.ApplyWindow(frame, this.FilterWindow);

            int valid_nfft = this.MBI.nfft / 2 + 1;
            
            Complex[] Outputs = DSPTools.FFT(frame.Select(D => new Complex(D, 0.0f)).ToArray(), valid_nfft);

            float[] Internals = Outputs.Select(O => (float)O.Magnitude).ToArray();

            float[] Results = new float[this.dct.dctlen];

            for (int i = 0; i < this.dct.dctlen; i++)
            {
                double t = 0.0f;
                for (int j = 0; j < this.MBI.nfilters; j++)
                {
                    //DCT变换，解卷积
                    t += this.dct.coeff[i, j] * Math.Log(
                        DSPTools.Product(Internals, this.MBI.filter, j, valid_nfft) +
                        DSPTools.EPS,
                        DSPTools.NewBase);
                }
                Results[i] = (float)(t* this.LifterWindow[i]);//倒谱提升  
            }
            return Internals;
        }

        public struct Formant
        {
            public int TrackIndex;

            public float MelFrequency;
            public float MelBandwidth;
            public float Frequency => DSPTools.MelToHz(this.MelFrequency);
            public float Bandwidth => DSPTools.MelToHz(this.MelBandwidth);
        }
        public Formant[] GetMFCCFormants(float[] mfcc, int maxCount)
        {
            //TODO: details
            List<Formant> formants = new List<Formant>();

            float dx = 0.1f;
            float xmax = 1f;

            for (int i = 1; i < mfcc.Length -1; i++)
            {
                //sharp top
                if (mfcc[i] > mfcc[i - 1] && mfcc[i] >= mfcc[i + 1])
                {
                    Formant formant = new Formant();

                    formant.TrackIndex = i;

                    float firstDerivative = mfcc[i + 1] - mfcc[i - 1];
                    float secondDerivative = 2 * mfcc[i] - (mfcc[i - 1] + mfcc[i + 1]);

                    formant.MelFrequency = dx * (i - 1.0f + 0.5f * firstDerivative / secondDerivative);

                    float min3dB = 0.5f * (mfcc[i] + 0.125f* firstDerivative * firstDerivative / secondDerivative);
                    
                    /* Search left. */
                    int j = i - 1;

                    while (mfcc[j] > min3dB && j > 1) j--;
                    
                    formant.MelBandwidth = mfcc[j] > min3dB ? formant.MelFrequency : formant.MelFrequency - dx * (j - 1 + (min3dB - mfcc[j]) / (mfcc[j + 1] - mfcc[j]));
                    
                    /* Search right. */
                    j = i + 1;

                    while (mfcc[j] > min3dB && j < mfcc.Length) j++;
                    
                    formant.MelBandwidth += mfcc[j] > min3dB ? xmax - formant.MelFrequency : dx * (j - 1 - (min3dB - mfcc[j]) / (mfcc[j - 1] - mfcc[j])) - formant.MelFrequency;

                    formants.Add(formant);

                    if (formants.Count == maxCount) break;
                }
            }
            return formants.ToArray();
        }

        public class FormantLine
        {
            public List<Formant> Formants { get; } = new List<Formant>();


        }
        /// <summary>
        /// 计算共振峰线：在共振峰帧的序列基础上合成共振峰线
        /// 共振峰线以很好的控制共振峰的形成过程
        /// 由此多个共振峰可以并行生成
        /// </summary>
        /// <param name="FormantFrames"></param>
        /// <returns></returns>
        public List<FormantLine> TrackFormants(List<Formant[]> FormantFrames)
        {
            List<FormantLine> lines = new List<FormantLine>();
            for(int i = 0; i < FormantFrames.Count; i++)
            {
                //TODO:build formant line with slices
            }

            return lines;
        }


        #endregion
    }
}
