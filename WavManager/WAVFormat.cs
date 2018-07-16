namespace SASR
{
    // This struct contains audio format information and is used
    // by the WAVFile class.
    public struct WAVFormat
    {
        public byte NumChannels { get; set; }

        public bool IsStereo => (NumChannels == 2);

        public int SampleRateHz { get; set; }

        public short BitsPerSample { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="NumChannels">The number of channels</param>
        /// <param name="SampleRateHz">The sample rate (Hz)</param>
        /// <param name="BitsPerSample">The number of bits per sample</param>
        public WAVFormat(byte NumChannels, int SampleRateHz, short BitsPerSample)
        {
            this.NumChannels = NumChannels;
            this.SampleRateHz = SampleRateHz;
            this.BitsPerSample = BitsPerSample;
        }

        public static bool operator ==(WAVFormat f1, WAVFormat f2)
            => ((f1.NumChannels == f2.NumChannels) &&
                   (f1.SampleRateHz == f2.SampleRateHz) &&
                   (f1.BitsPerSample == f2.BitsPerSample));

        public static bool operator !=(WAVFormat f1, WAVFormat f2)
            => ((f1.NumChannels != f2.NumChannels) ||
                   (f1.SampleRateHz != f2.SampleRateHz) ||
                   (f1.BitsPerSample != f2.BitsPerSample));

        public override bool Equals(object obj) => (obj is WAVFormat format) && (this == format);

        public override int GetHashCode() => base.GetHashCode() ^ this.NumChannels ^ this.SampleRateHz ^ this.BitsPerSample;
    }
}