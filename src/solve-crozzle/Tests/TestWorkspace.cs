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
			Assert.That(workspace.XStart, Is.EqualTo(-1));
			Assert.That(workspace.YStart, Is.EqualTo(0));
			Assert.That(workspace.Width, Is.EqualTo(7));
			Assert.That(workspace.Values.Length, Is.EqualTo("*Apple*".Length));
			Assert.That(workspace.ToString(), Is.EqualTo("*Apple*\r\n"));
		}

		[Test]
		public void TestExpand()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			var rectangles = new[]
			{
				new Rectangle
				{
					MinX = -1,
					MaxX = 5,
					MinY = 0,
					MaxY = 0
				},
				new Rectangle
				{
					MinX = -1,
					MaxX = 4,
					MinY = -4,
					MaxY = 1
				},
				new Rectangle
				{
					MinX = -3,
					MinY = -4,
					MaxX = 4,
					MaxY = 1
				}
			};
			workspace = workspace.ExpandSize(rectangles[0]);
			workspace = workspace.ExpandSize(rectangles[1]);
			var currentRectangle = workspace.GetCurrentRectangle();
			workspace = workspace.ExpandSize(rectangles[2]);
		}
	}
}
