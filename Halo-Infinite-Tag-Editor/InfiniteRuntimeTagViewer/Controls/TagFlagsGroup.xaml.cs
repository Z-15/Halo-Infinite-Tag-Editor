using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Halo_Infinite_Tag_Editor;
using InfiniteRuntimeTagViewer.Halo;

namespace InfiniteRuntimeTagViewer.Interface.Controls

{
    /// <summary>
    /// Interaction logic for TagFlagsGroup.xaml
    /// </summary>
    public partial class TagFlagsGroup : UserControl
    {
		public static readonly RoutedEvent FlagToggledEvent = EventManager.RegisterRoutedEvent(
			name: "FlagToggled",
			routingStrategy: RoutingStrategy.Bubble,
			handlerType: typeof(RoutedEventHandler),
			ownerType: typeof(UserControl));

		public event RoutedEventHandler FlagToggled
		{
			add { AddHandler(FlagToggledEvent, value); }
			remove { RemoveHandler(FlagToggledEvent, value); }
		}

		void RaiseCustomRoutedEvent()
		{
			RoutedEventArgs routedEventArgs = new(routedEvent: FlagToggledEvent);
			RaiseEvent(routedEventArgs);
		}
        public byte[] data;
		public long startAddress;
        public int? amountOfBytes;
        public int? maxBit;

        public TagFlagsGroup()
        {
            InitializeComponent();
        }

		public void generateBitsFromFile(int amountOfBytes, int maxBit, byte[] data, Dictionary<int, string>? descriptions = null)
		{
			this.data = data;
			this.amountOfBytes = amountOfBytes;
			this.maxBit = maxBit;

			if (maxBit == 0)
			{
				maxBit = maxBit = amountOfBytes * 8;
			}

			spBitCollection.Children.Clear();

			int maxAmountOfBytes = Math.Clamp((int) Math.Ceiling((double) maxBit / 8), 0, amountOfBytes);
			int bitsLeft = maxBit - 1; // -1 to start at 

			for (int @byte = 0; @byte < maxAmountOfBytes; @byte++)
			{
				if (bitsLeft < 0)
				{
					continue;
				}

				int amountOfBits = @byte * 8 > maxBit ? ((@byte * 8) - maxBit) : 8;
				byte flags_value = (byte) data[@byte];

				for (int bit = 0; bit < amountOfBits; bit++)
				{
					int currentBitIndex = (@byte * 8) + bit;
					if (bitsLeft < 0)
					{
						continue;
					}

					CheckBox? checkbox = null;

					int _byte = @byte, _bit = bit;

					checkbox = new CheckBox();
					checkbox.Margin = new System.Windows.Thickness(5, 0, 0, 0);
					checkbox.IsChecked = flags_value.GetBit(bit);
					checkbox.Checked += (s, e) => Checkbox_BitIsChanged(_byte, _bit);
					checkbox.Unchecked += (s, e) => Checkbox_BitIsChanged(_byte, _bit);

					checkbox.Content =
						descriptions != null && descriptions.ContainsKey(currentBitIndex)
						? descriptions[(@byte * 8) + bit] : "Flag " + (currentBitIndex);

					checkbox.ToolTip = new TextBlock()
					{
						Foreground = Brushes.White,
						FontFamily = new FontFamily("Arabic Transparent"),
						FontSize = 12,
						Text = $"Flag Bit {currentBitIndex}, Addr = :^{bit}"
					};

					if (checkbox != null)
					{
						spBitCollection.Children.Add(checkbox);
					}

					bitsLeft--;
				}
			}
		}

		

		private void Checkbox_BitIsChanged(int byteNo, int bit)
        {
			byte output = 0;

			for (int x = 0; x < 8; x++)
            {
                int index = (byteNo * 8) + x;
                if (spBitCollection.Children.Count < index)
				{
					continue;
				}

				CheckBox? cbx = (CheckBox)spBitCollection.Children[index];
				output.UpdateBit(x, value: (bool)cbx.IsChecked);
			}

			data[byteNo] = output;


			RaiseCustomRoutedEvent();
        }
    }
}
