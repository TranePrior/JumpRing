using System.Collections.Generic;
using JumpRing.Game.Core.Services;
using JumpRing.Game.Gameplay.Pooling;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    /// <summary>
    /// What a revive needs from the coin field: a clean slate laid out around wherever the player
    /// came back, instead of the coins that belonged to the run up to its death.
    /// </summary>
    public interface ICoinSpawner
    {
        void RespawnFromCurrentPosition();
    }

    public sealed class CoinStepSpawner : MonoBehaviour, ICoinSpawner
    {
        [Header("Dependencies")]
        [SerializeField]
        private RunSessionController runSessionController;

        [SerializeField]
        private LinePathGenerator linePathGenerator;

        [SerializeField]
        private Transform ringTransform;

        [SerializeField]
        private GameObject coinPrefab;

        [SerializeField]
        private Transform spawnedCoinsParent;

        [SerializeField]
        private CurrencyService currencyService;

        [SerializeField]
        private RiskRewardSystem riskRewardSystem;

        [SerializeField]
        private MicroEventSystem microEventSystem;

        [SerializeField]
        private BonusEffectManager bonusEffectManager;

        [Header("Spawn")]
        [SerializeField, Min(0.1f)]
        private float spawnStep = 10f;

        [SerializeField, Min(0f)]
        private float spawnAheadDistance = 18f;

        [SerializeField, Min(0f)]
        private float spawnStartOffset = 4f;

        [SerializeField, Min(0f)]
        private float despawnBehindDistance = 8f;

        [SerializeField]
        private float spawnYOffset = 0f;

        public void SetCoinPrefab(GameObject prefab)
        {
            coinPrefab = prefab;
        }

        /// <summary>Enough to cover the coins alive on screen at once, so a run never allocates.</summary>
        private const int PrewarmCount = 8;

        private readonly Queue<PooledInstance> spawnedCoins = new();
        private PrefabPoolRegistry coinPools;
        private float nextSpawnX;
        private bool isSpawning;

        private ICurrencyService CurrencyService => currencyService;

        private void Awake()
        {
            coinPools = new PrefabPoolRegistry(spawnedCoinsParent, PrewarmCount);
        }

        private void OnEnable()
        {
            runSessionController.RunStarted += OnRunStarted;
            runSessionController.RunFinished += OnRunFinished;
        }

        private void OnDisable()
        {
            runSessionController.RunStarted -= OnRunStarted;
            runSessionController.RunFinished -= OnRunFinished;
        }

        private void Update()
        {
            if (!isSpawning)
            {
                return;
            }

            SpawnAhead();
            DespawnBehind();
            SnapCoinsToLine();
        }

        private void OnRunStarted()
        {
            ClearSpawnedCoins();
            nextSpawnX = CalculateFirstSpawnX();
            isSpawning = true;
        }

        public void RespawnFromCurrentPosition()
        {
            ClearSpawnedCoins();
            nextSpawnX = CalculateFirstSpawnX();

            // A revive resumes a run that never fired RunFinished, so spawning happens to still be
            // on. Saying so explicitly keeps this method correct on its own terms instead of
            // leaning on which death path the caller came through.
            isSpawning = true;
        }

        private void OnRunFinished()
        {
            isSpawning = false;
        }

        private void SpawnAhead()
        {
            var spawnLimitX = ringTransform.position.x + spawnAheadDistance;

            while (nextSpawnX <= spawnLimitX)
            {
                SpawnCoin(nextSpawnX);
                nextSpawnX += spawnStep;
            }
        }

        private void DespawnBehind()
        {
            var despawnX = ringTransform.position.x - despawnBehindDistance;

            while (spawnedCoins.Count > 0)
            {
                var coin = spawnedCoins.Peek();

                // Already picked up: the collectible handed itself back to the pool.
                if (!coin.gameObject.activeSelf)
                {
                    spawnedCoins.Dequeue();
                    continue;
                }

                if (coin.transform.position.x >= despawnX)
                {
                    break;
                }

                coin.Release();
                spawnedCoins.Dequeue();
            }
        }

        private void SpawnCoin(float xPosition)
        {
            var yPosition = linePathGenerator.EvaluateHeightAtX(xPosition) + spawnYOffset;
            var spawnPosition = new Vector3(xPosition, yPosition, 0f);
            var spawnedCoin = coinPools.Rent(coinPrefab, spawnPosition);
            var coinCollectible = spawnedCoin.GetComponent<CoinCollectible>();
            coinCollectible.Construct(CurrencyService, runSessionController, riskRewardSystem, microEventSystem, bonusEffectManager);
            spawnedCoins.Enqueue(spawnedCoin);
        }

        private void SnapCoinsToLine()
        {
            foreach (var coin in spawnedCoins)
            {
                if (!coin.gameObject.activeSelf)
                {
                    continue;
                }

                var pos = coin.transform.position;
                pos.y = linePathGenerator.EvaluateHeightAtX(pos.x) + spawnYOffset;
                coin.transform.position = pos;
            }
        }

        private float CalculateFirstSpawnX()
        {
            var firstDesiredX = ringTransform.position.x + spawnStartOffset;
            return Mathf.Ceil(firstDesiredX / spawnStep) * spawnStep;
        }

        public void RemoveCoinNear(float x, float tolerance)
        {
            var tempQueue = new Queue<PooledInstance>();

            while (spawnedCoins.Count > 0)
            {
                var coin = spawnedCoins.Dequeue();

                if (!coin.gameObject.activeSelf)
                {
                    continue;
                }

                if (Mathf.Abs(coin.transform.position.x - x) <= tolerance)
                {
                    coin.Release();
                    continue;
                }

                tempQueue.Enqueue(coin);
            }

            while (tempQueue.Count > 0)
            {
                spawnedCoins.Enqueue(tempQueue.Dequeue());
            }
        }

        private void ClearSpawnedCoins()
        {
            while (spawnedCoins.Count > 0)
            {
                var coin = spawnedCoins.Dequeue();

                if (!coin.gameObject.activeSelf)
                {
                    continue;
                }

                coin.Release();
            }
        }
    }
}
