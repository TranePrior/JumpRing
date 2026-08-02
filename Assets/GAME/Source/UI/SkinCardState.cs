namespace JumpRing.Game.UI
{
    /// <summary>What the card's action button does in the current state.</summary>
    public enum SkinCardAction
    {
        Buy,
        Upgrade,
        Select
    }

    /// <summary>
    /// Snapshot of everything a shop card needs to render itself. Passed by readonly reference to
    /// keep the shop grid rebuild allocation-free.
    /// </summary>
    public readonly struct SkinCardState
    {
        public SkinCardState(
            bool isOwned,
            bool isActive,
            bool canAfford,
            int upgradeLevel,
            int maxUpgradeLevel,
            int upgradePrice,
            bool canAffordUpgrade)
        {
            IsOwned = isOwned;
            IsActive = isActive;
            CanAfford = canAfford;
            UpgradeLevel = upgradeLevel;
            MaxUpgradeLevel = maxUpgradeLevel;
            UpgradePrice = upgradePrice;
            CanAffordUpgrade = canAffordUpgrade;
        }

        public bool IsOwned { get; }
        public bool IsActive { get; }
        public bool CanAfford { get; }
        public int UpgradeLevel { get; }
        public int MaxUpgradeLevel { get; }
        public int UpgradePrice { get; }
        public bool CanAffordUpgrade { get; }

        public bool IsMaxUpgraded => UpgradeLevel >= MaxUpgradeLevel;

        /// <summary>
        /// Single source of truth for the action button: the view renders it and the presenter
        /// executes it, so the label can never promise something the click does not do. Selecting a
        /// skin is never bound to the button while there is still something to sell for it — the
        /// card body handles selection instead.
        /// </summary>
        public SkinCardAction ResolveAction()
        {
            if (!IsOwned)
            {
                return SkinCardAction.Buy;
            }

            return IsMaxUpgraded ? SkinCardAction.Select : SkinCardAction.Upgrade;
        }

        /// <summary>
        /// Whether a click on the card body does anything: an owned skin is selected, an affordable
        /// one is bought. Selection stays reachable even when an upgrade is unaffordable.
        /// </summary>
        public bool IsCardClickable()
        {
            return IsOwned || CanAfford;
        }

        /// <summary>Whether the action button can currently be pressed.</summary>
        public bool IsActionAvailable()
        {
            switch (ResolveAction())
            {
                case SkinCardAction.Buy:
                    return CanAfford;
                case SkinCardAction.Upgrade:
                    return CanAffordUpgrade;
                default:
                    return !IsActive;
            }
        }
    }
}
