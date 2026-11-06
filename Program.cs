using System;
using System.Windows.Forms;
using System.Threading;

namespace GVPN
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new VpnTrayApplication());
        }
    }
}
