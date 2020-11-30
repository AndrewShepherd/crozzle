using crozzle;
using NUnit.Framework;
using System.Linq;

namespace crozzle_tests
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void TestOneWord()
		{
			Workspace? workspace = Workspace.Generate(new[] { "Apple" });
			workspace = workspace.PlaceWord(Direction.Across, "Apple", 0, 0);
			Assert.That(workspace, Is.Not.Null);
			if(workspace == null)
			{
				throw new System.Exception("Assertion failed");
			}
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
			Workspace? workspaceOne = workspace.PlaceWord(Direction.Across, "A", 3, 3);
			var workspaceTwo = workspaceOne?.PlaceWord(Direction.Across, "B", 3, 4);
			Assert.That(workspaceTwo.IsValid, Is.False);
			INextStepGenerator generator = new SpaceFillingNextStepGenerator(
					new SpaceFillingGenerationSettings { MaxContiguousSpaces = int.MaxValue }
				);
			var nextSteps = generator.GenerateNextSteps(workspaceTwo).ToList();
			Assert.That(nextSteps, Has.Count.EqualTo(1));
			Assert.That(nextSteps.First().IsValid, Is.True);
		}

		[Test]
		public void TwoWordEquals()
		{
			var workspace = Workspace.Generate(new[] { "A", "B", "CAB" });
			var w1 = workspace.PlaceWord(Direction.Across, "A", 3, 3);
			Assert.That(w1.IsValid, Is.True);
			w1 = w1.PlaceWord(Direction.Across, "B", 3, 4);
			Assert.That(w1.IsValid, Is.False);

			w1 = w1.Normalise();
			var w2 = workspace
				.PlaceWord(Direction.Across, "B", 3, 4)
				.PlaceWord(Direction.Across, "A", 3, 3)
				.Normalise();
			Assert.That(w2.IsValid, Is.False);
			Assert.That(
				w1.WordDatabase.GetHashCode(),
				Is.EqualTo(w2.WordDatabase.GetHashCode())
			);
			Assert.That(
				HashUtils.GenerateHash(w1.SlotEntries),
				Is.EqualTo(HashUtils.GenerateHash(w2.SlotEntries))
			);
			Assert.That(w1.GetHashCode(), Is.EqualTo(w2.GetHashCode()));
			Assert.That(w1, Is.EqualTo(w2));
		}

		[Test]
		public void TwoWordsDifferentOrderOfPlacement()
		{
			var workspace = Workspace.Generate(new[] { "ABCD", "BCDE", "CDEF", "DEFG" });
			var w1 = workspace.PlaceWord(Direction.Across, "ABCD", 1, 1)
				.PlaceWord(Direction.Down, "BCDE", 2, 1)
				.Normalise();
			var w2 = workspace
				.PlaceWord(Direction.Down, "BCDE", 2, 1)
				.PlaceWord(Direction.Across, "ABCD", 1, 1)
				.Normalise();
			Assert.That(w1, Is.EqualTo(w2));
			Assert.That(
				w1.WordDatabase.GetHashCode(),
				Is.EqualTo(w2.WordDatabase.GetHashCode())
			);
			Assert.That(
				HashUtils.GenerateHash(w1.SlotEntries),
				Is.EqualTo(HashUtils.GenerateHash(w2.SlotEntries))
			);
			Assert.That(w1.GetHashCode(), Is.EqualTo(w2.GetHashCode()));
			Assert.That(w1, Is.EqualTo(w2));
		}


		[Test]
		public void TestDetectAdjacenciesAcross()
		{
			Workspace? workspace = Workspace.Generate(new[] { "SUEZ", "ZURICH", "QUITO", "EQUADOR" });
			workspace = workspace.PlaceWord(Direction.Down, "SUEZ", 0, 0);
			workspace = workspace.PlaceWord(Direction.Across, "ZURICH", 0, 3);
			workspace = workspace.PlaceWord(Direction.Down, "QUITO", 1, 2);
			Assert.That(workspace.IsValid, Is.False);
		}

		[Test]
		public void CompareLocations()
		{
			Assert.That(new Location(0, 0).CompareTo(null), Is.EqualTo(1));
			Assert.That(
				new Location(0, 0).CompareTo(new Location(1, 0)),
				Is.EqualTo(-1)
			);
			Assert.That(
				new Location(0, 0).CompareTo(new Location(0, 1)),
				Is.EqualTo(-1)
			);
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