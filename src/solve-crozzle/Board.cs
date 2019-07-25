using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Board
	{
		public int MaxWidth = 17;
		public int MaxHeight = 12;


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

		public static char CharAt(this Board board, Location location)
		{
			if (!(board.Rectangle.Contains(location)))
			{
				return (char)0;
			}
			int index = board.Rectangle.IndexOf(location);
			return index < board.Values.Length ? board.Values[index] : (char)0;
		}

		public static Rectangle GetRectangleForWord(Direction direction, string word, int x, int y) =>
			new Rectangle(
				new Location(
					x - (direction == Direction.Across ? 1 : 0),
					y - (direction == Direction.Down ? 1 : 0)
				),
				direction == Direction.Across ? word.Length + 2 : 1,
				direction == Direction.Down ? word.Length + 2 : 1
			);

		public static Func<Location, Location> Move(int dx, int dy) =>
			l => new Location(l.X + dx, l.Y + dy);

		public static Func<Location, Location> MoveLeft =>
			Move(-1, 0);

		public static Func<Location, Location> MoveRight =>
			Move(1, 0);

		public static Func<Location, Location> MoveUp =>
			Move(0, -1);

		public static Func<Location, Location> MoveDown =>
			Move(0, 1);



		public static PartialWord GetWordAt(this Board board, Direction direction, Location location)
		{
			(var back, var forward) = direction == Direction.Across
				? (MoveLeft, MoveRight)
				: (MoveUp, MoveDown);
			Location start = location;
			while (Char.IsLetter(board.CharAt(back(start))))
			{
				start = back(start);
			}
			List<char> list = new List<char>();
			list.Add(board.CharAt(start));
			Location end = start;

			while (Char.IsLetter(board.CharAt(forward(end))))
			{
				end = forward(end);
				list.Add(board.CharAt(end));
			}
			return new PartialWord
			{
				Direction = direction,
				Value = new string(list.ToArray()),
				Rectangle = new Rectangle(start, end)
			};
		}

		public static bool CanPlaceWord(this Board board, Direction direction, string word, Location location)
		{
			var r = GetRectangleForWord(direction, word, location.X, location.Y)
				.Union(board.Rectangle);
			if ((r.Width > board.MaxWidth) || (r.Height > board.MaxHeight))
				return false;

			(
				var startMarkerLocation,
				var endMarkerLocation
			) = direction == Direction.Across
			? (new Location(location.X - 1, location.Y), new Location(location.X + word.Length, location.Y))
			: (new Location(location.X, location.Y - 1), new Location(location.X, location.Y + word.Length));

			var startMarker = board.CharAt(startMarkerLocation);
			var endMarker = board.CharAt(endMarkerLocation);

			if (!((startMarker == '*') || (startMarker == (char)0)))
				return false;
			if (!((endMarker == '*') || (endMarker == (char)0)))
				return false;

			for (int i = 0; i < word.Length; ++i)
			{
				var c = direction == Direction.Down
					? board.CharAt(new Location(location.X, location.Y + i))
					: board.CharAt(new Location(location.X + i, location.Y));
				if (c != (char)0)
				{
					if (word[i] != c)
					{
						return false;
					}

				}
			}
			return true;
		}



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
