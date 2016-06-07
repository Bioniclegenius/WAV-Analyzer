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
    List<PointF> ampgraph = new List<PointF>();
    short numchannels;
    int samplerate;
    short bitspersample;
    int minfreq = 20;
    int maxfreq = 13289;
    double lowscale = .75;
    double highscale = 17;
    int firstbin, lastbin;
    int FFTSize;
    int numbernotes;
    public WAVAnalyzer(Size size) {
      Width=size.Width;
      Height=size.Height;

      //put files here, choose which one ot use
      string[] config = File.ReadAllLines("config.txt");


      string filename = "..\\Music\\"+config[1]+".wav";
      minfreq=Convert.ToInt32(config[4]);
      maxfreq=Convert.ToInt32(config[6]);
      int pow=Convert.ToInt32(config[9]);//keep between 9 and 11 for best results, 13 at max
      lowscale=Convert.ToDouble(config[12]);
      highscale=Convert.ToDouble(config[14]);
      FFTSize=(int)(Math.Pow(2,pow));
      filebytes=File.ReadAllBytes(filename);
      waveOut=new WaveOutEvent();
      wavreader=new WaveFileReader(filename);
      file=prepare(filename);
      for(int x = 0;x<FFTSize;x++) {
        double freq = x*samplerate;
        freq/=FFTSize;
        double prevfreq = (x-1)*samplerate;
        prevfreq/=FFTSize;
        if(prevfreq<minfreq&&freq>=minfreq)
          firstbin=x;
        if(prevfreq<=maxfreq&&freq>maxfreq)
          lastbin=x;
      }
      if(lastbin>FFTSize/2)
        lastbin=FFTSize/2;
      double highfreq = lastbin*samplerate;
      highfreq/=FFTSize;
      numbernotes=(int)(12*Math.Log(highfreq/440.0)/Math.Log(2)+49);
      double filemax = 0;
      for(int x = 0;x<Width;x++) {
        double max = 0;
        double min = 0;
        double totalpos = 0;
        double totalneg = 0;
        double countpos = 0;
        double countneg = 0;
        double szun = file.Length;
        szun/=Width;
        for(int y = (int)((x-1)*szun);y<x*szun;y++) {
          if(y<0)
            y=0;
          if(y>=file.Length)
            break;
          if(file[y]>max)
            max=file[y];
          if(file[y]<min)
            min=file[y];
          if(Math.Abs(file[y])>filemax)
            filemax=Math.Abs(file[y]);
          if(file[y]>0) {
            totalpos+=file[y];
            countpos++;
          }
          if(file[y]<0) {
            totalneg-=file[y];
            countneg++;
          }
        }
        totalpos/=countpos;
        totalneg/=countneg;
        double mult = Height/2;
        mult/=filemax;
        //ampgraph.Add(new PointF((float)(max*mult),(float)(-min*mult)));
        ampgraph.Add(new PointF((float)(totalpos*mult),(float)(totalneg*mult)));
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
        g.DrawLine(p,x,(float)(Height/2-ampgraph[x].X),x,(float)(Height/2+ampgraph[x].Y));
      }
      curtime=waveOut.GetPosition();
      curtime/=2;
      curtime+=44;
      Complex[] data = new Complex[FFTSize];
      for(int x = 0;x<FFTSize;x++) {
        if((int)(x+curtime+.5)<file.Length)
          data[x]=new Complex(file[(int)(x+curtime+.5)],0);
        else
          data[x]=new Complex(0,0);
      }
      g.DrawString(Convert.ToString(lastbin)+" "+Convert.ToString(FFTSize/2),f,b2,5,5);
      for(int x = 0;x<FFTSize/2;x+=100) {
      }
      FourierTransform.FFT(data,FourierTransform.Direction.Forward);
      List<string> lines = new List<string>();
      for(int x = firstbin;x<=lastbin;x++) {
        float horiz = x*Width;
        horiz/=lastbin-firstbin;
        //new horiz calc
        double freq = x*samplerate;
        freq/=FFTSize;
        double numkey = 12*Math.Log(freq/440.0)/Math.Log(2)+49;
        horiz=(float)(numkey*Width);
        horiz/=numbernotes;
        double value = Math.Sqrt(Math.Pow(data[x].Re,2)+Math.Pow(data[x].Im,2));
        double scalemult = highscale-lowscale;
        scalemult/=lastbin-firstbin;
        double offset = lowscale-firstbin*scalemult;
        g.DrawLine(p3,horiz,Height,horiz,(float)(Height-value*10000*(x*scalemult+offset)));
        //lines.Add(Convert.ToString(x)+" :\t"+Convert.ToString(data[x].Re)+"\n\t\t"+Convert.ToString(data[x].Im)+"\n\t\t"+Convert.ToString(value));
      }
      //File.WriteAllLines("Debug.txt",lines.ToArray());
    }
    public Double[] prepare(String wavePath) {
      Double[] data;
      byte[] wave;
      byte[] sR = new byte[4];
      numchannels = BitConverter.ToInt16(filebytes,22);
      samplerate=BitConverter.ToInt32(filebytes,24);
      bitspersample=BitConverter.ToInt16(filebytes,34);
      System.IO.FileStream WaveFile = System.IO.File.OpenRead(wavePath);
      wave=new byte[WaveFile.Length];
      data=new Double[(wave.Length-44)/numchannels];//shifting the headers out of the PCM data;
      WaveFile.Read(wave,0,Convert.ToInt32(WaveFile.Length));//read the wave file into the wave variable
                                                             /***********Converting and PCM accounting***************/
      
      byte[] wavholder = new byte[2];
      for(int i = 0;i<data.Length;i++) {
        wavholder[0]=wave[i*numchannels+44];
        wavholder[1]=wave[i*numchannels+45];
        data[i]=BitConverter.ToInt16(wavholder,0)/Math.Pow(2,bitspersample);
      }
      //65536.0.0=2^n,       n=bits per sample;

      return data;
    }
  }
}
