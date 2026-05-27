# コンソールUI/UX改善プラン

## Context

現在のゲームは全ての出力が `Console.WriteLine` で下に流れ続け、10回戦闘すると画面が完全に読めなくなる。ショップではポーション価格・武器ステータスが表示されず、入力方法もメニューごとにバラバラ（矢印キー/数字キー/テキスト入力）。ポーション使用は毎回強制的にプロンプトが出るが、所持0本でも聞かれる。

**目標**: 画面の見通しを良くし、ショップ・ポーション操作を直感的にする。

---

## 既知の技術的問題

**メッセージ二重経路問題**: `GameMessageBus` のリアルタイム購読（GameSystem:24）とバッチ描画（BattleResult.Messages等）が共存。ドメインイベント（ダメージ等）は即時表示され、BattleManagerのメッセージ（攻撃宣言等）は後からバッチ描画される。表示順が意図と異なる可能性あり。

→ 本改善では `GameMessageBus` 購読を `ConsoleRenderer` 経由に統一して解決する。

---

## Phase 1: ConsoleRenderer の導入（基盤）

**新規ファイル**: `GameEngine/Systems/ConsoleRenderer.cs`

全コンソール出力を一元管理する static クラス。主要メソッド:

| メソッド | 機能 |
|---|---|
| `ClearScreen(string title)` | `Console.Clear()` + ヘッダー表示 |
| `RenderMessage(GameMessage)` | MessageType に応じた ANSI 色付き出力 |
| `RenderMessages(IEnumerable<GameMessage>)` | バッチ描画 |
| `RenderHPBar(string name, int current, int max)` | `[████████........] 80/100 HP` のようなHPバー |
| `RenderStatusPanel(PlayerState, EnemyState?)` | プレイヤー＋敵のHP・ステータス一覧 |
| `SelectFromMenu(string[] options, MenuOrientation)` | 矢印キー統一メニュー |
| `WaitForKeyPress(string prompt)` | "Press any key..." 待ち |
| `WriteSection(string title)` | `═══ TITLE ═══` セクション区切り |
| `WriteInfo/Success/Warning/Error(string)` | 色付きユーティリティ |

**色マッピング**:
- Combat: Yellow / Success: Green / Warning: Bold Yellow / Error: Red
- Experience: Cyan / Gold: Bold Yellow / System: Gray / Info: Default

**変更ファイル**:
- [GameSystem.cs](GameEngine/Systems/GameSystem.cs) — `RenderMessages` と `OnMessagePublished` を `ConsoleRenderer` 経由に変更
- [GameFlowContext.cs](GameEngine/Systems/StateMachine/GameFlowContext.cs) — 直接 `Console.WriteLine` → `ConsoleRenderer` に移行

---

## Phase 2: 画面クリアによるスクロール解消

各ステート遷移時に `ConsoleRenderer.ClearScreen()` を挿入。

| タイミング | ファイル | ヘッダー例 |
|---|---|---|
| ゲーム開始 | [StartState.cs](GameEngine/Systems/StateMachine/States/StartState.cs) | `GAME START` |
| エンカウント開始 | [EncounterState.cs](GameEngine/Systems/StateMachine/States/EncounterState.cs) | `NEW ENCOUNTER` |
| 戦闘の各ターン開始 | [BattleManager.cs](GameEngine/Systems/BattleSystem/BattleManager.cs) | `BATTLE - Turn N` |
| ショップ表示 | [ConsoleGameInput.cs](GameEngine/Systems/ConsoleGameInput.cs) | `SHOP` |
| エンカウント結果 | [PostEncounterState.cs](GameEngine/Systems/StateMachine/States/PostEncounterState.cs) | `ENCOUNTER RESULTS` |
| ゲームオーバー | [GameOverState.cs](GameEngine/Systems/StateMachine/States/GameOverState.cs) | `GAME OVER` |

**重要**: 戦闘結果（勝利/敗北）表示後に `WaitForKeyPress()` を入れ、画面クリアで結果が消えないようにする。

---

## Phase 3: 戦闘画面の改善

**目標**: 各ターンがクリアな固定レイアウトで表示される。

