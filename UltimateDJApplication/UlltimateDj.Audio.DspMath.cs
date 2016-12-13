using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Dsp;

namespace UltimateDJ.Audio.Math
{
    /// <summary>
    /// Has all necessary math functions for the FFT.
    /// </summary>
    static class UDJAudioDspMath
    {
        /// <summary>
        /// Gets the frequency of a FFT bin according to its position. 
        /// </summary>
        /// <param name="index">Zero-indexed bin position.</param>
        /// <param name="sampleRate">FFT audio sample rate.</param>
        /// <param name="fftSize">FFT Buffer size.</param>
        /// <returns></returns>
        public static double FFTBinFrequency(int index, int sampleRate, int fftSize)
        {
            return index * sampleRate / (double)fftSize;
        }

        /// <summary>
        /// Gets the nearest zero-indexed position of a bin according to its frequency.
        /// </summary>
        /// <param name="frequency">Desired audio frequency.</param>
        /// <param name="sampleRate">FFT audio sample rate.</param>
        /// <param name="fftSize">FFT Buffer size.</param>
        /// <returns></returns>
        public static int FFTNearestBin(double frequency, int sampleRate, int fftSize)
        {
            return (int)System.Math.Round(frequency * fftSize / sampleRate);
        }

        /// <summary>
        /// Converts a float to a NAudio.Dsp.Complex by assigning its real part to the float, and its imaginary part to zero.
        /// </summary>
        /// <param name="input">The float to be converted.</param>
        /// <returns>The converted NAudio.Dsp.Complex.</returns>
        public static Complex ToComplex(this float input)
        {
            Complex output = new Complex();
            output.X = input;
            output.Y = 0;
            return output;
        }

        /// <summary>
        /// Converts a NAudio.Dsp.Complex to float by extracting its real part.
        /// </summary>
        /// <param name="input">The NAudio.Dsp.Complex to be converted.</param>
        /// <returns>The converted float.</returns>
        public static float ToFloat(this Complex input)
        {
            return input.X;
        }
    }
}
