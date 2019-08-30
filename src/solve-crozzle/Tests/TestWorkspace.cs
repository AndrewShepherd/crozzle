using NUnit.Framework;
using System.Linq;
using crozzle;

namespace solve_crozzle.Tests
{
	[TestFixture]
	public class TestWorkspace
	{

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
		public void TestOneWordEqual()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			var w1 = workspace.PlaceWord(Direction.Across, "Apple", 0, 0);
			var w2 = workspace.PlaceWord(Direction.Across, "Apple", 0, 0);
			Assert.That(w1.GetHashCode(), Is.EqualTo(w2.GetHashCode()));
			Assert.That(w1, Is.EqualTo(w2));


		}

		[Test]
		public void TestDetectAdjacencies()
		{
			var workspace = Workspace.Generate(new[] { "A", "B", "CAB" });
			var workspaceOne = workspace.PlaceWord(Direction.Across, "A", 3, 3);
			var workspaceTwo = workspaceOne.PlaceWord(Direction.Across, "B", 3, 4);
			Assert.That(workspaceTwo.PartialWords.Count, Is.EqualTo(1));
			var partialWord = workspaceTwo.PartialWords.First();
			Assert.That(partialWord.Value, Is.EqualTo("AB"));
			Assert.That(partialWord.Direction, Is.EqualTo(Direction.Down));
			Assert.That(partialWord.Rectangle.TopLeft.X, Is.EqualTo(3));
			Assert.That(partialWord.Rectangle.TopLeft.Y, Is.EqualTo(3));
			var nextSteps = workspaceTwo.GenerateNextSteps().ToList();
			Assert.That(nextSteps, Has.Count.EqualTo(1));
		}

		[Test]
		public void TwoWordEquals()
		{
			var workspace = Workspace.Generate(new[] { "A", "B", "CAB" });
			var w1 = workspace.PlaceWord(Direction.Across, "A", 3, 3)
				.PlaceWord(Direction.Across, "B", 3, 4)
				.Normalise();
			var w2 = workspace
				.PlaceWord(Direction.Across, "B", 3, 4)
				.PlaceWord(Direction.Across, "A", 3, 3)
				.Normalise();
			Assert.That(
				w1.WordDatabase.GetHashCode(),
				Is.EqualTo(w2.WordDatabase.GetHashCode())
			);
			Assert.That(
				HashUtils.GenerateHash(w1.PartialWords),
				Is.EqualTo(HashUtils.GenerateHash(w2.PartialWords))
			);
			Assert.That(
				HashUtils.GenerateHash(w1.Slots),
				Is.EqualTo(HashUtils.GenerateHash(w2.Slots))
			);
			Assert.That(w1.GetHashCode(), Is.EqualTo(w2.GetHashCode()));
			Assert.That(w1, Is.EqualTo(w2));
		}

		[Test]
		public void TestDetectAdjacenciesAcross()
		{
			var workspace = Workspace.Generate(new[] { "SUEZ", "ZURICH", "QUITO" });
			workspace = workspace.PlaceWord(Direction.Down, "SUEZ", 0, 0);
			workspace = workspace.PlaceWord(Direction.Across, "ZURICH", 0, 3);
			workspace = workspace.PlaceWord(Direction.Down, "QUITO", 1, 2);
			Assert.That(workspace.PartialWords.Count, Is.EqualTo(1));
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
