
namespace crozzle
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;

	public class Board: IComparable<Board>
	{
		public static int MaxWidth = 17;
		public static int MaxHeight = 12;
		public Rectangle Rectangle = Rectangle.Empty;
		public ImmutableSortedSet<WordPlacement> WordPlacements = ImmutableSortedSet<WordPlacement>.Empty;

		public Board()
		{
			 this._values = new Lazy<char[]>(GenerateValues);
		}

		public static Board Empty()
		{
			return new Board();
		}

		private char[] GenerateValues()
		{
			char[] values = new char[this.Rectangle.Area];
			Func<int, int> moveUp = n => n - +this.Rectangle.Width,
				moveDown = n => n + this.Rectangle.Width,
				moveLeft = n => n - 1,
				moveRight = n => n + 1;
			foreach(var wordplacement in this.WordPlacements)
			{
				(Func<int, int> forward, Func<int, int> back) =
					wordplacement.Direction == Direction.Across
					? (moveRight, moveLeft)
					: (moveDown, moveUp);
				int gridLocation = this.Rectangle.IndexOf(wordplacement.Location);
				values[back(gridLocation)] = '*';
				for(
					int i = 0; i < wordplacement.Word.Length; 
					++i,
					gridLocation = forward(gridLocation)
				)
				{
					values[gridLocation] = wordplacement.Word[i];
				}
				values[gridLocation] = '*';
			}
			return values;
		}

		private readonly Lazy<char[]> _values;

		public char[] Values => _values.Value;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _values.Value.Length; ++i)
			{
				sb.Append((Values[i] == (char)0) || (Values[i] == '*') ? '_' : Values[i]);
				if (i % Rectangle.Width == Rectangle.Width - 1)
				{
					sb.AppendLine();
				}
			}
			return sb.ToString();
		}

		public override bool Equals(object? obj)
		{
			return (obj is Board b)
				&& b.Rectangle.Equals(this.Rectangle)
				&& b.WordPlacements.SetEquals(this.WordPlacements);
		}

		public override int GetHashCode()
		{
			var hash = Rectangle.GetHashCode();
			int i = 0;
			foreach (var wp in this.WordPlacements)
			{
				hash ^= wp.GetHashCode().RotateLeft((++i) % 32);
			}
			return hash;
		}

		public Board Move(Vector v) =>
			new Board
			{
				Rectangle = this.Rectangle.Move(v),
				WordPlacements = ImmutableSortedSet.CreateRange<WordPlacement>(
					this.WordPlacements.Select(
						wp => wp.Move(v)
					)
				)
			};

		public int CompareTo(Board? other)
		{
			if(other == null)
			{
				return -1;
			}
			var c1 = this.WordPlacements.Count().CompareTo(other.WordPlacements.Count());
			if (c1 != 0)
			{
				return c1;
			}
			foreach(
				var t in this.WordPlacements.Zip(
					other.WordPlacements, 
					(_1, _2) => _1.CompareTo(_2)
				)
			)
			{
				if(t != 0)
				{
					return t;
				}
			}
			return 0;
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

		public static Func<Location, Location> Move(int dx, int dy) =>
			l => new Location(l.X + dx, l.Y + dy);

	
		public static Board PlaceWord(this Board board, WordPlacement wordPlacement) =>
			new Board
			{
				Rectangle = board.Rectangle,
				WordPlacements = board.WordPlacements.Add(
					wordPlacement
				)
			};

		public static Board ExpandSize(this Board board, Rectangle newRectangle) =>
			new Board
			{
				WordPlacements = board.WordPlacements,
				Rectangle = board.Rectangle.Union(newRectangle)
			};
	}
}
