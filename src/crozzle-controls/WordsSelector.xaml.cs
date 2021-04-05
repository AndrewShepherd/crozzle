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
using crozzle;

namespace crozzle_controls
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
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


		private static string Sanitize(string word) =>
			new string(
				word.ToUpper()
					.Where(c => Char.IsLetter(c))
					.ToArray()
				);

		public IEnumerable<string> Words
		{
			get => (IEnumerable<string>)((DependencyObject)this).GetValue(WordsDependencyProperty);
			set
			{
				var currentWords = this.Words;
				if (object.ReferenceEquals(currentWords, value))
				{
					return;
				}
				currentWords = value?.Select(Sanitize).ToArray();
				this.ViewModel.Words = currentWords;
				this.SetValue(WordsDependencyProperty, currentWords);
			}
		}

		private async void FileOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (ofd.ShowDialog() == true)
			{
				using (var stream = File.OpenRead(ofd.FileName))
				{
					this.Words = await WordStreamReader.Read(stream);
				}
				this.ViewModel.FileName = System.IO.Path.GetFileName(ofd.FileName);
			}
		}
	}
}