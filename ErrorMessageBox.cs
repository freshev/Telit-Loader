using System.Drawing;
using System.Windows.Forms;

namespace TelitLoader {
    public partial class ErrorMessageBox : Form {
        public ErrorMessageBox(string label, string caption = "Error") {
            InitializeComponent();
            label1.Font = new Font(FontFamily.GenericMonospace, label1.Font.SizeInPoints);
            label1.Text = label;
            Text = caption;
            SizeF size = TextRenderer.MeasureText(label, label1.Font);
            Size = new Size((int)size.Width + 50, (int)size.Height + 120);
            buttonRetry.Location = new Point(((int)size.Width + 50) / 2 - buttonRetry.Size.Width + 15, buttonRetry.Location.Y);
            buttonCancel.Location = new Point(((int)size.Width + 50) / 2 + 25, buttonCancel.Location.Y);
        }
    }
}
