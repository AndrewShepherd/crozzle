namespace crozzle_graph
{
	using System.Collections.Immutable;
	using crozzle;

	public class IntersectionSolution
	{
		internal Workspace Workspace { get; set; } = Workspace.Empty;
		internal ImmutableHashSet<Intersection> Intersections
		{ 
			get;
			set;
		} = ImmutableHashSet<Intersection>.Empty;
	}
}
