using System.Collections.Generic;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class BonusSpawner : MonoBehaviour
    {
        private const string ConsecutiveDeathsKey = "BonusSpawner_ConsecutiveDeaths";
        private const int DeathResetScoreThreshold = 15;

        [Header("Dependencies")]
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private DifficultyManager difficultyManager;

        [SerializeField]
        private RiskRewardSystem riskRewardSystem;

        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [SerializeField]
        private BonusConfig bonusConfig;

        [SerializeField]
        private Transform ringTransform;

        [SerializeField]
        private Transform spawnedBonusParent;

        [Header("Segment Settings")]
        [SerializeField, Min(3), Tooltip("Taps per segment")]
        private int segmentSize = 3;

        [SerializeField, Min(1), Tooltip("Minimum segments between bonuses")]
        private int cooldownSegments = 1;

        [SerializeField, Min(1), Tooltip("Skip first N segments (no bonuses at start)")]
        private int skipFirstSegments = 1;

        [Header("Spawn Chances")]
        [SerializeField, Range(0f, 1f)]
        private float baseChance = 0.90f;

        [SerializeField, Range(0f, 0.5f)]
        private float nearMissBoost = 0.30f;

        [SerializeField, Range(0f, 0.5f)]
        private float strugglingBoost = 0.20f;

        [SerializeField, Range(0f, 0.3f)]
        private float proPlayerPenalty = 0.10f;

        [SerializeField, Min(1), Tooltip("Deaths in a row to count as struggling")]
        private int strugglingDeathThreshold = 2;

        [SerializeField, Min(1), Tooltip("Score without dying to count as pro")]
        private int proPlayerScoreThreshold = 40;

        [Header("Guaranteed Bonus")]
        [SerializeField, Min(2)]
        private int guaranteedBonusSegment = 2;

        [Header("Spawn Position")]
        [SerializeField, Min(1f)]
        private float spawnAheadDistance = 12f;

        [SerializeField]
        private float spawnYOffset = 0f;

        [SerializeField, Min(1f)]
        private float despawnBehindDistance = 8f;

        private readonly Queue<GameObject> spawnedBonuses = new();

        private int currentTapCount;
        private int currentSegment;
        private int segmentsSinceLastBonus;
        private bool nearMissThisSegment;
        private bool hasBonusThisRun;
        private bool isSpawning;

        private int ConsecutiveDeaths
        {
            get => PlayerPrefs.GetInt(ConsecutiveDeathsKey, 0);
            set => PlayerPrefs.SetInt(ConsecutiveDeathsKey, value);
        }

        private void OnEnable()
        {
            runSessionController.RunStarted += OnRunStarted;
            runSessionController.RunFinished += OnRunFinished;

            if (riskRewardSystem != null)
            {
                riskRewardSystem.NearMissDetected += OnNearMiss;
            }
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
            runSessionController.RunFinished -= OnRunFinished;

            if (riskRewardSystem != null)
            {
                riskRewardSystem.NearMissDetected -= OnNearMiss;
            }
        }

        private void Update()
        {
            if (!isSpawning)
            {
                return;
            }

            DespawnBehind();

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;
            var newSegment = score / segmentSize;

            if (newSegment <= currentSegment)
            {
                return;
            }

            currentSegment = newSegment;
            segmentsSinceLastBonus++;
            OnSegmentBoundary();
            nearMissThisSegment = false;
        }

        private void OnRunStarted()
        {
            ClearSpawnedBonuses();
            currentTapCount = 0;
            currentSegment = 0;
            segmentsSinceLastBonus = cooldownSegments;
            nearMissThisSegment = false;
            hasBonusThisRun = false;
            isSpawning = true;
        }

        private void OnRunFinished()
        {
            isSpawning = false;

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;

            if (score >= DeathResetScoreThreshold)
            {
                ConsecutiveDeaths = 0;
            }
            else
            {
                ConsecutiveDeaths++;
            }
        }

        private void OnNearMiss()
        {
            nearMissThisSegment = true;
        }

        private void OnSegmentBoundary()
        {
            if (currentSegment <= skipFirstSegments)
            {
                return;
            }

            if (segmentsSinceLastBonus < cooldownSegments)
            {
                return;
            }

            if (bonusEffectManager.HasActiveBonus)
            {
                return;
            }

            var chance = CalculateSpawnChance();

            // Guaranteed bonus if none spawned this run yet
            if (!hasBonusThisRun && currentSegment >= guaranteedBonusSegment)
            {
                chance = 1f;
            }

            if (Random.value < chance)
            {
                var type = GetWeightedBonusType();
                SpawnBonusAhead(type);
                segmentsSinceLastBonus = 0;
                hasBonusThisRun = true;
            }
        }

        private float CalculateSpawnChance()
        {
            var chance = baseChance;

            if (nearMissThisSegment)
            {
                chance += nearMissBoost;
            }

            if (ConsecutiveDeaths >= strugglingDeathThreshold)
            {
                chance += strugglingBoost;
            }

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;

            if (score >= proPlayerScoreThreshold)
            {
                chance -= proPlayerPenalty;
            }

            return Mathf.Clamp01(chance);
        }

        private BonusType GetWeightedBonusType()
        {
            var entries = bonusConfig.Entries;

            if (entries == null || entries.Length == 0)
            {
                return BonusType.SlowMotion;
            }

            var score = difficultyManager != null ? difficultyManager.CurrentScore : 0;
            var isStruggling = ConsecutiveDeaths >= strugglingDeathThreshold;
            var isPro = score >= proPlayerScoreThreshold;

            var totalWeight = 0f;
            var weights = new float[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                var w = entries[i].weight;

                if (isStruggling)
                {
                    // Boost helpful bonuses
                    if (entries[i].type is BonusType.SlowMotion or BonusType.CalmLine or BonusType.SizeUp)
                    {
                        w *= 1.5f;
                    }
                }

                if (isPro)
                {
                    // Boost score boost for good players
                    if (entries[i].type == BonusType.ScoreBoost)
                    {
                        w *= 2f;
                    }

                    // Reduce help bonuses
                    if (entries[i].type is BonusType.SlowMotion or BonusType.CalmLine or BonusType.SizeUp)
                    {
                        w *= 0.5f;
                    }
                }

                weights[i] = w;
                totalWeight += w;
            }

            var roll = Random.value * totalWeight;
            var cumulative = 0f;

            for (var i = 0; i < entries.Length; i++)
            {
                cumulative += weights[i];

                if (roll <= cumulative)
                {
                    return entries[i].type;
                }
            }

            return entries[entries.Length - 1].type;
        }

        private void SpawnBonusAhead(BonusType type)
        {
            var entry = bonusConfig.GetEntry(type);

            if (entry.prefab == null)
            {
                return;
            }

            var spawnX = ringTransform.position.x + spawnAheadDistance;
            var lineY = linePathGenerator.EvaluateHeightAtX(spawnX);
            var spawnY = lineY + spawnYOffset;
            var spawnPos = new Vector3(spawnX, spawnY, 0f);

            var parent = spawnedBonusParent != null ? spawnedBonusParent : transform;
            var go = Instantiate(entry.prefab, spawnPos, Quaternion.identity, parent);
            var collectible = go.GetComponent<BonusCollectible>();

            if (collectible != null)
            {
                collectible.Construct(bonusEffectManager, runSessionController, type);
            }

            spawnedBonuses.Enqueue(go);
        }

        private void DespawnBehind()
        {
            var despawnX = ringTransform.position.x - despawnBehindDistance;

            while (spawnedBonuses.Count > 0)
            {
                var bonus = spawnedBonuses.Peek();

                if (bonus == null)
                {
                    spawnedBonuses.Dequeue();
                    continue;
                }

                if (bonus.transform.position.x >= despawnX)
                {
                    break;
                }

                Destroy(bonus);
                spawnedBonuses.Dequeue();
            }
        }

        private void ClearSpawnedBonuses()
        {
            while (spawnedBonuses.Count > 0)
            {
                var bonus = spawnedBonuses.Dequeue();

                if (bonus != null)
                {
                    Destroy(bonus);
                }
            }
        }
    }
}
