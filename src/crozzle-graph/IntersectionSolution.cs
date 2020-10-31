namespace crozzle_graph
{
	using System.Collections.Immutable;
	using crozzle;

	public class IntersectionSolution
	{
		public ImmutableHashSet<Intersection> Intersections
		{ 
			get;
			set;
		} = ImmutableHashSet<Intersection>.Empty;

		public ImmutableHashSet<Intersection> ExcludedIntersections
		{
			get;
			set;
		} = ImmutableHashSet<Intersection>.Empty;
	}
}
