using UnityEngine;

namespace Pixension.Utilities
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public static class RotationHelper
    {
        public static Quaternion GetRotation(Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return Quaternion.Euler(0, 0, 0);
                case Direction.East:
                    return Quaternion.Euler(0, 90, 0);
                case Direction.South:
                    return Quaternion.Euler(0, 180, 0);
                case Direction.West:
                    return Quaternion.Euler(0, 270, 0);
                default:
                    return Quaternion.identity;
            }
        }

        public static Vector3Int RotatePosition(Vector3Int pos, Direction dir, Vector3Int pivot = default)
        {
            Vector3Int relative = pos - pivot;
            Vector3Int rotated;

            switch (dir)
            {
                case Direction.North:
                    rotated = relative;
                    break;
                case Direction.East:
                    rotated = new Vector3Int(-relative.z, relative.y, relative.x);
                    break;
                case Direction.South:
                    rotated = new Vector3Int(-relative.x, relative.y, -relative.z);
                    break;
                case Direction.West:
                    rotated = new Vector3Int(relative.z, relative.y, -relative.x);
                    break;
                default:
                    rotated = relative;
                    break;
            }

            return rotated + pivot;
        }

        public static Direction GetRandomDirection(System.Random random)
        {
            return (Direction)random.Next(0, 4);
        }

        public static Direction GetRandomDirection(System.Random random, bool[] allowedDirections)
        {
            if (allowedDirections == null || allowedDirections.Length != 4)
            {
                Debug.LogError("allowedDirections array must have exactly 4 elements");
                return Direction.North;
            }

            int allowedCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if (allowedDirections[i])
                    allowedCount++;
            }

            if (allowedCount == 0)
            {
                Debug.LogWarning("No allowed directions, returning North");
                return Direction.North;
            }

            int randomIndex = random.Next(0, allowedCount);
            int currentIndex = 0;

            for (int i = 0; i < 4; i++)
            {
                if (allowedDirections[i])
                {
                    if (currentIndex == randomIndex)
                        return (Direction)i;
                    currentIndex++;
                }
            }

            return Direction.North;
        }

        public static Direction RotateDirection(Direction dir, int steps)
        {
            int currentDir = (int)dir;
            int newDir = (currentDir + steps) % 4;

            if (newDir < 0)
                newDir += 4;

            return (Direction)newDir;
        }
    }
}