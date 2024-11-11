using System.Windows.Forms;

namespace TelitLoader {
    public partial class CopyMessageBox : Form {
        public CopyMessageBox(string label) {
            InitializeComponent();
            label1.Text = label;
            buttonAll.Select();
        }
    }
}
