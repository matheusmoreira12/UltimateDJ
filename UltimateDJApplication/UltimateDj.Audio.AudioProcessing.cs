using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Dsp;

using UltimateDJ.Audio.Math;

namespace UltimateDJ.Audio
{
    static class UDJAudioManager
    {
        public static Dictionary<Guid, AudioFileReader> Readers = new Dictionary<Guid, AudioFileReader> { };
        public static Dictionary<Guid, WaveChannel32> Channels = new Dictionary<Guid, WaveChannel32> { };
        public static Dictionary<Guid, WaveMixerStream32> Mixers = new Dictionary<Guid, WaveMixerStream32> { };
        public static Dictionary<Guid, WaveOut> OutputDevices = new Dictionary<Guid, WaveOut> { };

        public static Guid SetUpReader(string filename)
        {
            var guid = Guid.NewGuid();
            Readers.Add(guid, new AudioFileReader(filename));
            return guid;
        }

        public static Guid SetUpChannel(WaveStream sourceStream)
        {
            var guid = Guid.NewGuid();
            Channels.Add(guid, new WaveChannel32(sourceStream));
            return guid;
        }

        public static Guid SetUpMixer(WaveStream[] inputStreams, bool autoStop = true)
        {
            var guid = Guid.NewGuid();
            Mixers.Add(guid, new WaveMixerStream32(inputStreams, autoStop));
            return guid;
        }

        public static Guid SetUpOutputDevice(IWaveProvider inputStream)
        {
            var guid = Guid.NewGuid();
            var outDev = new WaveOut();
            outDev.Init(inputStream);
            outDev.Play();
            OutputDevices.Add(guid, outDev);
            return guid;
        }
    }

    static class UDJAudioProcessing
    {
        static BiQuadFilter[] tempInputFilters = null;
        static WaveStream tempInputStream = null;
        static byte[] tempInputBuffer = null;
        static byte[] tempOutputBuffer = null;

        async static Task<int> asynchLoadBuffer(int offset, int count)
        {
            //if the temporary input stream is null, throw a NullReferenceException
            if (tempInputStream == null)
                throw new NullReferenceException();
            //if arguments are out of range, throw an ArgumentOutOfRangeException
            if (offset < 0 || offset + count > tempInputStream.Length || count <= 0)
                throw new ArgumentOutOfRangeException();
            //if the temporary input buffer is null, create an instance
            if (tempInputBuffer == null)
                tempInputBuffer = new byte[tempInputStream.Length];
            return await tempInputStream.ReadAsync(tempInputBuffer, offset, count);
        }

        async static Task asyncWriteOutputBuffer(int i, int j, int bps)
        {
            //block copy the converted data to the output temporary buffer
            Buffer.BlockCopy(
                //convert the resulting float to byte[]
                BitConverter.GetBytes(
                    //transform the sample from the input temporary buffer
                    tempInputFilters[i].Transform(
                        //select, extract and convert to byte[] a float from the input buffer  
                        BitConverter.ToSingle(tempInputBuffer, j)
                    )), 0, tempOutputBuffer, j, bps);
        }
        /// <summary>
        /// Processes asynchronously a segmented copy of the audio buffer.
        /// </summary>
        /// <param name="waveStream"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        async static Task applyFilters()
        {
            //if the temporary input buffer is null, throw a NullReferenceException
            if (tempInputStream == null || tempInputBuffer == null || tempInputFilters == null)
                throw new NullReferenceException();
            //get the input buffer length
            int length = tempInputBuffer.Length,
                //get the bytes-per-sample value
                bps = tempInputStream.WaveFormat.BitsPerSample / 8;
            tempOutputBuffer = new byte[length];
            //start applying filters
            for (int i = 0; i < tempInputFilters.Length; i++)
                for (int j = 0; j < length; j += bps)
                    await asyncWriteOutputBuffer(i, j, bps);
        }

        public static readonly float[] GRAPHIC_3_BAND_EQ_DEFAULT_FREQ = { 900, 3000, 6000 };
        public static readonly float GRAPHIC_3_BAND_EQ_DEFAULT_Q = 3;

