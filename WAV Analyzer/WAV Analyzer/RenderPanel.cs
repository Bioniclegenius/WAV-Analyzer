using System.Drawing;
using System.Windows.Forms;

namespace WAV_Analyzer
{
    public class RenderPanel : Panel
    {
        public WAVAnalyzer wav;
        public RenderPanel(Size sz)
        {
            Width = sz.Width;
            Height = sz.Height;
            wav = new WAVAnalyzer(sz);
            Location = new Point(0, 0);
            DoubleBuffered = true;
            Paint += new PaintEventHandler(PaintEvent);
            MouseMove += new MouseEventHandler(mouseMove);
            MouseClick += new MouseEventHandler(mouseClick);
            Invalidate();
        }
        private void PaintEvent(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            wav.disp(g);
            Invalidate();
        }
        private void mouseMove(object sender, MouseEventArgs m)
        {
            wav.mouseMove(new Point(m.X, m.Y));
        }
        private void mouseClick(object sender, MouseEventArgs m)
        {
            wav.seek(new Point(m.X, m.Y));
        }
        public void setVolume(int vol)
        {
            wav.setVolume(vol);
        }
        public bool playPause()
        {
            return wav.playPause();
        }
    }
}
