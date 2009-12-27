using System.Windows.Forms;

namespace XG.Client.TCP.WinForms
{
    public partial class TextDialog : Form
    {
        private DialogResult myResult;

        public TextDialog()
        {
            InitializeComponent();
        }

        public DialogResult ShowAsDialog(string aText)
        {
            this.myResult = DialogResult.Cancel;
            this.lblText.Text = aText;
            this.tbText.Text = "";
            this.ShowDialog();
            return this.myResult;
        }

        private void btnSave_Click(object sender, System.EventArgs e)
        {
            this.myResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.myResult = DialogResult.Cancel;
            this.Close();
        }

        public string GetInput()
        {
            return this.tbText.Text;
        }
    }
}