        public static readonly float[] GRAPHIC_7_BAND_EQ_DEFAULT_FREQ = { 50, 125, 315, 750, 2200, 6000, 12000 };
        public static readonly float GRAPHIC_7_BAND_EQ_DEFAULT_Q = 3;

        public static readonly float[] GRAPHIC_10_BAND_EQ_DEFAULT_FREQ = new float[] { 32, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        public static readonly float GRAPHIC_10_BAND_EQ_DEFAULT_Q = 3;

        public static readonly float GRAPHIC_EQ_DBGAIN_MIN = -15; //dB
        public static readonly float GRAPHIC_EQ_DBGAIN_MAX = -15; //dB

        public static double[] LinearArrayToDecibelsArray(double[] linear)
        {
            return linear.Select(item => NAudio.Utils.Decibels.LinearToDecibels(item)).ToArray();
        }
        public static float[] LinearArrayToDecibelsArray(float[] linear)
        {
            return linear.Select(item => (float)NAudio.Utils.Decibels.LinearToDecibels(item)).ToArray();
        }

        public static BiQuadFilter[] Equalizer(float sampleRate, int nBands, float[] centreFreqs, float q, float[] dbGain)
        {
            BiQuadFilter[] filters = new BiQuadFilter[nBands];
            for (int i = 0; i < nBands && i < centreFreqs.Length && i < dbGain.Length; i++)
                filters[i] = BiQuadFilter.PeakingEQ(sampleRate, centreFreqs[i], q, dbGain[i]);
            return filters;
        }

        public static BiQuadFilter[] Graphic3BandEQ(float sampleRate, float[] dbGain)
        {
            return Equalizer(sampleRate, 3, GRAPHIC_3_BAND_EQ_DEFAULT_FREQ, GRAPHIC_3_BAND_EQ_DEFAULT_Q, dbGain);
        }
        public static BiQuadFilter[] Graphic3BandLinearGainEQ(float sampleRate, float[] linearGain)
        {
            return Graphic3BandEQ(sampleRate, LinearArrayToDecibelsArray(linearGain));
        }

        public static BiQuadFilter[] Graphic7BandEQ(float sampleRate, float[] dbGain)
        {
            return Equalizer(sampleRate, 7, GRAPHIC_3_BAND_EQ_DEFAULT_FREQ, GRAPHIC_7_BAND_EQ_DEFAULT_Q, dbGain);
        }
        public static BiQuadFilter[] Graphic7BandLinearGainEQ(float sampleRate, float[] linearGain)
        {
            return Graphic7BandEQ(sampleRate, LinearArrayToDecibelsArray(linearGain));
        }

        public static BiQuadFilter[] Graphic10BandEQ(float sampleRate, float[] dbGain)
        {
            return Equalizer(sampleRate, 11, GRAPHIC_10_BAND_EQ_DEFAULT_FREQ, GRAPHIC_10_BAND_EQ_DEFAULT_Q, dbGain);
        }
        public static BiQuadFilter[] Graphic10BandLinearGainEQ(float sampleRate, float[] linearGain)
        {
            return Graphic10BandEQ(sampleRate, LinearArrayToDecibelsArray(linearGain));
        }

        public static async Task ApplyEqualizer(this WaveStream waveStream, BiQuadFilter[] equalizer)
        {
            tempInputStream = waveStream;
            tempInputStream.Position = 0;

            tempInputFilters = equalizer;

            //read to buffer
            int x = 0;
            while (x < tempInputStream.Length)
                x += await asynchLoadBuffer(x, 2048);

            //apply filters
            await applyFilters();

            var provider = new BufferedWaveProvider(waveStream.WaveFormat);
            provider.BufferLength = tempOutputBuffer.Length;
            provider.AddSamples(tempOutputBuffer, 0, tempOutputBuffer.Length);

            Guid guid2 = UDJAudioManager.SetUpOutputDevice(provider);
        }
    }
}
