using System;

namespace SASR
{
    /// <summary>
    /// Class for exception objects thrown by the WAVFile class when an error occurs
    /// </summary>
    public class WAVFileException : Exception
    {
        public String ThrowingMethodName { get; }
        public WAVFileException(System.String ErrorMessage, System.String ThrowingMethodName)
            : base(ErrorMessage)
        {
            this.ThrowingMethodName = ThrowingMethodName;
        }

    }

    /// <summary>
    /// This exception is thrown by the WAVFile class during audio file merging.
    /// </summary>
    public class WAVFileAudioMergeException : WAVFileException
    {
        public WAVFileAudioMergeException(String ErrorMessage, String ThrowingMethodName)
            : base(ErrorMessage, ThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for read errors.
    /// </summary>
    public class WAVFileReadException : WAVFileException
    {
        public WAVFileReadException(String ErrorMessage, String ThrowingMethodName)
            : base(ErrorMessage, ThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for write errors.
    /// </summary>
    public class WAVFileWriteException : WAVFileException
    {
        public WAVFileWriteException(String ErrorMessage, String ThrowingMethodName)
            : base(ErrorMessage, ThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// Represents an exception for general WAV file I/O
    /// </summary>
    public class WAVFileIOException : WAVFileException
    {
        public WAVFileIOException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage, pThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for an unsupported number of bits per sample.
    /// </summary>
    public class WAVFileBitsPerSampleException : WAVFileException
    {
        public short BitsPerSample { get; }
        public WAVFileBitsPerSampleException(System.String ErrorMessage, System.String pThrowingMethodName, short BitsPerSample)
            : base(ErrorMessage, pThrowingMethodName)
        {
            this.BitsPerSample = BitsPerSample;
        }

    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for an unsupported sample rate.
    /// </summary>
    public class WAVFileSampleRateException : WAVFileException
    {
        public int SampleRate { get; }
        public WAVFileSampleRateException(String ErrorMessage, String ThrowingMethodName, int SampleRate)
            : base(ErrorMessage, ThrowingMethodName)
        {
            this.SampleRate = SampleRate;
        }

    }
}