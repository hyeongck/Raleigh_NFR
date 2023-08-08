using System.Runtime.InteropServices;
using System.Text;

namespace ClothoSharedItems.Import.GPIB
{
    public static class GpibAPI32
    {
        [DllImport("Gpib-32.dll")]
        public static extern int ibfind(string udname);

        [DllImport("Gpib-32.dll")]
        public static extern int ibsic(int ud);

        [DllImport("Gpib-32.dll")]
        public static extern int ibdev(int boardID, int pad, int sad, int tmo, int eot, int eos);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibwrt(int ud, string buf, int cnt);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibwrta(int ud, string buf, int cnt);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibrd(int ud, StringBuilder buf, int cnt);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibrd(int ud, byte[] buf, int cnt);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibrda(int ud, StringBuilder buf, int cnt);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibclr(int ud);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibloc(int ud);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibtmo(int ud, int tmo);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibln(int ud, int pad, int sad, out short listen);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ibrsp(int ud, ref byte spr);

        [DllImport("Gpib-32.dll")]
        public static extern GpibStatus ThreadIbsta();

        [DllImport("Gpib-32.dll")]
        public static extern GpibError ThreadIberr();

        [DllImport("Gpib-32.dll")]
        public static extern int ThreadIbcnt();
    }

    public static class GpibAPI64
    {
        [DllImport("ni4882.dll")]
        public static extern int ibfind(string udname);

        [DllImport("ni4882.dll")]
        public static extern int ibsic(int ud);

        [DllImport("ni4882.dll")]
        public static extern int ibdev(int boardID, int pad, int sad, int tmo, int eot, int eos);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibwrt(int ud, string buf, int cnt);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibwrta(int ud, string buf, int cnt);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibrd(int ud, StringBuilder buf, int cnt);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibrd(int ud, byte[] buf, int cnt);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibrda(int ud, StringBuilder buf, int cnt);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibclr(int ud);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibloc(int ud);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibconfig(int ud, int option, int value);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibtmo(int ud, int tmo);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibln(int ud, int pad, int sad, out short listen);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ibrsp(int ud, ref byte spr);

        [DllImport("ni4882.dll")]
        public static extern GpibStatus ThreadIbsta();

        [DllImport("ni4882.dll")]
        public static extern GpibError ThreadIberr();

        [DllImport("ni4882.dll")]
        public static extern int ThreadIbcnt();
    }
}