```
═══════════════════ BATTLE - Turn 3 ═══════════════════

  Hero        [████████████........] 120/150 HP   AP:30 DP:10
  Dark Elf    [██████..............] 45/120 HP

  Attack:  [ Default ]   Melee    Magic     ← → Enter

───────────────────────────────────────────────────────
  Hero attacks Dark Elf with Melee!
  Dark Elf takes 28 damage!
  Dark Elf attacks Hero with Magic!
  Hero takes 12 damage!
───────────────────────────────────────────────────────
```

**変更点**:
- [BattleManager.cs](GameEngine/Systems/BattleSystem/BattleManager.cs):
  - `ExecuteBattle` ループ先頭で画面クリア + ステータスパネル描画
  - `DisplayBattleStatus` を `ConsoleRenderer.RenderStatusPanel` に置換
  - 勝利/敗北時の表示をボックス描画に変更
- [UserInteraction.cs](GameEngine/Systems/UserInteraction.cs):
  - `SelectAttackStrategy` を全選択肢を横並び表示 + ハイライトに改善
  - 既存の `ClearLastOutput()` ANSI パターンを活用

**勝利画面例**:
```
╔═══════════════════════════════════╗
║          VICTORY!                 ║
║  Dark Elf has been defeated!      ║
║  +15 Gold   +30 Experience        ║
╚═══════════════════════════════════╝
       Press any key...
```

---

## Phase 4: ショップUI改善

**現状の問題**:
- ポーション価格が表示されない（`ShopState.PotionPrice` は存在するが未使用）
- 武器ステータス（AP/DP）が表示されない（`WeaponInfo` に値はあるが名前しか出ない）
- 数字キー操作で、間違ったキーを押すと無言で退店
- 1回の操作で強制退店

**改善後のレイアウト**:
```
══════════════════════ SHOP ═══════════════════════════

  Gold: 150        Potions: 3        HP: 80/100

┌─────────────────────────────────────────────────────┐
│ > Buy Potion      (10 gold each, heals 10 HP)      │
│   Buy Weapon                                       │
│   Exit Shop                                        │
└─────────────────────────────────────────────────────┘
  ↑↓ to select, Enter to confirm

武器選択サブメニュー:
┌─────────────────────────────────────────────────────┐
│ > SWORD     AP +20  DP +5   HP +100                │
│   AXE       AP +30  DP +3   HP +80                 │
│   BOW       AP +35  DP +2   HP +70                 │
│   Back                                             │
└─────────────────────────────────────────────────────┘
  Currently equipped: Default
```

**変更ファイル**:
- [ConsoleGameInput.cs](GameEngine/Systems/ConsoleGameInput.cs) — `SelectShopAction` を全面書き換え:
  - 数字キー → `ConsoleRenderer.SelectFromMenu` (矢印キー)
  - ポーション価格・回復量を表示
  - 武器ステータス（AP/DP/HP）を表示
  - 「Back」オプション追加（武器サブメニュー）
  - 無効入力でも退店しない
- [EventManager.cs](GameEngine/Systems/EventManager.cs) — `HandleShopEvent` でショップをループ化:
  - Exit が選択されるまで `SelectShopAction` → `ProcessShopAction` を繰り返す
  - 購入ごとに `PlayerState` を更新して Gold/Potion 表示を最新化

---

## Phase 5: ポーション使用の改善

**現状の問題**:
- ポーション0本でもプロンプトが出る
- 回復量が表示されない
- 現在HPが表示されない
- スキップ方法がわかりにくい（"Q"入力）

**改善**:
- [ConsoleGameInput.cs](GameEngine/Systems/ConsoleGameInput.cs) — `SelectRestAction` 書き換え:
  - ポーション0本なら即座に `null` を返す（プロンプト表示なし）
  - HP と回復量を表示: `HP: 45/100  Potions: 3 (each heals 10 HP)`
  - 矢印キーメニューに変更:
    ```
    > Skip
      Use 1 Potion  (HP → 55/100)
      Use 2 Potions (HP → 65/100)
      Use 3 Potions (HP → 75/100)
    ```
  - HP満タンなら "HP is full!" と表示してスキップ

---

## Phase 6: 入力方式の統一

`ConsoleRenderer.SelectFromMenu` を全メニューで共有する。

