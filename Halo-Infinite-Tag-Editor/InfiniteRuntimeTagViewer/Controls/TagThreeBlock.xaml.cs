namespace InfiniteRuntimeTagViewer.Interface.Controls
{
    /// <summary>
    /// Interaction logic for valueBlock.xaml
    /// </summary>
    public partial class TagThreeBlock
    {
		public TagThreeBlock(int labelWidth)
        {
            InitializeComponent();

            f_label1.Width = labelWidth;
            f_label2.Width = labelWidth;
            f_label3.Width = labelWidth;
        }
	}
}