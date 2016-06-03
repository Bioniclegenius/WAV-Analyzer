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
    public WAVAnalyzer(Size size) {
      Width=size.Width;
      Height=size.Height;
      string filename = "..\\12_-_Mabe_Village.wav";
      file=prepare(filename);
      filebytes=File.ReadAllBytes(filename);
      waveOut=new WaveOutEvent();
      wavreader=new WaveFileReader(filename);
      waveOut.Init(wavreader);
      waveOut.Play();
    }
    public void disp(Graphics g) {
      Pen p = new Pen(Color.FromArgb(255,220,0));
      SolidBrush b = new SolidBrush(Color.FromArgb(0,0,0));
      Pen p2 = new Pen(Color.FromArgb(255,0,0));
      SolidBrush b2 = new SolidBrush(Color.FromArgb(255,0,0));
      Font f = new Font("Arial",12);
      g.FillRectangle(b,0,0,Width,Height);
      double curtime = waveOut.GetPosition();
      curtime*=Width;
      curtime/=file.Length*4;
      g.DrawLine(p2,(int)(curtime),Height/2+250,(int)(curtime),Height/2-250);
      for(int x = 0;x<Width;x++) {
        double max = 0;
        double total = 0;
        double count = 0;
        for(int y = (x-1)*file.Length/Width;y<x*file.Length/Width;y++) {
          if(y<0)
            y=0;
          if(y>=file.Length)
            break;
          if(Math.Abs(file[y])>max)
            max=Math.Abs(file[y]);
          total+=Math.Abs(file[y]);
          count++;
        }
        total/=count;
        total*=5;
        g.DrawLine(p,x,(int)(Height/2+max*255),x,(int)(Height/2-max*255));
        //if(x==(int)(curtime+.5))
        g.DrawString(Convert.ToString(filebytes[34]),f,b2,new PointF(5,5));
      }
    }
    public static Double[] prepare(String wavePath) {
      Double[] data;
      byte[] wave;
      byte[] sR = new byte[4];
      System.IO.FileStream WaveFile = System.IO.File.OpenRead(wavePath);
      wave=new byte[WaveFile.Length];
      data=new Double[(wave.Length-44)/4];//shifting the headers out of the PCM data;
      WaveFile.Read(wave,0,Convert.ToInt32(WaveFile.Length));//read the wave file into the wave variable
                                                             /***********Converting and PCM accounting***************/
      for(int i = 0;i<data.Length;i++) {
        data[i]=BitConverter.ToInt16(wave,i*2)/32768.0;
      }
      //65536.0.0=2^n,       n=bits per sample;

      return data;
    }
  }
}
