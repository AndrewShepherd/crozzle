using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle_desktop
{
	class WordsSelectorViewModel : ViewModelBase
	{
		public string FileName { get; set; }

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
