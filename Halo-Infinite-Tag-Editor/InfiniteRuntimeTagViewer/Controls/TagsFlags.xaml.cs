using System.Windows;
using System.Windows.Controls;
using Halo_Infinite_Tag_Editor;
using InfiniteRuntimeTagViewer.Halo;

namespace InfiniteRuntimeTagViewer.Interface.Controls
{
    public partial class TagsFlags : UserControl
    {
        public TagsFlags()
        {
            InitializeComponent();
            flag1.Tag = this;
            flag2.Tag = this;
            flag3.Tag = this;
            flag4.Tag = this;
            flag5.Tag = this;
            flag6.Tag = this;
            flag7.Tag = this;
            flag8.Tag = this;
        }
    }
}
