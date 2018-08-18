using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SASR
{
    public static class DSPTools
    {
        #region Consts
        public const double EPS = 0.0000001;
        public const double NewBase = 10.0;
        public const float DefaultPreEmphasiseFactor = 0.9375f;
        #endregion

        #region Window
        public static void CreateLiftWindow(float[] data)//倒谱提升窗归一化。
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int m = data.Length;
            float max_value = 0.0f;
            for (int i = 1; i <= data.Length; i++)
            {
                data[i - 1] = (float)(1.0 + 0.5 * m * Math.Sin(Math.PI * i / m));
                if (data[i - 1] > max_value)
                {
                    max_value = data[i - 1];
                }
            }
            for (int i = 1; i <= m; i++)
            {
                data[i - 1] /= max_value;
            }
        }
        public static void CreateHanningWindow(float[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int m = data.Length;
            int half = 0;
            if (m % 2 == 0)
            {
                half = m / 2;

                for (int i = 1; i <= half; ++i)
                {
                    data[i - 1] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * i / (m + 1.0)));
                }

                int index = half + 1;
                for (int i = half; i >= 1; i--)
                {
                    data[index - 1] = data[i - 1];
                    index++;
                }

            }
            else
            {
                half = (m + 1) / 2;

                for (int i = 1; i <= half; ++i)
                {
                    data[i - 1] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * i / (m + 1.0)));
                }

                int index = half + 1;
                for (int i = half - 1; i >= 1; i--)
                {
                    data[index - 1] = data[i - 1];
                    index++;
                }

            }
        }
        public static void CreateHammingWindow(float[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int m = data.Length;
            int half = 0;
            if (m % 2 == 0)
            {
                half = m / 2;

                for (int i = 1; i <= half; ++i)
                {
                    data[i - 1] = (float)(0.54 - 0.46 * Math.Cos(Math.PI * 2.0 * i / (m + 1.0)));
                }

                int index = half + 1;
                for (int i = half; i >= 1; i--)
                {
                    data[index - 1] = data[i - 1];
                    index++;
                }

            }
            else
            {
                half = (m + 1) / 2;

                for (int i = 1; i <= half; ++i)
                {
                    data[i - 1] = (float)(0.54 - 0.46 * Math.Cos(Math.PI * 2.0 * i / (m + 1.0)));
                }

                int index = half + 1;
                for (int i = half - 1; i >= 1; i--)
                {
                    data[index - 1] = data[i - 1];
                    index++;
                }

            }
        }
        public static void ApplyWindow(float[] data, float[] window)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (data.Length != window.Length) throw new ArgumentOutOfRangeException(nameof(data));

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = data[i] * window[i];
            }
        }
        #endregion
        #region Tools
        public static void PreEmphasise(float[] data, float factor)//预加重
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            for (int i = data.Length - 1; i <= 1; i--)
            {
                data[i] = data[i] - factor * data[i - 1];
            }
            //data[0] = data[0];
        }

        public static float HzToMel(float f)
        {
            return (float)(1127 * Math.Log(1.0 + f / 700));
        }
        public static float MelToHz(float data)
        {
            return (float)(700 * (Math.Exp(data / 1127) - 1));
        }
        public static int HzToN(float f, int fs, int nfft)
        {
            return (int)(f / fs * nfft + 1);
        }

        public static void DctCoeff(int m, int n, float[,] coeff)//标准DCT变换。
        {
            for (int i = 1; i <= m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    coeff[i - 1, j] = (float)Math.Cos((2 * j + 1) * i * Math.PI / (2 * n));
                }
            }
        }

        public static float Product(float[] data1, float[] data2, int len)
        {
            float result = 0.0f;
            for (int i = 0; i < len; i++)
            {
                result += data1[i] * data2[i];
            }
            return result;
        }
        public static float Product(float[] data1, float[,] data2, int j, int len)
        {
            float result = 0.0f;
            for (int i = 0; i < len; i++)
            {
                result += data1[i] * data2[j, i];
            }
            return result;
        }
        public static void MelBank(int fs, int nfft, int low, int high, int nfilters, float[,] coeff)//三角滤波器组。
        {
            float fre_bin = (float)fs / nfft;
            float low_mel = HzToMel(low);
            float high_mel = HzToMel(high);
            float mel_bw = (high_mel - low_mel) / (nfilters + 1);
            int valid_nfft = nfft / 2 + 1;

            for (int j = 1; j <= nfilters; j++)
            {
                float mel_cent = j * mel_bw + low_mel;
                float mel_left = mel_cent - mel_bw;
                float mel_right = mel_cent + mel_bw;
                float freq_cent = MelToHz(mel_cent);
                float freq_left = MelToHz(mel_left);
                float freq_bw_left = freq_cent - freq_left;
                float freq_right = MelToHz(mel_right);
                float freq_bw_right = freq_right - freq_cent;
                for (int i = 1; i <= valid_nfft; i++)
                {
                    float freq = (i - 1) * fre_bin;
                    if (freq > freq_left && freq < freq_right)
                    {
                        if (freq <= freq_cent)
                        {
                            coeff[j - 1, i - 1] = (freq - freq_left) / freq_bw_left;
                        }
                        else
                        {
                            coeff[j - 1, i - 1] = (freq_right - freq) / freq_bw_right;
                        }
                    }
                }
            }
        }

        public static Complex[] FFT(Complex[] data, int count = 0)
        {
            if (count <= 1) return null;
            Complex[] vl = new Complex[count];
            Complex[] vr = new Complex[count];
            for (int i = 0; i < count; i += 2)
            {
                vl[i / 2] = data[i];
                vr[i / 2] = data[i + 1];
            }
            FFT(vl, count / 2); FFT(vr,count / 2);
            Complex wn = new Complex(Math.Cos(2 * Math.PI / count),Math.Sin(2 * Math.PI / count));
            Complex w = new Complex(1,0);
            for (int i = 0; i < (count / 2); i++)
            {
                data[i] = vl[i] + w * vr[i];
                data[i + count / 2] = vl[i] - w * vr[i];
                w = w * wn;
            }
            return data;
        }
        /// <summary>  
        /// 一维频率抽取基2快速傅里叶变换  
        /// 频率抽取：输入为自然顺序，输出为码位倒置顺序  
        /// 基2：待变换的序列长度必须为2的整数次幂  
        /// </summary>  
        /// <param name="data">待变换的序列(复数数组)</param>  
        /// <param name="count">序列长度,可以指定[0,sourceData.Length-1]区间内的任意数值,0:数据长度</param>  
        /// <returns>返回变换后的序列（复数数组）</returns>  
        //public static Complex[] FFT(Complex[] data, int count = 0)
        //{
        //    count = count > 0 ? count : data.Length;

        //    //2的r次幂为N，求出r; r能代表fft算法的迭代次数  
        //    int r = Convert.ToInt32(Math.Log(count, 2));

        //    //分别存储蝶形运算过程中左右两列的结果  
        //    Complex[] vl = new Complex[count];
        //    Complex[] vr = new Complex[count];

        //    Array.Copy(data, vl, count);

        //    //w代表旋转因子  
        //    Complex[] w = new Complex[count / 2];
        //    //为旋转因子赋值。（在蝶形运算中使用的旋转因子是已经确定的，提前求出以便调用）  
        //    //旋转因子公式 \  /\  /k __  
        //    //              \/  \/N  --  exp(-j*2πk/N)  
        //    //这里还用到了欧拉公式  
        //    for (int i = 0; i < count / 2; i++)
        //    {
        //        double angle = - i * Math.PI * 2 / count;

        //        w[i] = new Complex(Math.Cos(angle), Math.Sin(angle));
        //    }

        //    //蝶形运算  
        //    for (int i = 0; i < r; i++)
        //    {
        //        //i代表当前的迭代次数，r代表总共的迭代次数.  
        //        //i记录着迭代的重要信息.通过i可以算出当前迭代共有几个分组，每个分组的长度  

        //        //interval记录当前有几个组  
        //        // <<是左移操作符，左移一位相当于*2  
        //        //多使用位运算符可以人为提高算法速率^_^  
        //        int interval = 1 << i;

        //        //halfN记录当前循环每个组的长度N  
        //        int halfN = 1 << (r - i);

        //        //循环，依次对每个组进行蝶形运算  
        //        for (int j = 0; j < interval; j++)
        //        {
        //            //j代表第j个组  

        //            //gap=j*每组长度，代表着当前第j组的首元素的下标索引  
        //            int gap = j * halfN;

        //            //进行蝶形运算  
        //            for (int k = 0; k < halfN / 2; k++)
        //            {
        //                vr[k + gap] = vl[k + gap] + vl[k + gap + halfN / 2];
        //                vr[k + halfN / 2 + gap] = (vl[k + gap] - vl[k + gap + halfN / 2]) * w[k * interval];
        //            }
        //        }

        //        //将结果拷贝到输入端，为下次迭代做好准备  
        //        Array.Copy(vr, vl, vr.Length);
        //    }

        //    //将输出码位倒置  
        //    for (uint j = 0; j < count; j++)
        //    {
        //        //j代表自然顺序的数组元素的下标索引  

        //        //用rev记录j码位倒置后的结果  
        //        uint rev = 0;
        //        //num作为中间变量  
        //        uint num = j;

        //        //码位倒置（通过将j的最右端一位最先放入rev右端，然后左移，然后将j的次右端一位放入rev右端，然后左移...）  
        //        //由于2的r次幂=N，所以任何j可由r位二进制数组表示，循环r次即可  
        //        for (int i = 0; i < r; i++)
        //        {
        //            rev <<= 1;
        //            rev |= num & 1;
        //            num >>= 1;
        //        }
        //        vr[rev] = Format(vl[j]);
        //    }
        //    return vr;

        //}
        /// <summary>  
        /// 一维频率抽取基2快速傅里叶逆变换  
        /// </summary>  
        /// <param name="data">待反变换的序列（复数数组）</param>  
        /// <param name="count">序列长度,可以指定[0,sourceData.Length-1]区间内的任意数值,0:数据长度</param>  
        /// <returns>返回逆变换后的序列（复数数组）</returns>  
        public static Complex[] IFFT(Complex[] data, int count = 0)
        {
            count = count > 0 ? count : data.Length;
            //将待逆变换序列取共轭，再调用正变换得到结果，对结果统一再除以变换序列的长度N  

            for (int i = 0; i < count; i++)
            {
                data[i] = System.Numerics.Complex.Conjugate(data[i]);
            }

            Complex[] interVar = FFT(data, count);

            for (int i = 0; i < count; i++)
            {
                interVar[i] = new Complex(interVar[i].Real / count, -interVar[i].Imaginary / count);
            }

            return interVar;
        }
        public static double Format(double v) => Math.Abs(v) < EPS ? 0 : v;
        public static Complex Format(Complex c) => new Complex(Format(c.Real), Format(c.Imaginary));

        public static Complex W(int k, int n, int N)
        {
            return new Complex(Math.Cos(2 * Math.PI * k * n / N),- Math.Sin(2 * Math.PI * k * n / N));
        }
        public static Complex[] DFT(Complex[] data)
        {
            Complex[] results = new Complex[data.Length];

            for(int k = 0; k < data.Length; ++k)
            {
                Complex t = new Complex();

                for(int n = 0; n < data.Length; ++n)
                {
                    t += data[n] * W(k, n, data.Length);
                }
                results[k] = Format(t);
            }

            return results;
        }
        public static Complex[] IDFT(Complex[] data)
        {
            Complex[] results = new Complex[data.Length];

            for (int n = 0; n < data.Length; ++n)
            {
                Complex t = new Complex();

                for (int k = 0; k < data.Length; ++k)
                {
                    t += data[k] * W(k, -n, data.Length);
                }
                results[n] = Format(t/data.Length);
            }

            return results;
        }
        #endregion
        #region Pitch
        public static float GetPitch(float[] WaveFrame, int SampleRate)
        {
            if (WaveFrame == null || WaveFrame.Length == 0) throw new ArgumentNullException(nameof(WaveFrame));

            List<float> ACF = new List<float>();

            // calculate ACF  
            for (int k = 0; k < WaveFrame.Length; k++)
            {
                float sum = 0.0f;
                for (int i = 0; i < WaveFrame.Length - k; i++)
                {
                    sum += WaveFrame[i] * WaveFrame[i + k];
                }
                ACF.Add(sum);
            }
            
            // find the max one  
            float max = WaveFrame[0];

            int index = 0;

            for (int k = 1; k < WaveFrame.Length; k++)
            {
                if (ACF[k] > max)
                {
                    max = ACF[k];
                    index = k;
                }
            }
            return SampleRate / (float)index;
        }
        #endregion
    }
}
