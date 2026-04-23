using System;

namespace JumpRing.Game.Theming
{
    public interface ISkinShopService
    {
        event Action<SkinItem> SkinSelected;
        event Action<SkinItem> SkinPurchased;

        SkinPackCatalog Catalog { get; }
        SkinItem ActiveSkin { get; }

        bool IsOwned(SkinItem skin);
        bool CanAfford(SkinItem skin);
        bool TryPurchase(SkinItem skin);
        void SelectSkin(SkinItem skin);
    }
}
