namespace solve_crozzle
{
	using crozzle;

	public class PartialWord
	{
		public Direction Direction;
		public string Value;
		public Rectangle Rectangle;
		public override bool Equals(object obj)
		{
			if (!(obj is PartialWord pw))
			{
				return false;
			}
			return this.Direction.Equals(pw.Direction)
				&& this.Value.Equals(pw.Value)
				&& this.Rectangle.Equals(pw.Rectangle);
		}
		public override int GetHashCode() =>
			Direction.GetHashCode()
			^ Value?.GetHashCode() ?? 0
			^ Rectangle?.GetHashCode() ?? 0;

		public PartialWord Move(Vector v) =>
			new PartialWord
			{
				Direction = this.Direction,
				Rectangle = this.Rectangle.Move(v),
				Value = this.Value
			};
	}
}
