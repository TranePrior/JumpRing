using System.Collections.Generic;
using JumpRing.Game.Core.Services;
using UnityEngine;

namespace JumpRing.Game.Gameplay
{
    public sealed class CoinStepSpawner : MonoBehaviour
    {
        private enum SpawnYMode
        {
            OnLine = 0,
            CenterLine = 1,
        }

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
        private Transform centerLinePoint;

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
        private float spawnStep = 7f;

        [SerializeField, Min(0f)]
        private float spawnAheadDistance = 18f;

        [SerializeField, Min(0f)]
        private float spawnStartOffset = 4f;

        [SerializeField, Min(0f)]
        private float despawnBehindDistance = 8f;

        [SerializeField]
        private SpawnYMode spawnYMode = SpawnYMode.OnLine;

        [SerializeField]
        private float spawnYOffset = 0f;

        public void SetCoinPrefab(GameObject prefab)
        {
            coinPrefab = prefab;
        }

        private readonly Queue<GameObject> spawnedCoins = new();
        private float nextSpawnX;
        private bool isSpawning;

        private ICurrencyService CurrencyService => currencyService;

        private void OnEnable()
        {
            if (riskRewardSystem == null)
            {
                riskRewardSystem = Object.FindFirstObjectByType<RiskRewardSystem>();
            }

            if (microEventSystem == null)
            {
                microEventSystem = Object.FindFirstObjectByType<MicroEventSystem>();
            }

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
        }

        private void OnRunStarted()
        {
            ClearSpawnedCoins();
            nextSpawnX = CalculateFirstSpawnX();
            isSpawning = true;
            SpawnAhead();
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

                if (coin == null)
                {
                    spawnedCoins.Dequeue();
                    continue;
                }

                if (coin.transform.position.x >= despawnX)
                {
                    break;
                }

                Destroy(coin);
                spawnedCoins.Dequeue();
            }
        }

        private void SpawnCoin(float xPosition)
        {
            var yPosition = ResolveSpawnY(xPosition) + spawnYOffset;
            var spawnPosition = new Vector3(xPosition, yPosition, 0f);
            var spawnedCoin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity, spawnedCoinsParent);
            var coinCollectible = spawnedCoin.GetComponent<CoinCollectible>();
            coinCollectible.Construct(CurrencyService, runSessionController, riskRewardSystem, microEventSystem, bonusEffectManager);
            spawnedCoins.Enqueue(spawnedCoin);
        }

        private float ResolveSpawnY(float xPosition)
        {
            if (spawnYMode == SpawnYMode.CenterLine)
            {
                return centerLinePoint.position.y;
            }

            return linePathGenerator.EvaluateHeightAtX(xPosition);
        }

        private float CalculateFirstSpawnX()
        {
            var firstDesiredX = ringTransform.position.x + spawnStartOffset;
            return Mathf.Ceil(firstDesiredX / spawnStep) * spawnStep;
        }

        private void ClearSpawnedCoins()
        {
            while (spawnedCoins.Count > 0)
            {
                var coin = spawnedCoins.Dequeue();

                if (coin == null)
                {
                    continue;
                }

                Destroy(coin);
            }
        }
    }
}
