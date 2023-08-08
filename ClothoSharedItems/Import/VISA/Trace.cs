using System;
using System.Windows.Forms;

namespace ClothoSharedItems.Import.VISA
{
    public class Trace
    {
        private RichTextBox trace = (RichTextBox)null;

        public Trace(RichTextBox trace)
        {
            this.trace = trace;
        }

        public void Message(string message)
        {
            if (this.trace == null) return;
            this.trace.Invoke(new Action(() => this.trace.AppendText(Environment.NewLine + message)));
        }
    }
}