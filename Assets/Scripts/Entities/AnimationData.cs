using System.Collections.Generic;

namespace ContaminationPuzzle.Entities
{
    /// <summary>
    /// Container for a sequence of cell animation steps to be executed.
    /// </summary>
    public record AnimationData(IReadOnlyList<CellAnimationStep> animations)
    {
        public IReadOnlyList<CellAnimationStep> animations { get; } = animations;
    }
}