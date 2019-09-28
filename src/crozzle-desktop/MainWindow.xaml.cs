using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace crozzle_desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		MainWindowViewModel MainWindowViewModel => (MainWindowViewModel)(_mainGrid.DataContext);

		private async void FileOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if(ofd.ShowDialog() == true)
			{
				List<string> words = new List<string>();
				using (StreamReader sr = new StreamReader(ofd.FileName))
				{
					while(!sr.EndOfStream)
					{
						var line = await sr.ReadLineAsync();
						if(!String.IsNullOrWhiteSpace(line))
						{
							words.Add(line.Trim());
						}
					}
				}
				this.MainWindowViewModel.Words = words;

			}
		}
	}
}
