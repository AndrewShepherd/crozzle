namespace crozzle
{
	public record PartialWord
	{
		public Direction Direction { get; init; }
		public string? Value { get; init; }
		public Rectangle? Rectangle { get; init; }
	}
}
