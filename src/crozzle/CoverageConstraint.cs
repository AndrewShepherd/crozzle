namespace crozzle
{
	class CoverageConstraint
	{
		private readonly int _maxLocations = 4;

		public CoverageConstraint(int maxLocations)
		{
			_maxLocations = maxLocations;
		}

		public bool SatisfiesConstraint(GridRegion gridRegion) =>
			gridRegion.CountLocations() <= _maxLocations;
	}
}
