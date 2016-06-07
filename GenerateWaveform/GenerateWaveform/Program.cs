﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateWaveform {
  public enum WaveExampleType {
    ExampleSineWave = 0
  }
  class Program {
    // Header, Format, Data chunks
    WaveHeader header;
    WaveFormatChunk format;
    WaveDataChunk data;

    /// <snip>
    /// 
    public Program(WaveExampleType type) {
      // Init chunks
      header=new WaveHeader();
      format=new WaveFormatChunk();
      data=new WaveDataChunk();

      // Fill the data array with sample data
      switch(type) {
        case WaveExampleType.ExampleSineWave:

          // Number of samples = sample rate * channels * bytes per sample
          uint numSamples = 30*format.dwSamplesPerSec*format.wChannels;

          // Initialize the 16-bit array
          data.shortArray=new short[numSamples];

          int amplitude = 16384/2;  // Max amplitude for 16-bit audio
          double freq = 440.0f;   // Concert A: 440Hz

          // The "angle" used in the function, adjusted for the number of channels and sample rate.
          // This value is like the period of the wave.
          double t = (Math.PI*2*freq)/(format.dwSamplesPerSec*format.wChannels);
          double t2= (Math.PI*2*freq*2)/(format.dwSamplesPerSec*format.wChannels);

          for(uint i = 0;i<numSamples-1;i++) {
            // Fill with a simple sine wave at max amplitude
            for(int channel = 0;channel<format.wChannels;channel++) {
              data.shortArray[i+channel]=Convert.ToInt16(amplitude*Math.Sin(t*i)+amplitude*Math.Sin(t2*i)/4);
            }
            uint sec = i/(format.dwSamplesPerSec*format.wChannels);
            if(sec==1)
              freq=466.164;
            if(sec==2)
              freq=493.883;
            if(sec==3)
              freq=523.251;
            if(sec==4)
              freq=554.365;
            if(sec==5)
              freq=587.330;
            if(sec==6)
              freq=622.254;
            t=(Math.PI*2*freq)/(format.dwSamplesPerSec*format.wChannels);
            t2 = (Math.PI*2*freq*2)/(format.dwSamplesPerSec*format.wChannels);
          }

          // Calculate data chunk size in bytes
          data.dwChunkSize=(uint)(data.shortArray.Length*(format.wBitsPerSample/8));

          break;
      }
    }
    public void Save(string filePath) {
      // Create a file (it always overwrites)
      FileStream fileStream = new FileStream(filePath,FileMode.Create);
      // Use BinaryWriter to write the bytes to the file
      BinaryWriter writer = new BinaryWriter(fileStream);

      // Write the header
      writer.Write(header.sGroupID.ToCharArray());
      writer.Write(header.dwFileLength);
      writer.Write(header.sRiffType.ToCharArray());

      // Write the format chunk
      writer.Write(format.sChunkID.ToCharArray());
      writer.Write(format.dwChunkSize);
      writer.Write(format.wFormatTag);
      writer.Write(format.wChannels);
      writer.Write(format.dwSamplesPerSec);
      writer.Write(format.dwAvgBytesPerSec);
      writer.Write(format.wBlockAlign);
      writer.Write(format.wBitsPerSample);

      // Write the data chunk
      writer.Write(data.sChunkID.ToCharArray());
      writer.Write(data.dwChunkSize);
      foreach(short dataPoint in data.shortArray) {
        writer.Write(dataPoint);
      }

      writer.Seek(4,SeekOrigin.Begin);
      uint filesize = (uint)writer.BaseStream.Length;
      writer.Write(filesize-8);

      // Clean up
      writer.Close();
      fileStream.Close();
    }
    static void Main(string[] args) {
      Program p = new Program(WaveExampleType.ExampleSineWave);
      p.Save("test440.wav");
    }
  }
}
