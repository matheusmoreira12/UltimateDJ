using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        UltimateDJ.Controls.Turntable turntable = new UltimateDJ.Controls.Turntable();

        private void Form1_Load(object sender, EventArgs e)
        {
            Controls.Add(turntable);
            turntable.ForeColor = Color.Red;
            turntable.Maximum = 10000;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            turntable.Value += 12;
        }

        private void bpmIndicator1_Click(object sender, EventArgs e)
        {

        }
    }
}
