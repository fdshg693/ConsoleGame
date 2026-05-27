# GameEngine.Tests

コンソールRPGエンジンのテストプロジェクト。

## テスト環境

- **.NET 8.0**（エンジンと統一）/ **xUnit 2.9.2** / **Moq 4.20.72**
- DI 合成テスト用に `Microsoft.Extensions.DependencyInjection 8.0.0` を参照
- カバレッジ収集: coverlet.collector 6.0.2
- `enemy-specs.yml` をビルド出力にコピーする設定あり（Factory テストで実ファイルを使用）

## テスト実行

```bash
# 全テスト実行
dotnet test

# フォルダ単位でフィルタ実行
dotnet test --filter "FullyQualifiedName~GameEngine.Tests.Factory"
dotnet test --filter "FullyQualifiedName~GameEngine.Tests.Manager"
dotnet test --filter "FullyQualifiedName~GameEngine.Tests.Models"
```

## テスト構成

### TestDoubles/

- **NullRenderer.cs** -- `IRenderer` の no-op 実装。合成（DI）テストやロジックテストで出力に依存しない検証に使う

### DependencyInjection/

- **ServiceCollectionExtensionsTests.cs** -- `AddGameEngine` の DI 合成の回帰テスト
  - `GameConfig` が Singleton 登録されること（同一インスタンス）
  - `IEnemyFactory` が解決でき、敵キーが存在すること
  - ホスト合成を再現（スタブ `IGameInput` + `IRenderer`(NullRenderer) + 固定名 `IPlayer`）し、リポジトリ有無の双方で `GameSystem` が解決できること
  - `CreatePlayer` は `IGameMessageBus` を解決して `ExperienceManager`/`InventoryManager`/`Player` に注入
  - 内部スタブ `StubGameInput` は `SelectGameAction()` で `GameActionChoice.Continue` を返す

### Factory/

- **EnemyFactoryTests.cs** -- 注入された `EnemyFactory` インスタンスのメソッドを検証
  - `EnemyConfig` + `IGameMessageBus` + シード固定の `Random` を注入してインスタンス化（静的依存を排除した seam）
  - `GetAvailableEnemyKeys()` が既知の敵キー（例: "Goblin"）を含むこと
  - `Create("Goblin")` が名前・MaxHP・AttackStrategy の正しい Enemy を返すこと
  - 実際の `enemy-specs.yml` を読み込むため、YAML の変更がテストに影響する

### Manager/

- **HealthManagerTests.cs** -- `HealthManager` の HP/DP 計算と装備連動を検証
  - `IEquipmentStatsProvider` のテスト実装（`TestEquipmentProvider`）を内部クラスとして定義
  - 装備変更時に `MaxHP`・`TotalDP` が再計算され、`CurrentHP` が新 MaxHP にクリップされること
- **InventoryManagerTests.cs** -- `InventoryManager` と `HealthManager` の連携を検証
  - `InventoryManager(initialGold, initialPotions, potionPrice, IGameMessageBus)` でインスタンス化
  - `EquipWeapon()` で武器を装備すると `HealthManager` の `MaxHP`・`TotalDP` が更新されること
  - `InventoryManager` 自体が `IEquipmentStatsProvider` として機能することの実証

### Models/

詳細は [Models/CLAUDE.md](./Models/CLAUDE.md) を参照。

## テスト記述パターン

- **Arrange / Act / Assert** パターンを統一的に使用
- `[Fact]` 属性で単一ケース、`[Theory]` + `[InlineData]` でパラメタライズドテスト（Models 配下で使用）
- モック依存: `IEquipmentStatsProvider` 等のインターフェースをテスト用内部クラスで実装（Moq は依存に含まれるが、現時点では手動モックが中心）
