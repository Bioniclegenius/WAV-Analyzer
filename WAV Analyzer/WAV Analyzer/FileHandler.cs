using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAV_Analyzer
{
    class FileHandler
    {
        public FileInfo fileInfo;
        public long doubleLength;
        private Stream filestream;
        private short bitspersample;
        public FileHandler(string fname)
        {
            newFile(fname);
        }
        public void newFile(string filename)
        {
            fileInfo = new FileInfo(filename);
            doubleLength = (fileInfo.Length - 44) / 2;
            filestream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            bitspersample = getShort(34);
        }
        public byte[] getBytes(long start, int length)
        {
            byte[] output = new byte[length];
            lock (filestream)
            {
                filestream.Seek(start, SeekOrigin.Begin);
                filestream.Read(output, 0, length);
            }
            return output;
        }
        public byte getByte(long start)
        {
            byte[] output = getBytes(start, 1);
            return output[0];
        }
        public short getShort(long start)
        {
            byte[] output = getBytes(start, 2);
            return BitConverter.ToInt16(output, 0);
        }
        public int getInt(long start)
        {
            byte[] output = getBytes(start, 4);
            return BitConverter.ToInt32(output, 0);
        }
        public double getDouble(long start)
        {
            byte[] output = getBytes(start, 2);
            return BitConverter.ToInt16(output, 0);
        }
        public double getDoubleOffset(long start)
        {
            start *= 2;
            start += 44;
            return getDouble(start) / Math.Pow(2, bitspersample);
        }
        public long getLength()
        {
            return fileInfo.Length;
        }
    }
}
