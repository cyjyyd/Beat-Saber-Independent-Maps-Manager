using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    public partial class SettingsForm : Form
    {
        Config config = new Config();
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            checkBoxHashCache.Checked = config.HashCache;
            checkBoxMemFolder.Checked = config.LastFolder;
            checkBoxProxyDownload.Checked = config.DownProxy;
            checkBoxSaverCache.Checked = config.LocalCaChe;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            config.HashCache = checkBoxHashCache.Checked;
            config.LocalCaChe = checkBoxSaverCache.Checked;
            config.DownProxy = checkBoxProxyDownload.Checked;
            config.LastFolder = checkBoxMemFolder.Checked;
            config.configUpdate();
        }
    }
}
