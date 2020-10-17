using crozzle;
using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle_desktop
{
	public static class GeneratorNames
	{
		public const string CoverSlots = "Cover Slots";
		public const string FillSpace = "Fill Space";
	}

	public enum PositioningBehavior
	{
		Dynamic,
		Fixed
	};

	class AlgorithmSettingsViewModel : PropertyChangedEventSource
	{
		public string[] AvailableGeneratorNames =>
			new[]
			{
				GeneratorNames.CoverSlots,
				GeneratorNames.FillSpace,
			};

		private string _currentGenerator = GeneratorNames.CoverSlots;
		public string CurrentGenerator
		{
			get => _currentGenerator;
			set
			{
				if(_currentGenerator != value)
				{
					_currentGenerator = value;
					FirePropertyChangedEvents(nameof(CurrentGenerator));
				}
			}
		}

		private PositioningBehavior _positioningBehavior;
		public PositioningBehavior PositioningBehavior
		{
			get => _positioningBehavior;
			set
			{
				if(_positioningBehavior != value)
				{
					_positioningBehavior = value;
					FirePropertyChangedEvents(nameof(PositioningBehavior));
				}
			}
		}

		internal INextStepGenerator CreateGenerator()
		{
			if(_currentGenerator == GeneratorNames.CoverSlots)
			{
				int minAdjacentGroupSize = 2; // Should be set by control
				return new SlotFillingNextStepGenerator(minAdjacentGroupSize); 
			}
			else
			{
				return new SpaceFillingNextStepGenerator(
					new SpaceFillingGenerationSettings
					{
						MaxContiguousSpaces = 3
					}
				);
			}
		}
	}
}
