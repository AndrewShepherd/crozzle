using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle_desktop
{
	class WordsSelectorViewModel : PropertyChangedEventSource
	{
		private string _fileName = "No file selected";
		public string FileName 
		{
			get => _fileName;
			set
			{
				if(this._fileName != value)
				{
					this._fileName = value;
					FirePropertyChangedEvents(nameof(FileName));
				}
			}
		}

		private IEnumerable<string> _words;
		public IEnumerable<string> Words
		{
			get => _words;
			set
			{
				if(this._words != value)
				{
					this._words = value;
					FirePropertyChangedEvents(nameof(Words));
				}
			}
		}
	}
}
