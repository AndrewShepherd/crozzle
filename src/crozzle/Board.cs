﻿
namespace crozzle
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;

	public class Board : IComparable<Board>
	{
		public static int MaxWidth = 17;
		public static int MaxHeight = 12;
		public Rectangle Rectangle;
		public ImmutableSortedSet<WordPlacement> WordPlacements = ImmutableSortedSet<WordPlacement>.Empty;

		public Board()
		{
			 this._values = new Lazy<char[]>(GenerateValues);
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

		private static bool AreEqual(char[] v1, char[] v2)
		{
			if(v1.Length != v2.Length)
			{
				return false;
			}
			for(int i = 0; i < v1.Length; ++i)
			{				
				if(v1[i] != v2[i])
				{
					return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
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

		public int CompareTo(Board other)
		{
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

		public static Func<Location, Location> MoveLeft =>
			Move(-1, 0);

		public static Func<Location, Location> MoveRight =>
			Move(1, 0);

		public static Func<Location, Location> MoveUp =>
			Move(0, -1);

		public static Func<Location, Location> MoveDown =>
			Move(0, 1);

		public static PartialWord GetContiguousTextAt(this Board board, Direction direction, Location location)
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

		public static Board PlaceWord(this Board board, Direction direction, Location location, string word) =>
			new Board
			{
				Rectangle = board.Rectangle,
				WordPlacements = board.WordPlacements.Add(
					new WordPlacement(
						direction,
						location,
						word
					)
				)
			};

		public static bool CanPlaceWord(this Board board, Direction direction, string word, Location location)
		{
			var wordPlacement = new WordPlacement(direction, location, word);
			var r = wordPlacement.GetRectangle()
				.Union(board.Rectangle);
			if ((r.Width > Board.MaxWidth) || (r.Height > Board.MaxHeight))
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
				
				var l = direction == Direction.Down
					? new Location(location.X, location.Y + i)
					: new Location(location.X + i, location.Y);
				var c = board.CharAt(l);
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

		public static Board ExpandSize(this Board board, Rectangle newRectangle) =>
			new Board
			{
				WordPlacements = board.WordPlacements,
				Rectangle = board.Rectangle.Union(newRectangle)
			};
	}
}