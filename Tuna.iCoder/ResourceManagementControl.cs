using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tuna.iCoder
{
    public partial class ResourceManagementControl : UserControl
    {
        public ResourceManagementControl()
        {
            InitializeComponent();
            treeView1.ExpandAll();
        }
    }
}
