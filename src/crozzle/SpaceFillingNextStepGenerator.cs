using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	public class SpaceFillingNextStepGenerator : INextStepGenerator
	{
		private readonly SpaceFillingGenerationSettings _generationSettings;
		public SpaceFillingNextStepGenerator(SpaceFillingGenerationSettings generationSettings)
		{
			_generationSettings = generationSettings;
		}

		private static IEnumerable<GridRegion> DetermineSpacesThatMustBeFilled(
			Workspace workspace,
			Grid grid,
			CoverageConstraint coverageConstraint
		)
		{
			if (workspace.IncludedWords.Count() <= 2)
			{
				return Enumerable.Empty<GridRegion>();
			}
			var noManLands = grid.FindEnclosedSpaces(
				c =>
				(
					(c.CellType == GridCellType.BlankNoAdjacentSlots)
					|| (c.CellType == GridCellType.EndOfWordMarker)
				)
			).ToList();
			var failingNoMansLand = noManLands
				.Where(r => !coverageConstraint.SatisfiesConstraint(r))
				.ToList();
			if (failingNoMansLand.Any())
			{
				return failingNoMansLand;
			}

			var blanks = grid.FindEnclosedSpaces(
				c => c.CellType == GridCellType.Blank
			).ToList();

			var regionsToExamine = blanks
				.Select(b =>
				{
					foreach (var nml in noManLands)
					{
						if (b.IsAdjacentTo(nml))
						{
							b = b.Union(nml);
						}
					}
					return b;
				}
			).ToList();

			var regionsFirstRound = regionsToExamine
				.Where(
					r =>
						!coverageConstraint.SatisfiesConstraint(r)
				).ToList();

			var regionsSecondRound = grid.FindEnclosedSpaces(
				c =>
				(
					(c.CellType == GridCellType.BlankNoAdjacentSlots)
					|| (c.CellType == GridCellType.EndOfWordMarker)
					|| (c.CellType == GridCellType.Blank)
				)
			).Where(
					r =>
						!coverageConstraint.SatisfiesConstraint(r)
			).Where(
					r => !(regionsFirstRound.Any(r2 => r.OverlapsWith(r2)))
			).ToList();

			return regionsFirstRound.Concat(regionsSecondRound).ToList();
		}

		IEnumerable<Workspace> INextStepGenerator.GenerateNextSteps(Workspace workspace)
		{
			var grid = workspace.GenerateGrid();
			if (grid.PartialWords.Any())
			{
				foreach (var child in WorkspaceExtensions.CoverOnePartialWord(workspace, grid))
				{
					yield return child.Normalise();
				}
				yield break;
			}

			List<Slot> slotsToFill = null;
			var coverageConstraint = new CoverageConstraint(_generationSettings.MaxContiguousSpaces);

			var spacesThatMustBeFilled = DetermineSpacesThatMustBeFilled(
				workspace,
				grid,
				coverageConstraint
			);


			if (!(workspace.CanCoverEachSpace(grid, spacesThatMustBeFilled)))
			{
				yield break;
			}
			const int ThresholdWhereWeDontTryAny = 99;
			const int ThresholdWhereWeOnlyATryOne = 7; // Any larger and it runs out of memory

			spacesThatMustBeFilled = spacesThatMustBeFilled.Where(
				s => s.CountLocations() <= ThresholdWhereWeDontTryAny
			);

			if (spacesThatMustBeFilled.Any())
			{
				spacesThatMustBeFilled = spacesThatMustBeFilled
					.OrderBy(s => s.CountLocations())
				.ToList();
				IEnumerable<IEnumerable<Workspace>> generators = spacesThatMustBeFilled
					.Select(
						region =>
							WorkspaceExtensions.CoverRegion(
								workspace,
								coverageConstraint,
								region,
								this,
								region.CountLocations() <= ThresholdWhereWeOnlyATryOne ? int.MaxValue : 1
							)
					);
				// Until I've solved the duplicate generation problem
				// Have to keep the numbers down
				//generators = generators.Take(3);

				if (generators.Any())
				{
					foreach (
						var child in WorkspaceExtensions.CombineGeneratedWorkspaces(
							generators,
							this
						)
					)
					{
						yield return child.Normalise();
					}
					yield break;
				}
			}
			slotsToFill = workspace.SlotEntries.Select(se => se.Slot).ToList();

			while (slotsToFill.Any())
			{
				var slot = slotsToFill[0];
				slotsToFill.RemoveAt(0);
				workspace = workspace.RemoveSlot(slot);
				foreach (var child in workspace.CoverSlot(grid, slot))
				{
					yield return child.Normalise();
				}

				grid.RemoveSlot(slot);
			}
		}
	}
}
