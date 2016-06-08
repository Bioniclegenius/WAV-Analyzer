using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateWaveform {
  public enum WaveExampleType {
    ExampleSineWave = 0,
    Music = 1
  }
  class Program {
    // Header, Format, Data chunks
    WaveHeader header;
    WaveFormatChunk format;
    WaveDataChunk data;

    /// <snip>
    /// 
    public Program(WaveExampleType type,string file = "") {
      // Init chunks
      header=new WaveHeader();
      format=new WaveFormatChunk();
      data=new WaveDataChunk();
      Dictionary<char,int> scale = new Dictionary<char,int>(){
                              { 'a',1 },{ 'A',2 },{ 'b',3 },{ 'B',4 },{ 'c',4 },{ 'C',5 },{ 'd',6 },
                              { 'D',7 },{ 'e',8 },{ 'E',9 },{ 'f',9 },{'F',10 },{'g',11 },{'G',12 }
      };

      // Fill the data array with sample data
      if(type==WaveExampleType.ExampleSineWave) {//---Ignore this part, this was the initial program I copied---

        // Number of samples = sample rate * channels * bytes per sample
        uint numSamp = format.dwSamplesPerSec*format.wChannels;

        // Initialize the 16-bit array
        data.shortArray=new short[numSamp];

        int amplitude = 32768/16;  // Max amplitude for 16-bit audio
        double frequency = 440.0f;   // Concert A: 440Hz

        // The "angle" used in the function, adjusted for the number of channels and sample rate.
        // This value is like the period of the wave.
        double tpos = (Math.PI*2*frequency)/(format.dwSamplesPerSec*format.wChannels);

        for(uint i = 0;i<numSamp-1;i++) {
          // Fill with a simple sine wave at max amplitude
          for(int channel = 0;channel<format.wChannels;channel++) {
            data.shortArray[i+channel]=Convert.ToInt16(amplitude*Math.Sin(tpos*i));
          }
        }

        // Calculate data chunk size in bytes
        data.dwChunkSize=(uint)(data.shortArray.Length*(format.wBitsPerSample/8));

      }
      else if(type==WaveExampleType.Music){//---This is where all the magic happens, for file reading and whatnot---
        string[] lines = File.ReadAllLines(file);
        // Number of samples = sample rate * channels * bytes per sample
        double length = 0;
        double tempo = 1;
        bool lastlineread = false;
        bool foundtempo = false;
        for(int x = 0;x<lines.Length;x++) {
          if(lines[x].Length>=8) {
            if(lines[x].Substring(0,7)==";tempo ") {
              tempo=Convert.ToDouble(lines[x].Substring(7));
              foundtempo=true;
            }
            else
              foundtempo=false;
          }
          else
            foundtempo=false;
          if(lines[x]!=""&&!foundtempo) {
            if(!lastlineread) {
              for(int y = 0;y<lines[x].Length;y++) {
                if(lines[x][y]==';')
                  break;
                double amount = 1;
                amount/=tempo;
                length+=amount;
                lastlineread=true;
              }
            }
          }
          else if(lines[x]=="")
            lastlineread=false;
        }
        uint numSamples = (uint)(length*format.dwSamplesPerSec*format.wChannels);
        tempo=1;
        int line = 0;
        int pos = 0;
        uint lastsamplejump = 0;

        // Initialize the 16-bit array
        data.shortArray=new short[numSamples];

        int amp = 32768/16;  // Max amplitude for 16-bit audio

        // The "angle" used in the function, adjusted for the number of channels and sample rate.
        // This value is like the period of the wave.
        double t = 0;
        char lastnote = '.';
        byte octave = 2;

        for(uint i = 0;i<numSamples-1;i++) {
          if(lines[line].Length>=8) {
            while(lines[line].Substring(0,7)==";tempo ") {//tempo line, read and go to the next
              tempo=Convert.ToDouble(lines[line].Substring(7));
              line++;
              pos=0;
              if(line>=lines.Length)
                break;
              if(lines[line].Length<8)
                break;
            }
          }
          while(lines[line]=="") {//empty line, skip it
            line++;
            pos=0;
            if(line>=lines.Length)
              break;
          }
          if(line>=lines.Length)
            break;
          if(i-lastsamplejump>=1/tempo*format.dwSamplesPerSec*format.wChannels&&//moving to the next character
             i-1-lastsamplejump<1/tempo*format.dwSamplesPerSec*format.wChannels) {
            pos++;
            lastsamplejump=i;
            if(pos>=lines[line].Length) {
              line++;
              pos=0;
            }
          }
          if(line>=lines.Length)
            break;
          if(lines[line].Length>=8) {
            while(lines[line].Substring(0,7)==";tempo ") {//tempo line, read and go to the next
              tempo=Convert.ToDouble(lines[0].Substring(7));
              line++;
              pos=0;
              if(line>=lines.Length)
                break;
              if(lines[line].Length<8)
                break;
            }
          }
          while(lines[line]=="") {//empty line, skip it
            line++;
            pos=0;
            if(line>=lines.Length)
              break;
          }
          if(line>=lines.Length)
            break;
          if(scale.ContainsKey(lines[line][pos])) {
            if(pos<lines[line].Length-1) {
              if(lines[line][pos+1]>='0'&&lines[line][pos+1]<='9')
                octave=(Byte)((int)(lines[line][pos+1])-48);
            }
            lastnote=lines[line][pos];
            int notenum = scale[lastnote];
            notenum+=12*octave;
            double freq = numtofreq(notenum);
            t=(Math.PI*2*freq)/(format.dwSamplesPerSec*format.wChannels);
          }
          if(lines[line][pos]>='0'&&lines[line][pos]<='9') {
            octave=(Byte)((int)(lines[line][pos])-48);
            if(lastnote!='.') {
              int notenum = scale[lastnote];
              notenum+=12*octave;
              double freq = numtofreq(notenum);
              t=(Math.PI*2*freq)/(format.dwSamplesPerSec*format.wChannels);
            }
          }
          if(lines[line][pos]=='.') {
            lastnote='.';
            t=0;
          }
          for(int channel = 0;channel<format.wChannels;channel++) {
            data.shortArray[i+channel]=Convert.ToInt16(amp*Math.Sin(t*i));
          }
        }

        // Calculate data chunk size in bytes
        data.dwChunkSize=(uint)(data.shortArray.Length*(format.wBitsPerSample/8));

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
    double numtofreq(int notenumber) {
      double pow = notenumber-49;
      pow/=12;
      double freq = Math.Pow(2,pow);
      freq*=440;
      return freq;
    }
    static void Main(string[] args) {
      Console.WriteLine("Generating file...");
      Program p = new Program(WaveExampleType.Music,"music.txt");
      Console.WriteLine("Saving file...");
      p.Save("..\\..\\..\\..\\WAV Analyzer\\WAV Analyzer\\bin\\Music\\test440.wav");
      Console.WriteLine("Done.");
    }
  }
}
