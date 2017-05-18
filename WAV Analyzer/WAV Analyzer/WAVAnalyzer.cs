using AForge.Math;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WAV_Analyzer
{
    public class WAVAnalyzer
    {
        int Width, Height;
        WaveOutEvent waveOut;
        WaveFileReader wavreader;
        Stream wavstream;
        List<PointF> ampgraph = new List<PointF>();
        List<PointF> ampgraphCopy = new List<PointF>();
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
        FileHandler fh;
        byte[] fileHeader;
        Thread calcAmp;
        string progress;
        string curProgress;
        object progressLock = new object();
        int lastcount;
        Point m;
        int initTime;
        bool pause;
        public void calcAmpGraph()
        {
            double filemax = 0;
            for (int x = 0; x < Width; x++)
            {
                double max = 0;
                double min = 0;
                double totalpos = 0;
                double totalneg = 0;
                double countpos = 0;
                double countneg = 0;
                double szun = fh.doubleLength;
                szun /= Width;
                for (int y = (int)((x - 1) * szun); y < x * szun; y++)
                {
                    if (y < 0)
                        y = 0;
                    if (y >= fh.doubleLength)
                        break;
                    double val = fh.getDoubleOffset(y);
                    if (val > max)
                        max = (float)val;
                    if (val < min)
                        min = (float)val;
                    if (Math.Abs(val) > filemax)
                        filemax = Math.Abs(val);
                    if (val > 0)
                    {
                        totalpos *= countpos / (countpos + 1);
                        totalpos += val / (countpos + 1);
                        countpos++;
                    }
                    if (val < 0)
                    {
                        totalneg *= countneg / (countneg + 1);
                        totalneg -= val / (countneg + 1);
                        countneg++;
                    }
                }
                lock (progressLock)
                {
                    progress = string.Format("{0}/{1} - {2}%", x, Width, (int)(10000 * x / Width) / 100.0);
                }
                lock (ampgraph)
                {
                    ampgraph.Add(new PointF((float)totalpos, (float)totalneg));
                }
            }
            float highestpoint = 0;
            lock (ampgraph)
            {
                for (int x = 0; x < ampgraph.Count(); x++)
                {
                    if (ampgraph[x].X > highestpoint)
                        highestpoint = ampgraph[x].X;
                    if (ampgraph[x].Y < -1 * highestpoint)
                        highestpoint = -1 * ampgraph[x].Y;
                }
            }
            float mult = Height / 8;
            mult /= highestpoint;
            lock (ampgraph)
            {
                for (int x = 0; x < ampgraph.Count(); x++)
                {
                    ampgraph[x] = new PointF(ampgraph[x].X * mult, ampgraph[x].Y * mult);
                }
            }
        }
        public WAVAnalyzer(Size size)
        {
            Width = size.Width;
            Height = size.Height;

            //put files here, choose which one ot use
            string[] config = File.ReadAllLines("config.txt");

            string filename = "..\\Music\\" + config[1] + ".wav";
            minfreq = Convert.ToInt32(config[4]);
            maxfreq = Convert.ToInt32(config[6]);
            int pow = Convert.ToInt32(config[9]);//keep between 9 and 11 for best results, 13 at max
            lowscale = Convert.ToDouble(config[12]);
            highscale = Convert.ToDouble(config[14]);
            FFTSize = (int)(Math.Pow(2, pow));
            waveOut = new WaveOutEvent();
            wavstream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fh = new FileHandler(filename);
            wavreader = new WaveFileReader(wavstream);
            prepare(filename);
            m = new Point(-1, -1);
            lock (progressLock)
            {
                progress = "";
                curProgress = progress;
            }
            lastcount = 0;
            initTime = 0;
            pause = false;
            for (int x = 0; x < FFTSize; x++)
            {
                double freq = x * samplerate;
                freq /= FFTSize;
                double prevfreq = (x - 1) * samplerate;
                prevfreq /= FFTSize;
                if (prevfreq < minfreq && freq >= minfreq)
                    firstbin = x;
                if (prevfreq <= maxfreq && freq > maxfreq)
                    lastbin = x;
            }
            if (lastbin > FFTSize / 2)
                lastbin = FFTSize / 2;
            double highfreq = lastbin * samplerate;
            highfreq /= FFTSize;
            calcAmp = new Thread(new ThreadStart(calcAmpGraph));
            calcAmp.IsBackground = true;
            calcAmp.Start();
            numbernotes = (int)(12 * Math.Log(highfreq / 440.0) / Math.Log(2) + 49);
            waveOut.Init(wavreader);
            waveOut.Play();
        }
        public string getTime(long pos)
        {
            int milliseconds = (int)(1000 * pos / fh.getInt(28));
            int seconds = milliseconds / 1000;
            milliseconds %= 1000;
            int minutes = seconds / 60;
            seconds %= 60;
            int hours = minutes / 60;
            minutes %= 60;
            DateTime now = DateTime.Now;
            DateTime length = new DateTime(now.Year, now.Month, now.Day, hours, minutes, seconds, milliseconds);
            string time = "";
            if (hours != 0)
                time = length.ToString("HH:mm:ss");
            else
                time = length.ToString("mm:ss");
            return time;
        }
        public void mouseMove(Point mouseCoords)
        {
            m = mouseCoords;
        }
        public void seek(Point mouseCoords)
        {
            m = mouseCoords;
            if (m.X >= 0 && m.X < Width && m.Y >= 0 && m.Y < Height)
            {
                waveOut.Stop();
                int newPos = (int)((1.0 * m.X) / Width * (fh.fileInfo.Length) + .5);
                newPos -= newPos % ((int)(bitspersample / 8 * samplerate / 1000.0));
                initTime = newPos;
                wavstream.Seek(newPos, SeekOrigin.Begin);
                if(!pause)
                    waveOut.Play();
            }
        }
        public void setVolume(int vol)
        {
            waveOut.Volume = (float)(vol / 100.0);
        }
        public bool playPause()
        {
            pause = !pause;
            initTime = (int)(waveOut.GetPosition() + initTime);
            return pause;
        }
        public void disp(Graphics g)
        {
            if (!pause && waveOut.PlaybackState != PlaybackState.Playing)
                waveOut.Play();
            else if (pause && waveOut.PlaybackState == PlaybackState.Playing)
                waveOut.Stop();
            Pen p = new Pen(Color.FromArgb(255, 220, 0));
            SolidBrush b = new SolidBrush(Color.FromArgb(0, 0, 0));
            Pen p2 = new Pen(Color.FromArgb(255, 0, 0));
            Pen p3 = new Pen(Color.FromArgb(192, 192, 192));
            SolidBrush b2 = new SolidBrush(Color.FromArgb(255, 0, 0));
            Font f = new Font("Arial", 12);
            g.FillRectangle(b, 0, 0, Width, Height);
            double curtime = waveOut.GetPosition() + initTime;
            curtime *= Width;
            curtime /= fh.doubleLength * 2;
            g.DrawLine(p2, (int)(curtime), Height / 2 + 250, (int)(curtime), Height / 2 - 250);
            if (calcAmp.IsAlive)
            {
                if (Monitor.TryEnter(ampgraph))
                {
                    if (ampgraph.Count() != lastcount)
                    {
                        lastcount = ampgraph.Count();
                        ampgraphCopy = new List<PointF>(ampgraph);
                        float highestpoint = 0;
                        for (int x = 0; x < ampgraphCopy.Count(); x++)
                        {
                            if (ampgraphCopy[x].X > highestpoint)
                                highestpoint = ampgraphCopy[x].X;
                            if (ampgraphCopy[x].Y < -1 * highestpoint)
                                highestpoint = -1 * ampgraphCopy[x].Y;
                        }
                        float mult = Height / 8;
                        mult /= highestpoint;
                        for (int x = 0; x < ampgraphCopy.Count(); x++)
                        {
                            ampgraphCopy[x] = new PointF(ampgraphCopy[x].X * mult, ampgraphCopy[x].Y * mult);
                        }
                    }
                    Monitor.Exit(ampgraph);
                }
                if (Monitor.TryEnter(progressLock))
                {
                    curProgress = progress;
                    Monitor.Exit(progressLock);
                }
                for (int x = 0; x < ampgraphCopy.Count(); x++)
                {
                    g.DrawLine(p, x, (float)(Height / 2 - ampgraphCopy[x].X), x, (float)(Height / 2 + ampgraphCopy[x].Y));
                }
                g.DrawString(curProgress, f, b2, 5, 25);
            }
            else
            {
                for (int x = 0; x < ampgraph.Count(); x++)
                {
                    g.DrawLine(p, x, (float)(Height / 2 - ampgraph[x].X), x, (float)(Height / 2 + ampgraph[x].Y));
                }
            }/**/
            g.DrawString(string.Format("{0} / {1}", getTime(waveOut.GetPosition() + initTime), getTime(fh.fileInfo.Length - 44)), f, b2, 5, 5);
            if (m.X >= 0 && m.X <= Width)
            {
                string goaltime = getTime((int)((1.0 * m.X) / Width * (fh.fileInfo.Length - 44) + .5));
                g.DrawString(goaltime, f, b2, Width - g.MeasureString(goaltime, f).Width - 5, 5);
                g.DrawLine(p3, m.X, Height / 2 + 250, m.X, Height / 2 - 250);
            }
            curtime = waveOut.GetPosition() + initTime;
            curtime /= 2;
            curtime += 44;
            Complex[] data = new Complex[FFTSize];
            for (int x = 0; x < FFTSize; x++)
            {
                if ((int)(x + curtime + .5) < fh.doubleLength)
                    data[x] = new Complex(fh.getDoubleOffset((int)(x + curtime + .5)), 0);
                else
                    data[x] = new Complex(0, 0);
            }
            FourierTransform.FFT(data, FourierTransform.Direction.Forward);
            for (int x = firstbin; x <= lastbin; x++)
            {
                float horiz = x * Width;
                horiz /= lastbin - firstbin;
                //new horiz calc
                double freq = x * samplerate;
                freq /= FFTSize;
                double numkey = 12 * Math.Log(freq / 440.0) / Math.Log(2) + 49;
                horiz = (float)(numkey * Width);
                horiz /= numbernotes;
                horiz = Width * x / lastbin;
                double value = Math.Sqrt(Math.Pow(data[x].Re, 2) + Math.Pow(data[x].Im, 2));
                double scalemult = highscale - lowscale;
                scalemult /= lastbin - firstbin;
                double offset = lowscale - firstbin * scalemult;
                g.DrawLine(p3, horiz, Height, horiz, (float)(Height - value * 10000 * (x * scalemult + offset)));
            }
            /*Complex[] lowData = new Complex[FFTSize * 2];
            for (int x = 0; x < FFTSize * 2; x++)
            {
                if ((int)(x + curtime + .5) < fh.doubleLength)
                    lowData[x] = new Complex(fh.getDoubleOffset((int)(x + curtime + .5)), 0);
                else
                    lowData[x] = new Complex(0, 0);
            }
            FourierTransform.FFT(lowData, FourierTransform.Direction.Forward);
            for (double x = firstbin; x <= lastbin/2; x+=0.5)
            {
                float horiz = (float)(x * Width);
                horiz /= lastbin - firstbin;
                //new horiz calc
                double freq = x * samplerate;
                freq /= FFTSize;
                double numkey = 12 * Math.Log(freq / 440.0) / Math.Log(2) + 49;
                horiz = (float)(numkey * Width);
                horiz /= numbernotes;
                //horiz = Width * x / lastbin;
                double value = Math.Sqrt(Math.Pow(lowData[(int)(x*2)].Re, 2) + Math.Pow(lowData[(int)(x*2)].Im, 2));
                double scalemult = highscale - lowscale;
                scalemult /= lastbin - firstbin;
                double offset = lowscale - firstbin * scalemult;
                g.DrawLine(p3, horiz, Height, horiz, (float)(Height - value * 10000 * (x * scalemult + offset)));
            }*/
        }
        public void prepare(string wavePath)
        {
            numchannels = fh.getShort(22);
            samplerate = fh.getInt(24);
            bitspersample = fh.getShort(34);
            fileHeader = fh.getBytes(0, 44);
        }
    }
}
