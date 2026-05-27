# Mappers

ドメインモデル → DTO への変換ロジックを定義。namespace: `GameEngine.Mappers`

## ファイル構成

- **GameStateMapper.cs** - 拡張メソッド集
  - `IPlayer.ToPlayerState()` : `IPlayer` のプロパティから直接変換（`GetSaveData()` 不使用）
  - `IEnemy.ToEnemyState()` / `IWeapon.ToWeaponInfo()`
  - `CreateInitialBattleState()` / `CreateInitialShopState()` / `CreateEmptyGameState()` : 初期状態ファクトリ
  - `CreateMessage()` / `CreateMessages()` : `GameMessage` 生成ヘルパー
