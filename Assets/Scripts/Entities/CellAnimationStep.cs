using UnityEngine;

namespace ContaminationPuzzle.Entities
{
    /// <summary>
    /// Defines the type of animation to apply to a cell.
    /// </summary>
    public enum AnimationType
    {
        /// <summary>Movement animation from one position to another</summary>
        Move,
        
        /// <summary>Chained state animation (birth, mutation, etc.)</summary>
        ChainedAnimation
    }

    /// <summary>
    /// Represents a single step in a cell animation sequence.
    /// Can be either a movement or a state-triggered animation.
    /// </summary>
    public record CellAnimationStep
    {
        public AnimationType animationType { get; }
        public GameObject cellGO { get; }
        public string? triggerName { get; }
        public Vector2Int? positionOrigin { get; }
        public Vector2Int? positionDestination { get; }

        private CellAnimationStep(
            AnimationType animationType,
            GameObject cellGO,
            string? triggerName,
            Vector2Int? positionOrigin,
            Vector2Int? positionDestination)
        {
            this.animationType = animationType;
            this.cellGO = cellGO;
            this.triggerName = triggerName;
            this.positionOrigin = positionOrigin;
            this.positionDestination = positionDestination;
        }

        /// <summary>
        /// Factory method for creating a chained animation step.
        /// Used for state-triggered animations (birth, mutation, etc.)
        /// </summary>
        /// <param name="cellGO">The cell GameObject to animate</param>
        /// <param name="triggerName">The animator trigger name</param>
        /// <returns>A new CellAnimationStep with ChainedAnimation type</returns>
        public static CellAnimationStep Chained(GameObject cellGO, string triggerName)
        {
            return new CellAnimationStep(AnimationType.ChainedAnimation, cellGO, triggerName, null, null);
        }

        /// <summary>
        /// Factory method for creating a movement animation step.
        /// </summary>
        /// <param name="cellGO">The cell GameObject to move</param>
        /// <param name="origin">Starting position on the board</param>
        /// <param name="destination">Destination position on the board</param>
        /// <returns>A new CellAnimationStep with Move type</returns>
        public static CellAnimationStep Move(GameObject cellGO, Vector2Int origin, Vector2Int destination)
        {
            return new CellAnimationStep(AnimationType.Move, cellGO, null, origin, destination);
        }
    }
}
