using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
	/// Interaction logic for WordsSelector.xaml
	/// </summary>
	public partial class WordsSelector : UserControl
	{
		public static DependencyProperty WordsDependencyProperty = DependencyProperty.Register(
			nameof(WordsSelector.Words),
			typeof(IEnumerable<string>),
			typeof(WordsSelector),
			new FrameworkPropertyMetadata
			{
			}
		);
		public WordsSelector()
		{
			InitializeComponent();
		}

		private WordsSelectorViewModel ViewModel => (WordsSelectorViewModel)(this.Resources["ViewModel"]);

		public IEnumerable<string> Words
		{
			get => (IEnumerable<string>)((DependencyObject)this).GetValue(WordsDependencyProperty);
			set
			{
				var currentWords = this.Words;
				if(object.ReferenceEquals(currentWords, value))
				{
					return;
				}
				if((value != null) && (currentWords?.SequenceEqual(value) == true))
				{
					return;
				}
				this.ViewModel.Words = value;
				this.SetValue(WordsDependencyProperty, value);
			}
		}

		private async void FileOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (ofd.ShowDialog() == true)
			{
				List<string> words = new List<string>();
				using (StreamReader sr = new StreamReader(ofd.FileName))
				{
					while (!sr.EndOfStream)
					{
						var line = await sr.ReadLineAsync();
						if (!String.IsNullOrWhiteSpace(line))
						{
							words.Add(line.Trim());
						}
					}
				}
				this.Words = words;
				this.ViewModel.FileName = System.IO.Path.GetFileName(ofd.FileName);
			}
		}

	}
}
