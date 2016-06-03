using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WAV_Analyzer {
  public partial class Form1:Form {
    public Form1() {
      InitializeComponent();
    }

    private void Form1_Load(object sender,EventArgs e) {
      this.ClientSize=new Size(800,600);
      RenderPanel rp = new RenderPanel(this.ClientSize);
      this.Controls.Add(rp);
    }
  }
}
