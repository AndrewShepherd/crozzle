using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Board
	{
		public Rectangle Rectangle;
		public char[] Values;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Values.Length; ++i)
			{
				sb.Append((Values[i] == (char)0) || (Values[i] == '*') ? '_' : Values[i]);
				if (i % Rectangle.Width == Rectangle.Width - 1)
				{
					sb.AppendLine();
				}
			}
			return sb.ToString();
		}
	}

	public static class BoardExtensions
	{

		public static Location CalculateLocation(Rectangle rectangle, int index) =>
			rectangle.CalculateLocation(index);

		public static int IndexOf(this Board board, Location location) =>
			board.Rectangle.IndexOf(location);

		public static Board ExpandSize(this Board board, Rectangle newRectangle)
		{
			var currentRectangle = board.Rectangle;
			var rectangle = currentRectangle.Union(newRectangle);
			var newArray = new char[rectangle.Width * rectangle.Height];
			if (currentRectangle.Equals(rectangle))
			{
				Array.Copy(
					board.Values,
					newArray,
					newArray.Length
				);
			}
			else
			{
				var originalLocation = currentRectangle.CalculateLocation(0);
				var destIndex = rectangle.IndexOf(originalLocation);
				for (
					int sourceIndex = 0;
					sourceIndex < board.Values.Length;
					sourceIndex += currentRectangle.Width, destIndex += rectangle.Width
				)
				{
					Array.Copy(
						board.Values,
						sourceIndex,
						newArray,
						destIndex,
						currentRectangle.Width
					);
				}
			}
			return new Board
			{
				Rectangle = rectangle,
				Values = newArray
			};

		}
	}
}
