using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	public interface INextStepGenerator
	{
		IEnumerable<Workspace> GenerateNextSteps(Workspace workspace);
	}
}
