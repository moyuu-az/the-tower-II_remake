# Tower Game 大規模実装計画 v2

**作成日**: 2026-01-07
**目的**: The Tower II スタイルのゲームプレイ拡張

---

## 現状分析

### 実装済み機能
- ✅ コアゲームループ（時間システム、従業員通勤）
- ✅ グリッドシステム（配置、スナップ、検証）
- ✅ フロアシステム（ロビー、フロア構造、テナント配置）
- ✅ エレベーターシステム（シャフト、カー、乗客輸送）
- ✅ 従業員AI（状態マシン、エレベーター利用）
- ✅ UI（建設モード、時間表示）

### 未実装機能（今回の実装対象）
1. **経済システム** - 資金管理、建設コスト、家賃収入
2. **追加テナントタイプ** - レストラン、ショップ、アパート
3. **追加人物タイプ** - 住人（Resident）、訪問者（Visitor）
4. **解体システム** - 建物の削除と払い戻し

---

## アーキテクチャ設計

### 新規名前空間
```
TowerGame.Economy    → EconomyManager, BuildingCosts
TowerGame.Building   → Tenant (base), Restaurant, Shop, Apartment
TowerGame.People     → Resident, Visitor
```

### クラス階層

```
[Building Types]
MonoBehaviour
└── Tenant (abstract base class)
    ├── Office (refactored from OfficeBuilding)
    ├── Restaurant
    ├── Shop
    └── Apartment

[Person Types]
Person (abstract base class)
├── Employee (existing)
├── Resident (new)
└── Visitor (new)
```

---

## Phase 1: 経済システム

### EconomyManager.cs
```csharp
namespace TowerGame.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        // Singleton pattern
        public static EconomyManager Instance { get; private set; }

        // 資金管理
        private long currentMoney;
        public long CurrentMoney => currentMoney;

        // イベント
        public event Action<long> OnMoneyChanged;
        public event Action<long, string> OnTransaction;

        // Methods
        public bool TrySpend(long amount, string reason);
        public void Earn(long amount, string reason);
        public bool CanAfford(long amount);
    }
}
```

### BuildingCosts.cs (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "BuildingCosts", menuName = "TowerGame/Building Costs")]
public class BuildingCosts : ScriptableObject
{
    // 建設コスト
    public long lobbyCost = 100000;
    public long floorCost = 50000;
    public long officeCost = 80000;
    public long restaurantCost = 150000;
    public long shopCost = 120000;
    public long apartmentCost = 200000;
    public long elevatorCost = 300000;

    // 解体時の払い戻し率
    public float demolitionRefundRate = 0.5f;

    // 家賃収入（日額）
    public long officeRentPerDay = 5000;
    public long restaurantRentPerDay = 8000;
    public long shopRentPerDay = 6000;
    public long apartmentRentPerDay = 3000;
}
```

---

## Phase 2: テナントベースクラス

### Tenant.cs (抽象基底クラス)
```csharp
namespace TowerGame.Building
{
    public enum TenantType
    {
        Office,
        Restaurant,
        Shop,
        Apartment
    }

    public abstract class Tenant : MonoBehaviour
    {
        // 共通プロパティ
        protected int floorNumber;
        protected int towerId;
        protected int capacity;
        protected int segmentWidth;

        // 経済
        protected long rentPerDay;
        protected long buildCost;

        // 営業時間
        protected int openHour;
        protected int closeHour;

        // 共通メソッド
        public abstract TenantType Type { get; }
        public virtual bool IsOpen();
        public virtual void CollectRent();
        public abstract void OnDayChanged();
    }
}
```

---

## Phase 3: 追加テナントタイプ

### Restaurant.cs
- 営業時間: 11:00-22:00
- 訪問者（Visitor）が来店
- ランチタイム/ディナータイムでボーナス収入

### Shop.cs
- 営業時間: 10:00-21:00
- 訪問者（Visitor）が来店
- 週末ボーナス収入

### Apartment.cs
- 24時間（住居）
- 住人（Resident）が居住
- 住人は朝に外出、夜に帰宅

---

## Phase 4: 追加人物タイプ

### Resident.cs
```csharp
public enum ResidentState
{
    AtHome,           // アパートで休息
    LeavingHome,      // 外出準備
    GoingOut,         // 外出中（建物外）
    ReturningHome,    // 帰宅中
    UsingElevator,    // エレベーター利用中
    InLobby           // ロビー通過中
}
```

### Visitor.cs
```csharp
public enum VisitorState
{
    Approaching,      // 建物に向かっている
    EnteringLobby,    // ロビーに入る
    WaitingElevator,  // エレベーター待ち
    RidingElevator,   // エレベーター乗車中
    Shopping,         // 買い物/食事中
    LeavingTenant,    // テナントを出る
    LeavingBuilding   // 建物を出る
}
```

---

## Phase 5: 解体システム

### DemolitionSystem
- BuildingPlacerに解体モード追加
- クリックした建物を選択
- 確認後に解体
- BuildingCosts.demolitionRefundRate に基づいて払い戻し

---

## 実装順序

1. **EconomyManager** - 経済基盤
2. **BuildingCosts** - コスト設定
3. **Tenant基底クラス** - テナント抽象化
4. **Office リファクタリング** - OfficeBuildingをTenant継承に変更
5. **Restaurant** - 新規テナント
6. **Shop** - 新規テナント
7. **Apartment** - 新規テナント
8. **Resident** - 住人AI
9. **Visitor** - 訪問者AI
10. **解体システム** - 建物削除
11. **UI更新** - 経済UI、新建物ボタン

---

## ファイル構成

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs (更新: EconomyManager参照追加)
│   └── GameTimeManager.cs (既存)
├── Economy/
│   ├── EconomyManager.cs (新規)
│   └── BuildingCosts.cs (新規)
├── Building/
│   ├── Tenant.cs (新規: 基底クラス)
│   ├── Office.cs (新規: OfficeBuildingをリファクタ)
│   ├── Restaurant.cs (新規)
│   ├── Shop.cs (新規)
│   ├── Apartment.cs (新規)
│   ├── BuildingPlacer.cs (更新: 新建物タイプ、解体モード)
│   ├── FloorSystemManager.cs (更新: 新テナント検証)
│   └── ... (既存)
├── People/
│   ├── Person.cs (既存)
│   ├── Employee.cs (既存)
│   ├── Resident.cs (新規)
│   ├── Visitor.cs (新規)
│   └── PersonSpawner.cs (更新)
└── UI/
    ├── EconomyUI.cs (新規)
    └── BuildModeUI.cs (更新)
```

---

## 見積もり

| Phase | 内容 | 新規ファイル | 更新ファイル |
|-------|------|-------------|-------------|
| 1 | 経済システム | 2 | 1 |
| 2 | Tenant基底クラス | 1 | 1 |
| 3 | 追加テナント | 3 | 2 |
| 4 | 追加人物 | 2 | 1 |
| 5 | 解体システム | 0 | 2 |
| 6 | UI更新 | 1 | 1 |
| **合計** | | **9** | **8** |

---

## 注意事項

- 既存のOfficeBuildingとの後方互換性を維持
- エレベーターシステムとの連携を確保
- 時間イベント（OnHourChanged, OnDayChanged）を活用
- デバッグログは `[ClassName]` プレフィックスで統一
