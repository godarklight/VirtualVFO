using System.Numerics;

namespace VirtualVFO;
class Program
{
    static void Main(string[] args)
    {
        WavSource carrier = new WavSource("carrier.wav");
        WavSource baseband = new WavSource("baseband.wav");

        WavSink sinknovfo = new WavSink("mixer-novfo.wav");
        WavSink sink = new WavSink("mixer.wav");
        WavSink baseband90 = new WavSink("baseband-90.wav");

        int chunk_size = 8192;

        Complex[] carrierFFTInput = new Complex[chunk_size * 2];
        Complex[] basebandFFTInput = new Complex[chunk_size * 2];

        Complex[] mixerOutput = new Complex[chunk_size];
        Complex[] mixernovfoOutput = new Complex[chunk_size];
        Complex[] basebandOutput = new Complex[chunk_size];

        int lastProgress = 0;
        int frame = 0;

        double virtualVfoPhase = 0.0;
        double virtualVfoFrequency = -1200.0;

        while (true)
        {
            frame++;
            double[] carrierchunk = carrier.Read(chunk_size);
            double[] basebandchunk = baseband.Read(chunk_size);

            int progress = carrier.GetProgress();
            if (lastProgress != progress)
            {
                lastProgress = progress;
                Console.WriteLine($"{progress}%");
            }

            //Done reading, quit
            if (carrierchunk == null | basebandchunk == null)
            {
                break;
            }

            //Shift and move in data
            for (int i = 0; i < chunk_size; i++)
            {
                carrierFFTInput[i] = carrierFFTInput[i + chunk_size];
                basebandFFTInput[i] = basebandFFTInput[i + chunk_size];
                carrierFFTInput[i + chunk_size] = carrierchunk[i];
                basebandFFTInput[i + chunk_size] = basebandchunk[i];
            }

            //Take FFT's
            Complex[] carrierFFT = FFT.CalcFFT(carrierFFTInput);
            Complex[] basebandFFT = FFT.CalcFFT(basebandFFTInput);

            //Hilbert transform by doubling positive frequencies and zeroing negative frequencies
            for (int i = 0; i < (chunk_size * 2); i++)
            {
                //Skip DC and nyquist
                if (i == 0 || i == chunk_size)
                {
                    continue;
                }
                //Positive frequencies
                if (i < chunk_size)
                {
                    carrierFFT[i] = carrierFFT[i] * 2.0;
                    basebandFFT[i] = basebandFFT[i] * 2.0;
                }
                //Negative frequencies
                if (i > chunk_size)
                {
                    carrierFFT[i] = 0.0;
                    basebandFFT[i] = 0.0;
                }
            }

            //Filter the AF frequencies - because we can.
            //Low pass block
            double freqPerBin = 48000.0 / (double)(2.0 * chunk_size);
            int hz50 = (int)(50.0 / freqPerBin);
            int hz100 = (int)(100.0 / freqPerBin);
            int hz3000 = (int)(3000.0 / freqPerBin);
            int hz3100 = (int)(3100.0 / freqPerBin);
            for (int i = 0; i < hz50; i++)
            {
                basebandFFT[i] = 0.0;
            }
            //Low pass filter rolloff
            double lowDelta = hz100 - hz50;
            for (int i = hz50; i < hz100; i++)
            {
                double percent = (i - hz50) / lowDelta;
                basebandFFT[i] = basebandFFT[i] * percent;
            }
            //High pass rolloff
            double highDelta = hz3100 - hz3000;
            for (int i = hz3000; i < hz3100; i++)
            {
                double percent = 1.0 - ((i - hz3000) / highDelta);
                basebandFFT[i] = basebandFFT[i] * percent;
            }
            //High pass filter
            for (int i = hz3100; i < (chunk_size * 2); i++)
            {
                basebandFFT[i] = 0.0;
            }

            //Run an IFFT
            Complex[] carrierIFFT = FFT.CalcIFFT(carrierFFT);
            Complex[] basebandIFFT = FFT.CalcIFFT(basebandFFT);

            //Multiply quadrature signals togther (phasing product mixer)
            for (int i = 0; i < chunk_size; i++)
            {
                //Skip first 1/4 data, we are keeping the middle chunk - this is overlap discard.
                int offset = chunk_size / 2;

                //Calculate virtual vfo
                Complex virtualVFO = Complex.FromPolarCoordinates(1.0, virtualVfoPhase);
                virtualVfoPhase -= 2.0 * Math.PI * virtualVfoFrequency * (1.0 / 48000.0);
                virtualVfoPhase = virtualVfoPhase % Math.Tau;

                //Run the product mixer
                mixernovfoOutput[i] = carrierIFFT[i + offset] * basebandIFFT[i + offset];
                mixerOutput[i] = carrierIFFT[i + offset] * basebandIFFT[i + offset] * virtualVFO;

                //Save the 90degree baseband
                basebandOutput[i] = basebandIFFT[i + offset];
            }

            sinknovfo.Write(mixernovfoOutput);
            sink.Write(mixerOutput);
            baseband90.Write(basebandOutput);
        }
        sink.SaveFile();
        sinknovfo.SaveFile();
        baseband90.SaveFile();
        Console.WriteLine("Done");
    }
}
