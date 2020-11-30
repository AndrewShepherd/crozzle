﻿using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle_controls
{
	public class WordsSelectorViewModel : PropertyChangedEventSource
	{
		private IEnumerable<string> _words;
		public IEnumerable<string> Words
		{
			get => _words;
			set
			{
				if(this._words != value)
				{
					this._words = value;
					this.FirePropertyChangedEvents(nameof(Words));
				}
			}
		}
		public string FileName { get; internal set; }
	}
}
