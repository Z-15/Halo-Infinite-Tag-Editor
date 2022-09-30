using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Halo_Infinite_Tag_Editor;
using InfiniteRuntimeTagViewer.Halo;
using InfiniteRuntimeTagViewer.Halo.TagObjects;

namespace InfiniteRuntimeTagViewer.Interface.Controls
{
    public partial class TagBlock
    {
		public KeyValuePair<long, TagLayouts.C> Children { get; set; }
		public long BlockAddress { get; set; }
		public IRTV_TagStruct TagStruct { get; set; }
		public int stored_num_on_index = -1;
		public bool checkExpanded;
		public Dictionary<int, bool> childDataBlock = new();
		public int dataBlockInd = 0;
		public int size = 0;

		public TagBlock(IRTV_TagStruct tagStruct)
		{
			InitializeComponent();
			TagStruct = tagStruct;
		}

        private void indexbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			if (indexbox.SelectedIndex > -1)
			{
				stored_num_on_index = indexbox.SelectedIndex;
                Halo_Infinite_Tag_Editor.MainWindow.instance.BuildTagBlock(TagStruct, Children, this, Children.Value.AbsoluteTagOffset);
                checkExpanded = true;
				Expand_Collapse_Button.Content = "-";
			}
			else
			{
				dockpanel.Children.Clear();
			}
		}

		private void Expand_Collapse_Button_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			checkExpanded = !checkExpanded;
			if (checkExpanded)
			{
				indexbox.SelectedIndex = stored_num_on_index;
				Expand_Collapse_Button.Content = "-";
			}
			else
			{
				indexbox.SelectedIndex = -1;
				Expand_Collapse_Button.Content = "+";
			}
		}
	}
}
