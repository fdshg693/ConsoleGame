# Factory フォルダ

YAML定義からゲームオブジェクト（敵・武器）を生成するファクトリ群。

## 全体構成

- **YamlSpecLoader.cs** -- YAML読み込みの汎用基盤
- **EnemyFactory.cs** -- 敵の生成（`enemy-specs.yml`）
- **WeaponFactory.cs** -- 武器の生成（`weapon-specs.yml`）
- **PlayerFactory.cs** -- プレイヤーの生成/復元（`IPlayerFactory` 実装）

## YamlSpecLoader（汎用YAML読み込み）

- `Load<TSpec>()` ジェネリックメソッドで任意のSpec型を辞書として読み込む
- 呼び出し側からバリデーション関数（`Action<string, TSpec>`）を受け取り、各エントリに適用
- `comparer` 引数で辞書のキー比較方式を指定可能（例: 大文字小文字無視）
- エラーハンドリング: ファイル未存在、空ファイル、YAMLパースエラーをそれぞれ区別して例外送出

## EnemyFactory

- インスタンスクラスで `IEnemyFactory`（GameEngine.Interfaces）を実装。利用にはインスタンスが必要
- **コンストラクタ**: `EnemyFactory(EnemyConfig enemyConfig, IGameMessageBus bus, Random? random = null)`
  - `enemyConfig` -- ゴールド計算設定を注入
  - `bus` -- 生成する各 `Enemy` に伝播するドメインメッセージバスを注入
  - `random` -- 乱数源を注入（省略時は `Random.Shared`）。テストで決定的に差し替え可能
  - インスタンスコンストラクタ内で `enemy-specs.yml` を読み込みキャッシュ
- **パス解決**: `ResolveSpecPath` が指定相対パス → 出力ディレクトリ（`AppContext.BaseDirectory`）の順に探索（GameConfigLoaderと同じフォールバック）
- **Specクラス**: `EnemySpec`（Name, HP, AttackStrategy, Experience, AP, DP）
- キー比較: `StringComparer.Ordinal`（大文字小文字区別あり）
- バリデーション:
  - Name, AttackStrategy は必須
  - HP > 0、AP/DP/Experience >= 0
  - AttackStrategy は `AttackStrategyNames` 定数（Default / Melee / Magic）のいずれか
- 主要メソッド（すべてインスタンスメソッド）:
  - `Create(string key)` -- キー指定で `IEnemy` を生成。AttackStrategy文字列を `AttackStrategyNames` 定数で `IAttackStrategy` 実装にマッピング。`YieldGold` を `(spec.Experience / enemyConfig.GoldBaseMultiplier) + random.Next(enemyConfig.GoldRandomMin, enemyConfig.GoldRandomMax)` で算出し、注入された `bus` とともに `Enemy` に渡す（Enemy 側はゴールド計算・乱数を持たない）
  - `CreateRandomEnemy()` -- 注入された `Random` でランダムに1体を生成（テストで決定的にできる）
  - `GetAvailableEnemyKeys()` -- 登録済みキー一覧

## WeaponFactory

- **Specクラス**: `WeaponSpec`（Name, HP, AP, DP）-- EnemySpecと異なりAttackStrategy/Experienceがない
- staticコンストラクタで `weapon-specs.yml` を一度だけ読み込みキャッシュ
- キー比較: `StringComparer.OrdinalIgnoreCase`（大文字小文字区別なし）
- バリデーション: Name必須、HP > 0、AP/DP >= 0
- 主要メソッド:
  - `CreateWeapon(string weaponType)` -- キー指定で `IWeapon` を生成

## PlayerFactory（`IPlayerFactory` 実装）

- **コンストラクタ**: `PlayerFactory(GameConfig config, IGameMessageBus bus)`。`AddGameEngine` が Singleton 登録
- `CreateNew(string name)` -- 設定の初期値で新規プレイヤーを生成（`ExperienceManager`/`InventoryManager`/`Player` を手組み。合成起点の重複を集約）
- `Restore(PlayerSaveData data)` -- セーブデータからプレイヤーを完全復元:
  - `ExperienceManager` に Level/経験値を、`InventoryManager` に Gold/Potions を復元し、装備武器（`Weapon`）を再装着
  - 基礎 HP = `MaxHP − 装備武器HP` を `PlayerRestoreState` で `Player` へ渡し、現在HP・基礎AP/DPも復元
  - 攻撃戦略名は `AttackStrategy.GetAttackStrategy`（未知名は Default フォールバック）
- セッション復元（フェーズ3）で `GameSessionState.Player` から再構築する経路として利用

## 拡張ガイド

- **敵の追加**: `enemy-specs.yml` にエントリを追加するだけ（既存AttackStrategy使用時はコード変更不要）
- **新AttackStrategy追加時**: `AttackStrategyNames` 定数を追加し、`EnemyFactory.Create()` 内のswitchと `IsValidAttackStrategy()` の両方に追加が必要
- **武器の追加**: `weapon-specs.yml` にエントリを追加するだけ
- **新しい種類のファクトリ追加**: `YamlSpecLoader.Load<T>()` を利用して同パターンで作成可能
