using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WAV_Analyzer {
  public class RenderPanel:Panel {
    WAVAnalyzer wav;
    public RenderPanel(Size sz) {
      this.Width=sz.Width;
      this.Height=sz.Height;
      wav=new WAVAnalyzer(sz);
      this.Location=new Point(0,0);
      this.DoubleBuffered=true;
      this.Paint+=new System.Windows.Forms.PaintEventHandler(this.PaintEvent);
      Invalidate();
    }
    private void PaintEvent(object sender,PaintEventArgs e) {
      Graphics g = e.Graphics;
      wav.disp(g);
      Invalidate();
    }
  }
}
