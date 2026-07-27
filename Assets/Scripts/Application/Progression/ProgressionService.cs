using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.Application.Progression
{
    public sealed class ProgressionResult
    {
        public ProgressionResult(int level, int cultivation, int levelsGained)
        {
            Level = level;
            Cultivation = cultivation;
            LevelsGained = levelsGained;
        }

        public int Level { get; }
        public int Cultivation { get; }
        public int LevelsGained { get; }
    }

    public sealed class ProgressionService
    {
        private readonly int[] levelRequirements;

        public ProgressionService(IEnumerable<int> levelRequirements)
        {
            if (levelRequirements == null)
            {
                throw new ArgumentNullException(nameof(levelRequirements));
            }

            this.levelRequirements = new List<int>(levelRequirements).ToArray();
            if (this.levelRequirements.Length == 0)
            {
                throw new ArgumentException("至少需要一条升级需求。", nameof(levelRequirements));
            }

            for (int i = 0; i < this.levelRequirements.Length; i++)
            {
                if (this.levelRequirements[i] <= 0)
                {
                    throw new ArgumentException("升级需求必须大于零。", nameof(levelRequirements));
                }
            }
        }

        public ProgressionResult AddCultivation(int level, int cultivation, int amount)
        {
            level = Math.Max(1, level);
            cultivation = Math.Max(0, cultivation) + Math.Max(0, amount);
            int levelsGained = 0;

            while (cultivation >= RequirementFor(level))
            {
                cultivation -= RequirementFor(level);
                level += 1;
                levelsGained += 1;
            }

            return new ProgressionResult(level, cultivation, levelsGained);
        }

        public int RequirementFor(int level)
        {
            int index = Math.Max(0, Math.Min(levelRequirements.Length - 1, level - 1));
            return levelRequirements[index];
        }
    }
}
