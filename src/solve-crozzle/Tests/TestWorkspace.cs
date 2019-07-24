using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle.Tests
{
	[TestFixture]
	public class TestWorkspace
	{
		[Test]
		public void TestCanAdd()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			Assert.That(workspace.CanPlaceWord(Direction.Across, "Apple", 0, 0), Is.True);
		}

		[Test]
		public void TestOneWord()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			workspace = workspace.PlaceWord(Direction.Across, "Apple", 0, 0);
			Assert.That(workspace.Board.Rectangle.TopLeft.X, Is.EqualTo(-1));
			Assert.That(workspace.Board.Rectangle.TopLeft.Y, Is.EqualTo(0));
			Assert.That(workspace.Board.Rectangle.Width, Is.EqualTo(7));
			Assert.That(workspace.Board.Values.Length, Is.EqualTo("*Apple*".Length));
			Assert.That(workspace.Board.ToString(), Is.EqualTo("_Apple_\r\n"));
		}

		[Test]
		public void TestDetectAdjacencies()
		{
			var workspace = Workspace.Generate(new[] { "A", "B", "CAB" });
			workspace = workspace.PlaceWord(Direction.Across, "A", 3, 3);
			workspace = workspace.PlaceWord(Direction.Across, "B", 3, 4);
			Assert.That(workspace.PartialWords.Count, Is.EqualTo(1));
			var partialWord = workspace.PartialWords.First();
			Assert.That(partialWord.Value, Is.EqualTo("AB"));
			Assert.That(partialWord.Direction, Is.EqualTo(Direction.Down));
			Assert.That(partialWord.Location.X, Is.EqualTo(3));
			Assert.That(partialWord.Location.Y, Is.EqualTo(3));
			var nextSteps = workspace.GenerateNextSteps().ToList();
			Assert.That(nextSteps, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestExpand()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			var rectangles = new[]
			{
				new Rectangle(
					new Location(-1, 0),
					7,
					1
				),
				new Rectangle(
					new Location(-1, -4),
					6,
					6
				),
				new Rectangle(
					new Location(-3, -4),
					8,
					5
				)
			};
			var currentRectangle = workspace.GetCurrentRectangle();
			Assert.That(currentRectangle.Height, Is.EqualTo(0));
			workspace = workspace.ExpandSize(rectangles[0]);
			currentRectangle = workspace.GetCurrentRectangle();
			Assert.That(currentRectangle.Height, Is.EqualTo(1));
			Assert.That(workspace.Board.Values.Length, Is.EqualTo(currentRectangle.Area));
			workspace = workspace.ExpandSize(rectangles[1]);
			currentRectangle = workspace.GetCurrentRectangle();
			Assert.That(workspace.Board.Values.Length, Is.EqualTo(currentRectangle.Area));
			workspace = workspace.ExpandSize(rectangles[2]);
		}
	}
}
