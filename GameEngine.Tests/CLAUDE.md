# GameEngine.Tests

コンソールRPGエンジンのテストプロジェクト。

## テスト環境

- **.NET 9.0** / **xUnit 2.9.2** / **Moq 4.20.72**
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

### Factory/

- **EnemyFactoryTests.cs** -- `EnemyFactory` の静的メソッドを検証
  - `GetAvailableEnemyKeys()` が既知の敵キー（例: "Goblin"）を含むこと
  - `Create("Goblin")` が名前・MaxHP・AttackStrategy の正しい Enemy を返すこと
  - 実際の `enemy-specs.yml` を読み込むため、YAML の変更がテストに影響する

### Manager/

- **HealthManagerTests.cs** -- `HealthManager` の HP/DP 計算と装備連動を検証
  - `IEquipmentStatsProvider` のテスト実装（`TestEquipmentProvider`）を内部クラスとして定義
  - 装備変更時に `MaxHP`・`TotalDP` が再計算され、`CurrentHP` が新 MaxHP にクリップされること
- **InventoryManagerTests.cs** -- `InventoryManager` と `HealthManager` の連携を検証
  - `EquipWeapon()` で武器を装備すると `HealthManager` の `MaxHP`・`TotalDP` が更新されること
  - `InventoryManager` 自体が `IEquipmentStatsProvider` として機能することの実証

### Models/

詳細は [Models/CLAUDE.md](./Models/CLAUDE.md) を参照。

## テスト記述パターン

- **Arrange / Act / Assert** パターンを統一的に使用
- `[Fact]` 属性で単一ケース、`[Theory]` + `[InlineData]` でパラメタライズドテスト（Models 配下で使用）
- モック依存: `IEquipmentStatsProvider` 等のインターフェースをテスト用内部クラスで実装（Moq は依存に含まれるが、現時点では手動モックが中心）
