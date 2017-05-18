using System;
using System.Drawing;
using System.Windows.Forms;

namespace WAV_Analyzer
{
    public partial class Form1 : Form
    {
        private RenderPanel rp;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ClientSize = new Size(800, 600);
            rp = new RenderPanel(ClientSize);
            Controls.Add(rp);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            rp.setVolume(trackBar1.Value);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            bool paused=rp.playPause();
            if (paused)
                pictureBox1.Image = Properties.Resources.Play;
            else
                pictureBox1.Image = Properties.Resources.Pause;
            trackBar1.Focus();
        }
    }
}
