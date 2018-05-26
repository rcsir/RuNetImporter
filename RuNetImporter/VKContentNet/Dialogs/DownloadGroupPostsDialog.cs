using System;
using System.Windows.Forms;

namespace rcsir.net.vk.content.Dialogs
{
    public partial class DownloadGroupPostsDialog : Form
    {
        public decimal GroupId { get; set; }
        public bool IsGroup { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Boolean GroupWall { get; set; }
        public Boolean GroupTopics { get; set; }

        public DownloadGroupPostsDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            FromDate = dateTimeFromPicker.Value;
            ToDate = dateTimeToPicker.Value;
            GroupWall = groupWall.Checked;
            GroupTopics = groupTopics.Checked;
        }

        private void DownloadGroupPostsDialog_Load(object sender, EventArgs e)
        {
            groupIdNumeric.Value = GroupId;
            isGroupcheckBox.Checked = IsGroup;

            dateTimeFromPicker.Value = new DateTime(2000, 1, 1); // start from new millennium
            dateTimeToPicker.Value = DateTime.Today.AddDays(1);
        }

        private void dateTimeToPicker_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
