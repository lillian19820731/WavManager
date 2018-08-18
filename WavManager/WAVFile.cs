using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace SASR
{
    public interface IWaveOuputTarget
    {
        void AppendSamples(IList<short> buffer);
    }
    /// <summary>
    /// WAV file class: Allows manipulation of WAV audio files
    /// </summary>
    public class WAVFile : IWaveOuputTarget,IDisposable
    {
        // Static members
        public const int DataStartPos = 44; // The byte position of the start of the audio data
        public const int HeaderSize = 36;

        /// <summary>
        /// Gets the name of the file that was opened.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Gets the WAV file header as read from the file (array of 4 chars)
        /// </summary>
        public char[] WAVHeader { get; private set; }

        /// <summary>
        /// Gets the WAV file header as read from the file, as a String object
        /// </summary>
        public string WAVHeaderString{get {return  new string(WAVHeader);}}// => new string(WAVHeader);

        /// <summary>
        /// Gets the RIFF type as read from the file (array of chars)
        /// </summary>
        public char[] RIFFType { get; private set; }

        /// <summary>
        /// Gets the RIFF type as read from the file, as a String object
        /// </summary>
        //public string RIFFTypeString => new string(RIFFType);
                public string RIFFTypeString{get{ return new string(RIFFType);}}

        /// <summary>
        /// Gets the audio file's number of channels
        /// </summary>
        public byte NumChannels { get; private set; }

        /// <summary>
        /// Gets whether or not the file is stereo.
        /// </summary>
        //public bool IsStereo => (NumChannels == 2);
        public bool IsStereo{get{return (NumChannels == 2);}}

        /// <summary>
        /// Gets the audio file's sample rate (in Hz)
        /// </summary>
        public int SampleRateHz { get; private set; }

        /// <summary>
        /// Gets the number of bytes per second for the audio file
        /// </summary>
        public int BytesPerSec { get; private set; }

        /// <summary>
        /// Gets the number of bytes per sample for the audio file
        /// </summary>
        public short BytesPerSample { get; private set; }

        /// <summary>
        /// Gets the number of bits per sample for the audio file
        /// </summary>
        public short BitsPerSample { get; private set; }

        /// <summary>
        /// Gets the data size (in bytes) for the audio file.  This is read from a field
        /// in the WAV file header.
        /// </summary>
        public int DataSizeBytes { get; private set; }

        /// <summary>
        /// Gets the file size (in bytes).  This is read from a field in the WAV file header.
        /// </summary>
        public long FileSizeBytes{get{return Stream.Length;}}

        /// <summary>
        /// Gets the number of audio samples in the WAV file.  This is calculated based on
        /// the data size read from the file and the number of bits per sample.
        /// </summary>
        public int NumSamples{get{return  (DataSizeBytes / (int)(BitsPerSample / 8));}} 

        /// <summary>
        /// Gets the number of samples remaining (when in read mode).
        /// </summary>
        public int NumSamplesRemaining { get; private set; }

        /// <summary>
        /// Gets the mode of the open file (read, write, or read/write)
        /// </summary>
        public WAVFileMode FileOpenMode { get; private set; }

        // Property that uses 
        /// <summary>
        /// Gets a WAVFormat object containing the audio format information
        /// for the file (# channels, sample rate, and bits per sample).
        /// </summary>
        public WAVFormat AudioFormat => (new WAVFormat(NumChannels, SampleRateHz, BitsPerSample));

        /// <summary>
        /// Gets the current file byte position.
        /// </summary>
        public long FilePosition => (Stream != null ? Stream.Position : 0);

        public FileStream Stream { get; set; }

        /// <summary>
        /// This enumeration specifies file modes supported by the
        /// class.
        /// </summary>
        public enum WAVFileMode
        {
            READ,
            WRITE,
            READ_WRITE
        }

        /// <summary>
        /// WAVFile class: Default constructor
        /// </summary>
        public WAVFile()
        {
            InitMembers();
        }

        /// <summary>
        /// Destructor - Makes sure the file is closed.
        /// </summary>
        ~WAVFile()
        {
            this.Close();
        }

        public void Dispose()
        { this.Close(); }

        /// <summary>
        /// Opens a WAV file and attemps to read the header & audio information.
        /// </summary>
        /// <param name="FileName">The name of the file to open</param>
        /// <param name="Mode">The file opening mode.  Only READ and READ_WRITE are valid.  If you want to write only, then use Create().</param>
        /// <returns>A blank string on success, or a message on failure.</returns>
        public void Open(string FileName, WAVFileMode Mode)
        {
            this.Filename = FileName;
            this.Open(Mode);
        }

        /// <summary>
        /// Opens the file specified by mFilename and reads the file header and audio information.  Does not read any of the audio data.
        /// </summary>
        /// /// <param name="Mode">The file opening mode.  Only READ and READ_WRITE are valid.  If you want to write only, then use Create().</param>
        /// <returns>A blank string on success, or a message on failure.</returns>
        public void Open(WAVFileMode Mode)
        {
            if (this.Stream != null)
            {
                this.Stream.Close();
                this.Stream.Dispose();
                this.Stream = null;
            }
            String _FileName = this.Filename;

            this.InitMembers();

            // pMode should be READ or READ_WRITE.  Otherwise, throw an exception.  For
            // write-only mode, the user can call Create().
            if ((Mode != WAVFileMode.READ) && (Mode != WAVFileMode.READ_WRITE))
                throw new WAVFileException("File mode not supported: " + Mode, "WAVFile.Open()");

            if (!File.Exists(_FileName))
                throw new WAVFileException("File does not exist: " + _FileName, nameof(Open));

            if (!IsWaveFile(_FileName))
                throw new WAVFileException("File is not a WAV file: " + _FileName, nameof(Open));

            this.Filename = _FileName;

            try
            {
                this.Stream = File.Open(this.Filename, System.IO.FileMode.Open);

                this.FileOpenMode = Mode;

                // RIFF chunk (12 bytes total)
                // Read the header (first 4 bytes)
                byte[] buffer = new byte[4];
                this.Stream.Read(buffer, 0, 4);
                buffer.CopyTo(WAVHeader, 0);
                // Read the file size (4 bytes)
                this.Stream.Read(buffer, 0, 4);
                //mFileSizeBytes = BitConverter.ToInt32(buffer, 0);
                // Read the RIFF type
                this.Stream.Read(buffer, 0, 4);
                buffer.CopyTo(RIFFType, 0);

                // Format chunk (24 bytes total)
                // "fmt " (ASCII characters)
                this.Stream.Read(buffer, 0, 4);
                // Length of format chunk (always 16)
                this.Stream.Read(buffer, 0, 4);
                // 2 bytes (value always 1)
                this.Stream.Read(buffer, 0, 2);
                // # of channels (2 bytes)
                this.Stream.Read(buffer, 2, 2);
                this.NumChannels = (BitConverter.IsLittleEndian ? buffer[2] : buffer[3]);
                // Sample rate (4 bytes)
                this.Stream.Read(buffer, 0, 4);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.SampleRateHz = BitConverter.ToInt32(buffer, 0);
                // Bytes per second (4 bytes)
                this.Stream.Read(buffer, 0, 4);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.BytesPerSec = BitConverter.ToInt32(buffer, 0);
                // Bytes per sample (2 bytes)
                this.Stream.Read(buffer, 2, 2);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer, 2, 2);
                this.BytesPerSample = BitConverter.ToInt16(buffer, 2);
                // Bits per sample (2 bytes)
                this.Stream.Read(buffer, 2, 2);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer, 2, 2);
                this.BitsPerSample = BitConverter.ToInt16(buffer, 2);

                // Data chunk
                // "data" (ASCII characters)
                this.Stream.Read(buffer, 0, 4);
                // Length of data to follow (4 bytes)
                this.Stream.Read(buffer, 0, 4);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.DataSizeBytes = BitConverter.ToInt32(buffer, 0);

                // Total of 44 bytes read up to this point.

                // The data size should be file size - 36 bytes.  If not, then set
                // it to that.
                if (this.DataSizeBytes != (int)(FileSizeBytes - HeaderSize))
                    this.DataSizeBytes = (int)(FileSizeBytes - HeaderSize);

                // The rest of the file is the audio data, which
                // can be read by successive calls to NextSample().

                this.NumSamplesRemaining = this.NumSamples;

            }
            catch (Exception exc)
            {
                throw new WAVFileException(exc.Message, "WAVFile.Open()");
            }
        }
        public void Flush()
        {
            if (this.Stream != null)
            { // If in write or read/write mode, write the file size information to
              // the header.
                if ((this.FileOpenMode == WAVFileMode.WRITE) || (this.FileOpenMode == WAVFileMode.READ_WRITE))
                {
                    long p = this.Stream.Position;

                    // File size: Offset 4, 4 bytes
                    this.Stream.Seek(4, SeekOrigin.Begin);
                    // Note: Per the WAV file spec, we need to write file size - 8 bytes.
                    // The header is 44 bytes, and 44 - 8 = 36, so we write
                    // mDataBytesWritten + 36.
                    // 2009-03-17: Now using FileSizeBytes - 8 (to avoid mDataBytesWritten).
                    //mFileStream.Write(BitConverter.GetBytes(mDataBytesWritten+36), 0, 4);
                    int size = (int)FileSizeBytes - 8;
                    byte[] buffer = BitConverter.GetBytes(size);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(buffer);
                    this.Stream.Write(buffer, 0, 4);
                    // Data size: Offset 40, 4 bytes
                    this.Stream.Seek(40, 0);
                    //mFileStream.Write(BitConverter.GetBytes(mDataBytesWritten), 0, 4);
                    size = (int)(FileSizeBytes - DataStartPos);
                    buffer = BitConverter.GetBytes(size);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(buffer);
                    this.Stream.Write(buffer, 0, 4);
                    this.Stream.Flush();

                    this.Stream.Position = p;
                }
            }
        }
        /// <summary>
        /// Closes the file
        /// </summary>
        public void Close()
        {
            try
            {
                this.Flush();
            }
            catch (Exception exc)
            {
                throw new WAVFileException(exc.Message, "WAVFile.Close()");
            }
            finally
            {
                if (this.Stream != null)
                {
                    this.Stream.Close();
                    this.Stream.Dispose();
                    this.Stream = null;
                }

                // Reset the members back to defaults
                this.InitMembers();
            }
        }

        /// <summary>
        /// When in read mode, returns the next audio sample from the
        /// file.  The return value is a byte array and will contain one
        /// byte if the file contains 8-bit audio or 2 bytes if the file
        /// contains  16-bit audio.  The return value will be null if no
        /// next sample can be read.  For 16-bit samples, the byte array
        /// can be converted to a 16-bit integer using BitConverter.ToInt16().
        /// If there is an error, this method will throw a WAVFileReadException.
        /// </summary>
        /// <returns>A byte array containing the audio sample</returns>
        public byte[] NextSampleBytes(int SampleCount = 1)
        {
            byte[] audioSample = null;

            if (SampleCount <= 0 || SampleCount > this.NumSamplesRemaining)
                throw new WAVFileReadException("SimpleCount is out of range.", "WAVFile.NextSampleBytes()");

            // Throw an exception if mFileStream is null
            if (this.Stream == null)
                throw new WAVFileReadException("Read attempted with null internal file stream.", "WAVFile.NextSampleBytes()");

            // We should be in read or read/write mode.
            if ((this.FileOpenMode != WAVFileMode.READ) && (this.FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileReadException("Read attempted in incorrect mode: " + FileOpenMode, "WAVFile.NextSampleBytes()");

            try
            {
                int numBytes = this.BitsPerSample / 8 * SampleCount; // 8 bits per byte
                audioSample = new byte[numBytes];
                this.Stream.Read(audioSample, 0, numBytes);

                this.NumSamplesRemaining -= SampleCount;
            }
            catch (Exception exc)
            {
                audioSample = null;
                throw new WAVFileReadException(exc.Message, "WAVFile.NextSampleBytes()");
            }

            return audioSample;
        }

        /// <summary>
        /// When in read mode, returns the next audio sample from the loaded audio
        /// file.  This is a convenience method that can be used when it is known
        /// that the audio file contains 8-bit audio.  If the audio file is not
        /// 8-bit, this method will throw a WAVFileReadException. 
        /// </summary>
        /// <returns>The next audio sample from the loaded audio file.</returns>
        public byte NextSample8()
        {
            // If the audio data is not 8-bit, throw an exception.
            if (this.BitsPerSample != 8)
                throw new WAVFileReadException("Attempted to retrieve an 8-bit sample when audio is not 8-bit.", "WAVFile.NextSample8()");

            byte sample8 = 0;

            // Get the next sample using GetNextSample_ByteArray()
            byte[] sample = this.NextSampleBytes(1);
            if (sample != null)
                sample8 = sample[0];

            return sample8;
        }

        /// <summary>
        /// When in read mode, returns the next audio sample from the loaded audio
        /// file.  This is a convenience method that can be used when it is known
        /// that the audio file contains 16-bit audio.  If the audio file is not
        /// 16-bit, this method will throw a WAVFileReadException. 
        /// </summary>
        /// <returns>The next audio sample from the loaded audio file.</returns>
        public short NextSample16()
        {
            // If the audio data is not 16-bit, throw an exception.
            if (this.BitsPerSample != 16)
                throw new WAVFileReadException("Attempted to retrieve a 16-bit sample when audio is not 16-bit.", "WAVFile.NextSample16()");

            short sample16 = 0;

            // Get the next sample using GetNextSample_ByteArray()
            byte[] sample = this.NextSampleBytes(1);
            if (sample != null)
                sample16 = BitConverter.ToInt16(sample, 0);

            return sample16;
        }

        /// <summary>
        /// When in read mode, returns the next audio sample from the loaded audio
        /// file.  This returns the value as a 16-bit value regardless of whether the
        /// audio file is 8-bit or 16-bit.  If the audio is 8-bit, then the 8-bit sample
        /// value will be scaled to a 16-bit value.
        /// </summary>
        /// <returns>The next audio sample from the loaded audio file, as a 16-bit value, scaled if necessary.</returns>
        public short NextSampleAs16()
        {
            short sample_16bit = 0;

            if (this.BitsPerSample == 8)
                sample_16bit = ScaleByteToShort(this.NextSample8());
            else if (this.BitsPerSample == 16)
                sample_16bit = this.NextSample16();

            return sample_16bit;
        }

        /// <summary>
        /// When in read mode, returns the next audio sample from the loaded audio
        /// file.  This returns the value as an 8-bit value regardless of whether the
        /// audio file is 8-bit or 16-bit.  If the audio is 16-bit, then the 16-bit sample
        /// value will be scaled to an 8-bit value.
        /// </summary>
        /// <returns>The next audio sample from the loaded audio file, as an 8-bit value, scaled if necessary.</returns>
        public byte NextSampleAs8()
        {
            byte sample_8bit = 0;

            if (this.BitsPerSample == 8)
                sample_8bit = this.NextSample8();
            else if (this.BitsPerSample == 16)
                sample_8bit = ScaleShortToByte(this.NextSample16());

            return sample_8bit;
        }

        /// <summary>
        /// When in write mode, adds a new sample to the audio file.  Takes
        /// an array of bytes representing the sample.  The array should
        /// contain the correct number of bytes to match the sample size.
        /// If there are any errors, this method will throw a WAVFileWriteException.
        /// </summary>
        /// <param name="Sample">An array of bytes representing the audio sample</param>
        public void AppendSamples(byte[] Sample)
        {
            if (Sample == null) throw new ArgumentNullException(nameof(Sample));

            // We should be in write or read/write mode.
            if ((FileOpenMode != WAVFileMode.WRITE) && (FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileWriteException("Write attempted in incorrect mode: " + FileOpenMode,
                                                "WAVFile.AppendSamples()");

            // Throw an exception if mFileStream is null
            if (Stream == null)
                throw new WAVFileWriteException("Write attempted with null internal file stream.", "WAVFile.AppendSamples()");

            // If pSample contains an incorrect number of bytes for the
            // sample size, then throw an exception.
            if (Sample.Length != (BitsPerSample / 8)) // 8 bits per byte
                throw new WAVFileWriteException("Attempt to add an audio sample of incorrect size.", "WAVFile.AppendSamples()");

            try
            {
                Stream.Write(Sample, 0, Sample.Length);
                //mDataBytesWritten += numBytes;
            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.AppendSamples()");
            }

        }
        /// <summary>
        /// When in write mode, adds a new sample to the audio file.  Takes
        /// an array of bytes representing the sample.  The array should
        /// contain the correct number of bytes to match the sample size.
        /// If there are any errors, this method will throw a WAVFileWriteException.
        /// </summary>
        /// <param name="Sample">An array of shorts representing the audio sample</param>
        public void AppendSamples(IList<byte> Sample)
        {
            if (Sample == null) throw new ArgumentNullException(nameof(Sample));

            // We should be in write or read/write mode.
            if ((FileOpenMode != WAVFileMode.WRITE) && (FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileWriteException("Write attempted in incorrect mode: " + FileOpenMode,
                                                "WAVFile.AppendSamples()");

            // Throw an exception if mFileStream is null
            if (Stream == null)
                throw new WAVFileWriteException("Write attempted with null internal file stream.", "WAVFile.AppendSamples()");

            // If pSample contains an incorrect number of bytes for the
            // sample size, then throw an exception.
            if (Sample.Count != (BitsPerSample / 8)) // 8 bits per byte
                throw new WAVFileWriteException("Attempt to add an audio sample of incorrect size.", "WAVFile.AppendSamples()");

            try
            {
                int numShort = Sample.Count;
                for (int i = 0; i < numShort; i++)
                {
                    Stream.WriteByte(Sample[i]);
                }
            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.AppendSamples()");
            }

        }
        /// <summary>
        /// When in write mode, adds a new sample to the audio file.  Takes
        /// an array of bytes representing the sample.  The array should
        /// contain the correct number of bytes to match the sample size.
        /// If there are any errors, this method will throw a WAVFileWriteException.
        /// </summary>
        /// <param name="Sample">An array of shorts representing the audio sample</param>
        public void AppendSamples(IList<short> Sample)
        {
            if (Sample == null) throw new ArgumentNullException(nameof(Sample));

            // We should be in write or read/write mode.
            if ((FileOpenMode != WAVFileMode.WRITE) && (FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileWriteException("Write attempted in incorrect mode: " + FileOpenMode,
                                                "WAVFile.AppendSamples()");

            // Throw an exception if mFileStream is null
            if (Stream == null)
                throw new WAVFileWriteException("Write attempted with null internal file stream.", "WAVFile.AppendSamples()");

            // If pSample contains an incorrect number of bytes for the
            // sample size, then throw an exception.
            //if (Sample.Count != (BitsPerSample / 8)) // 8 bits per byte
            //    throw new WAVFileWriteException("Attempt to add an audio sample of incorrect size.", "WAVFile.AppendSamples()");

            try
            {
                int numShort = Sample.Count;
                for (int i = 0; i < numShort; i++)
                {
                    byte b0 = (byte)(Sample[i] & 0xff);
                    byte b1 = (byte)((Sample[i] >> 8) & 0xff);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Stream.WriteByte(b1);
                        Stream.WriteByte(b0);
                    }
                    else
                    {
                        Stream.WriteByte(b0);
                        Stream.WriteByte(b1);
                    }
                }
            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.AppendSamples()");
            }

        }
        /// <summary>
        /// When in write mode, adds an 8-bit sample to the audio file.
        /// Takes a byte containing the sample.  If the audio file is
        /// not 8-bit, this method will throw a WAVFileWriteException.
        /// </summary>
        /// <param name="Sample">The audio sample to add</param>
        public void AppendSample(byte Sample)
        {
            // If the audio data is not 8-bit, throw an exception.
            if (BitsPerSample != 8)
                throw new WAVFileWriteException("Attempted to add an 8-bit sample when audio file is not 8-bit.", "WAVFile.AppendSample()");

            // We should be in write or read/write mode.
            if ((FileOpenMode != WAVFileMode.WRITE) && (FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileWriteException("Write attempted in incorrect mode: " + FileOpenMode,
                                                "WAVFile.AddSample_ByteArray()");

            // Throw an exception if mFileStream is null
            if (Stream == null)
                throw new WAVFileWriteException("Write attempted with null internal file stream.", "WAVFile.AppendSample()");

            try
            {
                Stream.WriteByte(Sample);
            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.AppendSample()");
            }
        }

        /// <summary>
        /// When in write mode, adds a 16-bit sample to the audio file.
        /// Takes an Int16 containing the sample.  If the audio file is
        /// not 16-bit, this method will throw a WAVFileWriteException.
        /// </summary>
        /// <param name="Sample">The audio sample to add</param>
        public void AppendSample(short Sample)
        {
            // If the audio data is not 16-bit, throw an exception.
            if (BitsPerSample != 16)
                throw new WAVFileWriteException("Attempted to add a 16-bit sample when audio file is not 16-bit.", "WAVFile.AppendSample()");

            // We should be in write or read/write mode.
            if ((FileOpenMode != WAVFileMode.WRITE) && (FileOpenMode != WAVFileMode.READ_WRITE))
                throw new WAVFileWriteException("Write attempted in incorrect mode: " + FileOpenMode,
                                                "WAVFile.AppendSample()");

            // Throw an exception if mFileStream is null
            if (Stream == null)
                throw new WAVFileWriteException("Write attempted with null internal file stream.", "WAVFile.AppendSample()");

            try
            {
                byte b0 = (byte)(Sample & 0xff);
                byte b1 = (byte)((Sample >> 8) & 0xff);
                if (!BitConverter.IsLittleEndian)
                {
                    Stream.WriteByte(b1);
                    Stream.WriteByte(b0);
                }
                else
                {
                    Stream.WriteByte(b0);
                    Stream.WriteByte(b1);
                }
            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.AppendSample()");
            }

        }

        /// <summary>
        /// Creates a new WAV audio file.
        /// </summary>
        /// <param name="Filename">The name of the audio file to create</param>
        /// <param name="Stereo">Whether or not the audio file should be stereo (if this is false, the audio file will be mono).</param>
        /// <param name="SampleRate">The sample rate of the audio file (in Hz)</param>
        /// <param name="BitsPerSample">The number of bits per sample (8 or 16)</param>
        /// <param name="Overwrite">Whether or not to overwrite the file if it exists.  If this is false, then a System.IO.IOException will be thrown if the file exists.</param>
        /// 

        public void Create(string Filename, bool Stereo, int SampleRate, short BitsPerSample, bool Overwrite)
        {
            // In case a file is currently open, make sure it
            // is closed.  Note: Close() calls InitMembers().
            this.Close();

            // If the sample rate is not supported, then throw an exception.
            if (!SupportedSampleRate(SampleRate))
                throw new WAVFileSampleRateException("Unsupported sample rate: " + SampleRate.ToString(), "WAVFile.Create()", SampleRate);
            // If the bits per sample is not supported, then throw an exception.
            if (!SupportedBitsPerSample(BitsPerSample))
                throw new WAVFileBitsPerSampleException("Unsupported number of bits per sample: " + BitsPerSample.ToString(), "WAVFile.Create()", BitsPerSample);

            try
            {
                // Create the file.  If pOverwrite is true, then use FileMode.Create to overwrite the
                // file if it exists.  Otherwise, use FileMode.CreateNew, which will throw a
                // System.IO.IOException if the file exists.
                if (Overwrite)
                    this.Stream = File.Open(Filename, System.IO.FileMode.Create);
                else
                    this.Stream = File.Open(Filename, System.IO.FileMode.CreateNew);

                this.FileOpenMode = WAVFileMode.WRITE;

                // Set the member data from the parameters.
                this.NumChannels = Stereo ? (byte)2 : (byte)1;
                this.SampleRateHz = SampleRate;
                this.BitsPerSample = BitsPerSample;

                // Write the parameters to the file header.

                // RIFF chunk (12 bytes total)
                // Write the chunk IDD ("RIFF", 4 bytes)
                byte[] buffer = StrToByteArray(this.WAVHeader = new char[4] { 'R', 'I', 'F', 'F' });
                this.Stream.Write(buffer, 0, 4);


                // File size size (4 bytes) - This will be 0 for now
                Array.Clear(buffer, 0, buffer.Length);
                this.Stream.Write(buffer, 0, 4);
                // RIFF type ("WAVE")
                buffer = StrToByteArray(this.RIFFType = new char[4] { 'W', 'A', 'V', 'E' });
                this.Stream.Write(buffer, 0, 4);

                // Format chunk (24 bytes total)
                // "fmt " (ASCII characters)
                buffer = StrToByteArray(new char[4] { 'f', 'm', 't', ' ' });
                this.Stream.Write(buffer, 0, 4);
                // Length of format chunk (always 16, 4 bytes)
                Array.Clear(buffer, 0, buffer.Length);
                buffer[0] = 16;
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.Stream.Write(buffer, 0, 4);
                // 2 bytes (always 1)
                Array.Clear(buffer, 0, buffer.Length);
                buffer[0] = 1;
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer, 0, 2);
                this.Stream.Write(buffer, 0, 2);
                // # of channels (2 bytes)
                Array.Clear(buffer, 0, buffer.Length);
                buffer[0] = NumChannels;
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer, 0, 2);
                this.Stream.Write(buffer, 0, 2);
                // Sample rate (4 bytes)
                buffer = BitConverter.GetBytes(SampleRateHz);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.Stream.Write(buffer, 0, 4);
                // Calculate the # of bytes per sample: 1=8 bit Mono, 2=8 bit Stereo or
                // 16 bit Mono, 4=16 bit Stereo
                short bytesPerSample = 0;
                if (Stereo)
                    bytesPerSample = (short)((this.BitsPerSample / 8) * 2);
                else
                    bytesPerSample = (short)(this.BitsPerSample / 8);
                // Write the # of bytes per second (4 bytes)
                this.BytesPerSec = SampleRateHz * bytesPerSample;
                buffer = BitConverter.GetBytes(BytesPerSec);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);
                this.Stream.Write(buffer, 0, 4);
                // Write the # of bytes per sample (2 bytes)
                byte[] buffer_2bytes = BitConverter.GetBytes(bytesPerSample);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer_2bytes);
                this.Stream.Write(buffer_2bytes, 0, 2);
                // Bits per sample (2 bytes)
                buffer_2bytes = BitConverter.GetBytes(this.BitsPerSample);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(buffer_2bytes);
                this.Stream.Write(buffer_2bytes, 0, 2);

                // Data chunk
                // "data" (ASCII characters)
                buffer = StrToByteArray(new char[4] { 'd', 'a', 't', 'a' });
                this.Stream.Write(buffer, 0, 4);
                // Length of data to follow (4 bytes) - This will be 0 for now
                Array.Clear(buffer, 0, buffer.Length);
                this.Stream.Write(buffer, 0, 4);
                this.DataSizeBytes = 0;

                // Total of 44 bytes written up to this point.

                // The rest of the file is the audio data

            }
            catch (Exception exc)
            {
                throw new WAVFileWriteException(exc.Message, "WAVFile.Create()");
            }
        }

        /// <summary>
        /// Creates a new WAV audio file.  This is an overload that always overwrites the file if it exists.
        /// </summary>
        /// <param name="Filename">The name of the audio file to create</param>
        /// <param name="Stereo">Whether or not the audio file should be stereo (if this is false, the audio file will be mono).</param>
        /// <param name="SampleRate">The sample rate of the audio file (in Hz)</param>
        /// <param name="BitsPerSample">The number of bits per sample (8 or 16)</param>
        public void Create(string Filename, bool Stereo, int SampleRate, short BitsPerSample)
            => this.Create(Filename, Stereo, SampleRate, BitsPerSample, true);

        /// <summary>
        /// Returns whether or not a file is a WAV audio file.
        /// </summary>
        /// <param name="Filename">The name of the file to check</param>
        /// <returns>true if the file is a wave audio file, or false if not</returns>
        public static bool IsWaveFile(String Filename)
        {
            bool retval = false;

            if (File.Exists(Filename))
            {
                try
                {
                    // For a WAV file, the first 4 bytes should be "RIFF", and
                    // the RIFF type (3rd set of 4 bytes) should be "WAVE".
                    char[] fileID = new char[4];
                    char[] RIFFType = new char[4];

                    byte[] buffer = new byte[4];
                    using (FileStream fileStream = File.Open(Filename, System.IO.FileMode.Open))
                    {

                        // Read the file ID (first 4 bytes)
                        fileStream.Read(buffer, 0, 4);
                        buffer.CopyTo(fileID, 0);

                        // Read the next 4 bytes (but we don't care about this)
                        fileStream.Read(buffer, 0, 4);

                        // Read the RIFF ID (4 bytes)
                        fileStream.Read(buffer, 0, 4);
                        buffer.CopyTo(RIFFType, 0);
                    }
                    string fileIDStr = new string(fileID);
                    string RIFFTypeStr = new string(RIFFType);

                    retval = ((fileIDStr == "RIFF") && (RIFFTypeStr == "WAVE"));
                }
                catch (Exception exc)
                {
                    throw new WAVFileException(exc.Message, "WAVFile.IsWaveFile()");
                }
            }

            return retval;
        }

        /// <summary>
        /// Returns whether or not a sample rate is supported by this class.
        /// </summary>
        /// <param name="SampleRateHz">A sample rate (in Hz)</param>
        /// <returns>true if the sample rate is supported, or false if not.</returns>
        public static bool SupportedSampleRate(int SampleRateHz)
            => ((SampleRateHz == 8000) || (SampleRateHz == 11025) ||
                    (SampleRateHz == 16000) || (SampleRateHz == 18900) ||
                    (SampleRateHz == 22050) || (SampleRateHz == 32000) ||
                    (SampleRateHz == 37800) || (SampleRateHz == 44056) ||
                    (SampleRateHz == 44100) || (SampleRateHz == 48000));

        /// <summary>
        /// Returns whether or not a number of bits per sample is supported by this class.
        /// </summary>
        /// <param name="BitsPerSample">A number of bits per sample</param>
        /// <returns>true if the bits/sample is supported by this class, or false if not.</returns>
        public static bool SupportedBitsPerSample(short BitsPerSample)
            => ((BitsPerSample == 8) || (BitsPerSample == 16));

        /// <summary>
        /// Moves the file pointer back to the start of the audio data.
        /// </summary>
        public void SeekToAudioDataStart()
        {
            SeekToAudioSample(0);
            // Update mSamplesRemaining - but this is only necessary for read or read/write mode.
            if ((FileOpenMode == WAVFileMode.READ) || (FileOpenMode == WAVFileMode.READ_WRITE))
                NumSamplesRemaining = NumSamples;
        }

        /// <summary>
        /// Moves the file pointer to a given audio sample number.
        /// </summary>
        /// <param name="SampleNum">The sample number to which to move the file pointer</param>
        public void SeekToAudioSample(long SampleNum)
        {
            if (Stream != null)
            {
                // Figure out the byte position.  This will be mDataStartPos + however many
                // bytes per sample * pSampleNum.
                long bytesPerSample = BitsPerSample / 8;
                try
                {
                    Stream.Seek(DataStartPos + (bytesPerSample * SampleNum), 0);
                }
                catch (System.IO.IOException exc)
                {
                    throw new WAVFileIOException("Unable to to seek to sample " + SampleNum.ToString() + ": " + exc.Message,
                                                 "WAVFile.SeekToAudioSample()");
                }
                catch (System.NotSupportedException exc)
                {
                    throw new WAVFileIOException("Unable to to seek to sample " + SampleNum.ToString() + ": " + exc.Message,
                                                 "WAVFile.SeekToAudioSample()");
                }
                catch (Exception exc)
                {
                    throw new WAVFileException(exc.Message, "WAVFile.SeekToAudioSample()");
                }
            }
        }

        /// <summary>
        /// Initializes the data members (for the constructors).
        /// </summary>
        private void InitMembers()
        {
            Filename = null;
            Stream = null;
            WAVHeader = new char[4];
            DataSizeBytes = 0;
            BytesPerSample = 0;
            //_FileSizeBytes = 0;
            RIFFType = new char[4];

            // These audio format defaults correspond to the standard for
            // CD audio.
            NumChannels = 2;
            SampleRateHz = 44100;
            BytesPerSec = 176400;
            BitsPerSample = 16;

            FileOpenMode = WAVFileMode.READ;

            NumSamplesRemaining = 0;
        }

        /// <summary>
        /// Converts a string to a byte array.  The source for this came
        /// from http://www.chilkatsoft.com/faq/DotNetStrToBytes.html .
        /// </summary>
        /// <param name="text">A String object</param>
        /// <returns>A byte array containing the data from the String object</returns>
        private static byte[] StrToByteArray(char[] text) => Encoding.ASCII.GetBytes(text);

        /// <summary>
        /// Scales a byte value to a 16-bit (short) value by calculating the value's percentage of
        /// maximum for 8-bit values, then calculating the 16-bit value with that
        /// percentage.
        /// </summary>
        /// <param name="ByteVal">A byte value to convert</param>
        /// <returns>The 16-bit scaled value</returns>
        private static short ScaleByteToShort(byte ByteVal)
        {
            short val_16bit = 0;
            double scaleMultiplier = 0.0;
            if (ByteVal > 0)
            {
                scaleMultiplier = (double)ByteVal / (double)byte.MaxValue;
                val_16bit = (short)((double)short.MaxValue * scaleMultiplier);
            }
            else if (ByteVal < 0)
            {
                scaleMultiplier = (double)ByteVal / (double)byte.MinValue;
                val_16bit = (short)((double)short.MinValue * scaleMultiplier);
            }

            return val_16bit;
        }

        /// <summary>
        /// Scales a 16-bit (short) value to an 8-bit (byte) value by calculating the
        /// value's percentage of maximum for 16-bit values, then calculating the 8-bit
        /// value with that percentage.
        /// </summary>
        /// <param name="ShortVal">A 16-bit value to convert</param>
        /// <returns>The 8-bit scaled value</returns>
        private static byte ScaleShortToByte(short ShortVal)
        {
            byte val_8bit = 0;
            double scaleMultiplier = 0.0;
            if (ShortVal > 0)
            {
                scaleMultiplier = (double)ShortVal / (double)short.MaxValue;
                val_8bit = (byte)((double)byte.MaxValue * scaleMultiplier);
            }
            else if (ShortVal < 0)
            {
                scaleMultiplier = (double)ShortVal / (double)short.MinValue;
                val_8bit = (byte)((double)byte.MinValue * scaleMultiplier);
            }

            return val_8bit;
        }

    }
}
