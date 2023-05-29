namespace VirtualVFO;
using System.Text;

public class WavSource
{
    FileStream file;
    int bytesLeft;
    public WavSource(string filename)
    {
        file = new FileStream(filename, FileMode.Open);
        SeekToStart();
    }

    public void SeekToStart()
    {
        byte[] b4 = new byte[4];
        file.Seek(8, SeekOrigin.Begin);
        file.Read(b4, 0, 4);
        string wavHeader = Encoding.ASCII.GetString(b4, 0, 4);
        if (wavHeader != "WAVE")
        {
            throw new KeyNotFoundException($"Unsupported wav file {wavHeader}");
        }
        bool dataFound = false;
        while (!dataFound)
        {
            file.Read(b4, 0, 4);
            string chunkHeader = Encoding.ASCII.GetString(b4, 0, 4);
            file.Read(b4, 0, 4);
            int chunkSize = BitConverter.ToInt32(b4);
            if (chunkHeader == "data")
            {
                bytesLeft = chunkSize;
                dataFound = true;
            }
            else
            {
                file.Seek(chunkSize, SeekOrigin.Current);
            }
        }
    }

    public double[] Read(int samples)
    {
        if (samples > bytesLeft / 2)
        {
            return null;
        }
        bytesLeft -= samples * 2;
        double[] sampleDecode = new double[samples];
        for (int i = 0; i < samples; i++)
        {
            int lsb = file.ReadByte();
            int msb = file.ReadByte();
            short s = (short)((msb << 8) + lsb);
            sampleDecode[i] = s / (double)short.MaxValue;
        }
        return sampleDecode;
    }

    public int GetProgress()
    {
        return (int)((100 * file.Position) / file.Length);
    }
}