using System;
using System.Collections.Generic;
using PlatformLink;
using RetroCat.Modules.Core.UI.Activities.Popups.Core;
using RetroCat.Modules.FlexibleUI.Runtime.Popups;
using RetroCat.PlatformLink.Runtime.Source.Common.Modules.Leaderboards;
using UnityEngine;
using UnityEngine.UI;

namespace RetroCat.Modules.UITemplates.Common.Popups.Leaderboard
{
    public class LeaderboardPopup : PopupBase
    {
        private const string OpenLeaderboardEvent = "open-leaderboard";
        private const float CacheLifetimeSeconds = 5f * 60f;

        private static readonly Dictionary<string, LeaderboardCache> LeaderboardCacheById = new();

        [SerializeField] private LeaderboardRow _leaderboardRowPrefab;
        [SerializeField] private LeaderboardLeader[] _leaders;
        [SerializeField] private Transform _leadersParent;
        [SerializeField] private Transform _yourRankParent;

        [SerializeField] private string _leaderboardId;
        [SerializeField] private int _quantityTop = 5;
        [SerializeField] private int _quantityAround = 2;

        [SerializeField] private PopupContentLoader _popupContentLoader;

        [SerializeField] private GameObject _bottomAuthorized;
        [SerializeField] private GameObject _bottomNotAuthorized;

        [SerializeField] private Button _authorizeButton;

        private Func<int> _bestScoreProvider;

        private sealed class LeaderboardCache
        {
            public LeaderboardEntries Data;
            public DateTime CachedAtUtc;
            public int QuantityTop;
            public int QuantityAround;
            public int UserScore;
            public int NextHigherScore;
            public bool HasUserEntry;
        }

        public void SetBestScoreProvider(Func<int> provider)
        {
            _bestScoreProvider = provider;
        }

        protected override void OnInit()
        {
            foreach (LeaderboardLeader leader in _leaders)
                leader.gameObject.SetActive(false);

            bool isAuth = PLink.Platform.Authorized;
            SetAuthorizeState(isAuth);
        }

        private void SetAuthorizeState(bool isAuth)
        {
            _bottomAuthorized.SetActive(isAuth);
            _bottomNotAuthorized.SetActive(!isAuth);
        }

        protected override void OnOpenStarted()
        {
            if (PLink.IsInitialized)
                PLink.Analytics.SendEvent(OpenLeaderboardEvent);

            _popupContentLoader.ShowLoading();
            LoadLeaderboard();

            _authorizeButton.onClick.AddListener(OnAuthorizeButtonClicked);
        }

        protected override void OnOpenFinished() { }

        protected override void OnCloseStarted()
        {
            _authorizeButton.onClick.RemoveListener(OnAuthorizeButtonClicked);
        }

        protected override void OnCloseFinished() { }

        private void OnAuthorizeButtonClicked()
        {
            PLink.Platform.Authorize(success =>
            {
                SetAuthorizeState(success);
            });
        }

        private void LoadLeaderboard()
        {
            ClearContainer(_leadersParent);
            ClearContainer(_yourRankParent);

            if (string.IsNullOrEmpty(_leaderboardId))
            {
                _popupContentLoader.HideLoading();
                return;
            }

            if (TryApplyCachedLeaderboard())
                return;

            PLink.Leaderboard.GetEntries(_leaderboardId, _quantityTop, true, _quantityAround * 2, OnLeaderboardLoaded);
        }

        private void OnLeaderboardLoaded(bool ok, LeaderboardEntries data)
        {
            if (!ok || data == null || data.Entries == null)
            {
                if (!TryApplyCachedLeaderboard(force: true))
                    _popupContentLoader.HideLoading();
                return;
            }

            CacheLeaderboardData(data);
            ApplyLeaderboardData(data);
        }

        private bool TryApplyCachedLeaderboard(bool force = false)
        {
            if (!LeaderboardCacheById.TryGetValue(_leaderboardId, out LeaderboardCache cache))
                return false;

            if (cache == null || cache.Data == null || cache.Data.Entries == null)
                return false;

            if (cache.QuantityTop != _quantityTop || cache.QuantityAround != _quantityAround)
                return false;

            if (!force && ShouldRequestFreshData(cache))
                return false;

            ApplyLeaderboardData(cache.Data);
            return true;
        }

        private bool ShouldRequestFreshData(LeaderboardCache cache)
        {
            if (cache == null)
                return true;

            if ((DateTime.UtcNow - cache.CachedAtUtc).TotalSeconds >= CacheLifetimeSeconds)
                return true;

            int currentBestScore = GetCurrentBestScore();
            if (currentBestScore <= cache.UserScore)
                return false;

            if (!cache.HasUserEntry)
                return true;

            return currentBestScore > cache.NextHigherScore;
        }

        private void CacheLeaderboardData(LeaderboardEntries data)
        {
            if (string.IsNullOrEmpty(_leaderboardId))
                return;

            bool hasUserEntry = TryGetUserEntry(data, out LeaderboardEntry userEntry);

            var cache = new LeaderboardCache
            {
                Data = data,
                CachedAtUtc = DateTime.UtcNow,
                QuantityTop = _quantityTop,
                QuantityAround = _quantityAround,
                UserScore = hasUserEntry ? userEntry.Score : GetCurrentBestScore(),
                NextHigherScore = GetNextHigherScore(data),
                HasUserEntry = hasUserEntry
            };

            LeaderboardCacheById[_leaderboardId] = cache;
        }

