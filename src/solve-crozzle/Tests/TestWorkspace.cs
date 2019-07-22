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
		public void TestExpand()
		{
			var workspace = Workspace.Generate(new[] { "Apple" });
			workspace = workspace.PlaceWord(Direction.Across, "Apple", 0, 0);
			Assert.That(workspace.XStart, Is.EqualTo(-1));
			Assert.That(workspace.YStart, Is.EqualTo(0));
			Assert.That(workspace.Width, Is.EqualTo(7));
			Assert.That(workspace.Values.Length, Is.EqualTo("*Apple*".Length));
			Assert.That(workspace.ToString(), Is.EqualTo("*Apple*\r\n"));
		}
	}
}
