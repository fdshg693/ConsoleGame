# Factory フォルダ

YAML定義からゲームオブジェクト（敵・武器）を生成するファクトリ群。

## 全体構成

- **YamlSpecLoader.cs** -- YAML読み込みの汎用基盤
- **EnemyFactory.cs** -- 敵の生成（`enemy-specs.yml`）
- **WeaponFactory.cs** -- 武器の生成（`weapon-specs.yml`）

## YamlSpecLoader（汎用YAML読み込み）

- `Load<TSpec>()` ジェネリックメソッドで任意のSpec型を辞書として読み込む
- 呼び出し側からバリデーション関数（`Action<string, TSpec>`）を受け取り、各エントリに適用
- `comparer` 引数で辞書のキー比較方式を指定可能（例: 大文字小文字無視）
- エラーハンドリング: ファイル未存在、空ファイル、YAMLパースエラーをそれぞれ区別して例外送出

## EnemyFactory

- **Specクラス**: `EnemySpec`（Name, HP, AttackStrategy, Experience, AP, DP）
- staticコンストラクタで `enemy-specs.yml` を一度だけ読み込みキャッシュ
- キー比較: `StringComparer.Ordinal`（大文字小文字区別あり）
- バリデーション:
  - Name, AttackStrategy は必須
  - HP > 0、AP/DP/Experience >= 0
  - AttackStrategy は `Default` / `Melee` / `Magic` のいずれか
- 主要メソッド:
  - `Create(string key)` -- キー指定で `IEnemy` を生成。AttackStrategy文字列を `IAttackStrategy` 実装にマッピング
  - `CreateRandomEnemy()` -- ランダムに1体を生成
  - `GetAvailableEnemyKeys()` -- 登録済みキー一覧（テスト用）

## WeaponFactory

- **Specクラス**: `WeaponSpec`（Name, HP, AP, DP）-- EnemySpecと異なりAttackStrategy/Experienceがない
- staticコンストラクタで `weapon-specs.yml` を一度だけ読み込みキャッシュ
- キー比較: `StringComparer.OrdinalIgnoreCase`（大文字小文字区別なし）
- バリデーション: Name必須、HP > 0、AP/DP >= 0
- 主要メソッド:
  - `CreateWeapon(string weaponType)` -- キー指定で `IWeapon` を生成

## 拡張ガイド

- **敵の追加**: `enemy-specs.yml` にエントリを追加するだけ（既存AttackStrategy使用時はコード変更不要）
- **新AttackStrategy追加時**: `EnemyFactory.Create()` 内のswitchと `IsValidAttackStrategy()` の両方に追加が必要
- **武器の追加**: `weapon-specs.yml` にエントリを追加するだけ
- **新しい種類のファクトリ追加**: `YamlSpecLoader.Load<T>()` を利用して同パターンで作成可能