        private void ApplyLeaderboardData(LeaderboardEntries data)
        {
            PopulateTopEntries(data);
            PopulateAroundEntries(data);
            _popupContentLoader.HideLoading();
        }

        private int GetCurrentBestScore()
        {
            return _bestScoreProvider != null ? Mathf.Max(0, _bestScoreProvider()) : 0;
        }

        private static bool TryGetUserEntry(LeaderboardEntries data, out LeaderboardEntry userEntry)
        {
            userEntry = null;

            if (data == null || data.Entries == null || data.UserRank <= 0)
                return false;

            foreach (LeaderboardEntry entry in data.Entries)
            {
                if (entry == null || entry.Rank <= 0)
                    continue;

                if (entry.Rank != data.UserRank)
                    continue;

                userEntry = entry;
                return true;
            }

            return false;
        }

        private static int GetNextHigherScore(LeaderboardEntries data)
        {
            if (data == null || data.Entries == null || data.UserRank <= 1)
                return int.MaxValue;

            int nextHigherRank = int.MinValue;
            int nextHigherScore = int.MaxValue;

            foreach (LeaderboardEntry entry in data.Entries)
            {
                if (entry == null || entry.Rank <= 0)
                    continue;

                if (entry.Rank >= data.UserRank)
                    continue;

                if (entry.Rank <= nextHigherRank)
                    continue;

                nextHigherRank = entry.Rank;
                nextHigherScore = entry.Score;
            }

            return nextHigherRank == int.MinValue ? int.MaxValue : nextHigherScore;
        }

        private void PopulateTopEntries(LeaderboardEntries data)
        {
            List<LeaderboardEntry> entries = CollectTopEntries(data);

            for (int i = 0; i < _leaders.Length && i < entries.Count; i++)
            {
                _leaders[i].gameObject.SetActive(true);
                _leaders[i].SetUser(entries[i]);
            }

            for (int i = _leaders.Length; i < entries.Count; i++)
            {
                bool isUser = data.UserRank == entries[i].Rank;
                CreateRow(_leadersParent, entries[i], isUser);
            }
        }

        private void PopulateAroundEntries(LeaderboardEntries data)
        {
            List<LeaderboardEntry> entries = CollectAroundEntries(data);

            foreach (LeaderboardEntry entry in entries)
            {
                bool isUser = entry.Rank == data.UserRank;
                CreateRow(_yourRankParent, entry, isUser);
            }
        }

        private List<LeaderboardEntry> CollectTopEntries(LeaderboardEntries data)
        {
            var results = new List<LeaderboardEntry>();

            foreach (var entry in data.Entries)
            {
                if (entry == null)
                    continue;

                if (entry.Rank <= 0 || entry.Rank > _quantityTop)
                    continue;

                results.Add(entry);
            }

            results.Sort((left, right) => left.Rank.CompareTo(right.Rank));
            return results;
        }

        private List<LeaderboardEntry> CollectAroundEntries(LeaderboardEntries data)
        {
            var results = new List<LeaderboardEntry>();

            if (data.UserRank <= 0)
                return results;

            var uniqueByRank = new Dictionary<int, LeaderboardEntry>();
            foreach (var entry in data.Entries)
            {
                if (entry == null || entry.Rank <= 0)
                    continue;

                if (!uniqueByRank.ContainsKey(entry.Rank))
                    uniqueByRank.Add(entry.Rank, entry);
            }

            if (!uniqueByRank.TryGetValue(data.UserRank, out LeaderboardEntry userEntry))
                return results;

            var above = new List<LeaderboardEntry>();
            var below = new List<LeaderboardEntry>();
            foreach (var pair in uniqueByRank)
            {
                LeaderboardEntry entry = pair.Value;

                if (entry.Rank < data.UserRank)
                    above.Add(entry);
                else if (entry.Rank > data.UserRank)
                    below.Add(entry);
            }

            above.Sort((left, right) => right.Rank.CompareTo(left.Rank));
            below.Sort((left, right) => left.Rank.CompareTo(right.Rank));

            int takeAbove = Mathf.Min(_quantityAround, above.Count);
            int takeBelow = Mathf.Min(_quantityAround, below.Count);

            int missingAbove = _quantityAround - takeAbove;
            int missingBelow = _quantityAround - takeBelow;

            if (missingAbove > 0)
            {
                int extraBelowAvailable = below.Count - takeBelow;
                int addBelow = Mathf.Min(missingAbove, extraBelowAvailable);
                takeBelow += addBelow;
            }

            if (missingBelow > 0)
            {
                int extraAboveAvailable = above.Count - takeAbove;
                int addAbove = Mathf.Min(missingBelow, extraAboveAvailable);
                takeAbove += addAbove;
            }

            for (int i = takeAbove - 1; i >= 0; i--)
                results.Add(above[i]);

            results.Add(userEntry);

            for (int i = 0; i < takeBelow; i++)
                results.Add(below[i]);

            return results;
        }

        private void CreateRow(Transform parent, LeaderboardEntry entry, bool isUser)
        {
            var row = Instantiate(_leaderboardRowPrefab, parent);
            row.Initialize(entry, isUser);
        }

        private static void ClearContainer(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
