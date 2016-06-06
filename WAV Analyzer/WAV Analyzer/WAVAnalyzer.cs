using AForge.Math;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAV_Analyzer {
  public class WAVAnalyzer {
    double[] file;
    byte[] filebytes;
    int Width, Height;
    WaveOutEvent waveOut;
    WaveFileReader wavreader;
    List<double> ampgraph = new List<double>();
    public WAVAnalyzer(Size size) {
      Width=size.Width;
      Height=size.Height;
      string filename = "..\\Mus_ruins.wav";
      file=prepare(filename);
      filebytes=File.ReadAllBytes(filename);
      waveOut=new WaveOutEvent();
      wavreader=new WaveFileReader(filename);
      for(int x = 0;x<Width;x++) {
        double max = 0;
        double total = 0;
        double count = 0;
        double szun = file.Length;
        szun/=Width;
        for(int y = (int)((x-1)*szun);y<x*szun;y++) {
          if(y<44)
            y=44;
          if(y>=file.Length)
            break;
          if(Math.Abs(file[y])>max)
            max=Math.Abs(file[y]);
          total+=Math.Abs(file[y]);
          count++;
        }
        total/=count;
        ampgraph.Add(max*255);
      }
      waveOut.Init(wavreader);
      waveOut.Play();
    }
    public void disp(Graphics g) {
      Pen p = new Pen(Color.FromArgb(255,220,0));
      SolidBrush b = new SolidBrush(Color.FromArgb(0,0,0));
      Pen p2 = new Pen(Color.FromArgb(255,0,0));
      Pen p3 = new Pen(Color.FromArgb(192,192,192));
      SolidBrush b2 = new SolidBrush(Color.FromArgb(255,0,0));
      Font f = new Font("Arial",12);
      g.FillRectangle(b,0,0,Width,Height);
      double curtime = waveOut.GetPosition();
      curtime*=Width;
      curtime/=file.Length*2;
      g.DrawLine(p2,(int)(curtime),Height/2+250,(int)(curtime),Height/2-250);
      for(int x = 0;x<ampgraph.Count();x++) {
        g.DrawLine(p,x,(float)(Height/2-ampgraph[x]),x,(float)(Height/2+ampgraph[x]));
      }
      curtime=waveOut.GetPosition();
      curtime/=2;
      curtime+=44;
      int FFTSize = 1024;
      Complex[] data = new Complex[FFTSize];
      for(int x = 0;x<FFTSize;x++) {
        if((int)(x+curtime+.5)<file.Length)
          data[x]=new Complex(file[(int)(x+curtime+.5)],0);
        else
          data[x]=new Complex(0,0);
      }
      FourierTransform.FFT(data,FourierTransform.Direction.Forward);
      List<string> lines = new List<string>();
      for(int x = 0;x<=FFTSize/2;x++) {
        float horiz = x*Width;
        horiz/=FFTSize/2;
        double value = Math.Sqrt(Math.Pow(data[x].Re,2)+Math.Pow(data[x].Im,2));
        g.DrawLine(p3,horiz,Height,horiz,(float)(Height-value*10000));
        lines.Add(Convert.ToString(x)+" :\t"+Convert.ToString(data[x].Re)+"\n\t\t"+Convert.ToString(data[x].Im)+"\n\t\t"+Convert.ToString(value));
      }
      //File.WriteAllLines("Debug.txt",lines.ToArray());
    }
    /*public static Double[] read(String wavePath) {
      Double[] data;

      return data;
    }*/
    public static Double[] prepare(String wavePath) {
      Double[] data;
      byte[] wave;
      byte[] sR = new byte[4];
      System.IO.FileStream WaveFile = System.IO.File.OpenRead(wavePath);
      wave=new byte[WaveFile.Length];
      data=new Double[(wave.Length-44)/2];//shifting the headers out of the PCM data;
      WaveFile.Read(wave,0,Convert.ToInt32(WaveFile.Length));//read the wave file into the wave variable
                                                             /***********Converting and PCM accounting***************/
      
      byte[] wavholder = new byte[2];
      for(int i = 0;i<data.Length;i++) {
        wavholder[0]=wave[i*2+44];
        wavholder[1]=wave[i*2+45];
        data[i]=BitConverter.ToInt16(wavholder,0)/32768.0;
      }
      //65536.0.0=2^n,       n=bits per sample;

      return data;
    }
  }
}
