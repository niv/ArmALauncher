using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArmALauncher
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            this.Text = fi.Name;
        }

        public void Log(string text)
        {
            text = DateTime.Now.ToString("HH:mm:ss") + "> " + text;
            Console.WriteLine(text);
            logBox.AppendText(text + "\n");
        }
    }
}
