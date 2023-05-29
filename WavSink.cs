namespace VirtualVFO;
using System.IO;
using System.Text;
using System.Numerics;

public class WavSink
{
    MemoryStream ms;
    string filename;
    byte[] b4 = new byte[4];
    public WavSink(string filename)
    {
        this.filename = filename;
        ms = new MemoryStream();
    }

    public void SaveFile()
    {
        if (File.Exists(filename))
        {
            File.Delete(filename);
        }
        using (FileStream fs = new FileStream(filename, FileMode.Create))
        {
            //RIFF header
            Encoding.ASCII.GetBytes("RIFF", 0, 4, b4, 0);
            fs.Write(b4, 0, 4);
            int dataSize = (int)ms.Length;
            int fileSize = dataSize + 36;
            b4 = BitConverter.GetBytes(fileSize);
            fs.Write(b4, 0, 4);
            //WAVE header
            Encoding.ASCII.GetBytes("WAVE", 0, 4, b4, 0);
            fs.Write(b4, 0, 4);
            //fmt chunk
            Encoding.ASCII.GetBytes("fmt ", 0, 4, b4, 0);
            fs.Write(b4, 0, 4);
            b4 = BitConverter.GetBytes(16);
            fs.Write(b4, 0, 4);
            //Type 1 PCM
            fs.WriteByte(1);
            fs.WriteByte(0);
            //Number of channels: Stereo
            fs.WriteByte(2);
            fs.WriteByte(0);
            //Sample Rate
            b4 = BitConverter.GetBytes(48000);
            fs.Write(b4, 0, 4);
            //Sample rate * Bytes per sample * Channels
            b4 = BitConverter.GetBytes(48000 * 2 * 2);
            fs.Write(b4, 0, 4);
            //Bytes per sample * Channels
            fs.WriteByte(4);
            fs.WriteByte(0);
            //Bits per sample
            fs.WriteByte(16);
            fs.WriteByte(0);
            //data chunk
            Encoding.ASCII.GetBytes("data", 0, 4, b4, 0);
            fs.Write(b4, 0, 4);
            b4 = BitConverter.GetBytes(dataSize);
            fs.Write(b4, 0, 4);
            fs.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fs);
            ms = new MemoryStream();
        }
    }

    public void Write(Complex[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            short leftSample = (short)(samples[i].Real * short.MaxValue);
            short rightSample = (short)(samples[i].Imaginary * short.MaxValue);
            //Left LSB, Left MSB, Right LSB, Right MSB
            b4[0] = (byte)(leftSample & 0xFF);
            b4[1] = (byte)((leftSample >> 8) & 0xFF);
            b4[2] = (byte)(rightSample & 0xFF);
            b4[3] = (byte)((rightSample >> 8) & 0xFF);
            ms.Write(b4, 0, 4);
        }
    }
}