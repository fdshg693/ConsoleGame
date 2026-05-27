# GameEngine.Tests/Models

Modelsレイヤーの単体テスト群。xUnit + `[Theory]`/`[Fact]` で記述。

## テストファイル概要

- **AttackStrategyTests.cs** -- `AttackStrategy.GetAttackStrategy()` の文字列マッピング検証
  - 有効な戦略名 (Default / Melee / Magic) で正しい `IAttackStrategy` が返ること
  - 未知の戦略名で `Default` にフォールバックすること
- **GameStateMapperTests.cs** -- `GameStateMapper` のファクトリメソッド群を検証
  - `CreateEmptyGameState` / `CreateInitialBattleState` / `CreateInitialShopState` の初期値
  - `CreateMessage` / `CreateMessages` のメッセージ生成
  - `ToWeaponInfo` 拡張メソッド (デフォルト価格・カスタム価格・null ガード)
- **GameStateTests.cs** -- ステートマシン用データモデルのプロパティ検証
  - `GameState` / `PlayerState` / `EnemyState` / `BattleState` / `ShopState` / `GameMessage`
  - `MessageType` 全列挙値 (Info, Success, Warning, Error, Combat, System, Experience, Gold)
  - `GamePhase` 全列挙値 (Initialization, Exploration, Battle, Shop, Rest, GameOver)
- **PlayerActionTests.cs** -- プレイヤー行動モデルとバリデーション
  - `AttackAction` / `UseItemAction` / `ShopAction` / `GameControlAction` / `RestAction` の生成
  - `GameControlAction` は不正な `ActionType` で `ArgumentException` をスローすること
  - `PlayerActionValidator.IsValid()` による入力検証 (空文字列・不正戦略名・数量0以下・武器名未指定など)

## テスト実行

```bash
dotnet test --filter "FullyQualifiedName~GameEngine.Tests.Models"
```

## 注意点

- `AttackStrategy` のマッピングは YAML の戦略名と一致させる必要がある (Default / Melee / Magic)
- `PlayerActionValidator` は大文字小文字を区別しない (`"default"`, `"MELEE"` も有効)