| メニュー | 現状 | 改善後 |
|---|---|---|
| 攻撃戦略選択 | ←→ + Enter | `SelectFromMenu(Horizontal)` |
| ゲームアクション | ↑↓ + Enter | `SelectFromMenu(Vertical)` |
| ショップメニュー | 数字キー | `SelectFromMenu(Vertical)` |
| 武器選択 | 数字キー | `SelectFromMenu(Vertical)` |
| ポーション使用 | テキスト入力 | `SelectFromMenu(Vertical)` |

**変更ファイル**:
- [UserInteraction.cs](GameEngine/Systems/UserInteraction.cs) — `SelectAttackStrategy`, `SelectGameAction` を `SelectFromMenu` に委譲
- [ConsoleGameInput.cs](GameEngine/Systems/ConsoleGameInput.cs) — ショップ・ポーションメニューに適用

---

## Phase 7: 日本語メッセージの統一

現状、`UserInteraction.cs` のバリデーションメッセージは日本語、ゲーム本体は英語で混在。

→ ゲーム内の全ユーザー向けメッセージを英語に統一する。

**変更ファイル**:
- [UserInteraction.cs](GameEngine/Systems/UserInteraction.cs) — バリデーションメッセージ、スキップ案内等
- [GameFlowContext.cs](GameEngine/Systems/StateMachine/GameFlowContext.cs) — セーブ関連メッセージ

---

## 実装順序

```
Phase 1 (ConsoleRenderer) ─── 基盤。全フェーズの前提
  ├── Phase 2 (画面クリア) ─── Phase 1 に依存
  │     └── Phase 3 (戦闘画面) ─── Phase 2 に依存
  ├── Phase 6 (入力統一) ─── Phase 1 に依存
  │     ├── Phase 4 (ショップ) ─── Phase 6 に依存
  │     └── Phase 5 (ポーション) ─── Phase 6 に依存
  └── Phase 7 (言語統一) ─── Phase 1 に依存、他と並行可
```

推奨順: **1 → 6 → 2 → 7 → 3 → 4 → 5**

---

## 変更対象ファイル一覧

| ファイル | 変更内容 |
|---|---|
| `GameEngine/Systems/ConsoleRenderer.cs` | **新規作成** — 全UI出力の一元管理 |
| `GameEngine/Systems/GameSystem.cs` | RenderMessages を ConsoleRenderer 経由に変更 |
| `GameEngine/Systems/UserInteraction.cs` | SelectAttackStrategy/SelectGameAction を SelectFromMenu に委譲、日本語→英語 |
| `GameEngine/Systems/ConsoleGameInput.cs` | ショップUI全面書き換え、ポーション使用UI改善 |
| `GameEngine/Systems/BattleSystem/BattleManager.cs` | ターンごと画面クリア、HPバー表示、勝敗画面 |
| `GameEngine/Systems/EventManager.cs` | ショップをループ化 |
| `GameEngine/Systems/StateMachine/GameFlowContext.cs` | Console.WriteLine → ConsoleRenderer、日本語→英語 |
| `GameEngine/Systems/StateMachine/States/StartState.cs` | 画面クリア追加 |
| `GameEngine/Systems/StateMachine/States/EncounterState.cs` | 画面クリア追加 |
| `GameEngine/Systems/StateMachine/States/PostEncounterState.cs` | 画面クリア追加 |
| `GameEngine/Systems/StateMachine/States/GameOverState.cs` | 画面クリア追加 |

---

## 検証方法

1. `dotnet build` で全体ビルド成功を確認
2. `dotnet test` で既存テストが通ることを確認
3. `dotnet run --project ./GameEngine` で実際にプレイ:
   - 戦闘: 画面が毎ターンクリアされ、HPバーが見えること
   - 戦闘終了: 勝利/敗北画面が表示され、キー押下で次へ進むこと
   - ショップ: 価格・武器ステータスが見え、矢印キーで操作できること
   - ショップ: 複数回購入後に Exit で退店できること
   - ポーション: 0本時はプロンプトが出ないこと
   - ポーション: 回復量と現在HPが表示されること
   - 全メニュー: 矢印キー + Enter で操作統一されていること
