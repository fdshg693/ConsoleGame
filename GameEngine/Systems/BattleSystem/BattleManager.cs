using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;

namespace GameEngine.Systems.BattleSystem
{
    /// <summary>
    /// ターン制戦闘をステップ駆動で進める。内部 while ループは持たず、
    /// <see cref="StartBattle"/> で1戦闘を開始し、<see cref="SubmitPlayerTurn"/> を
    /// 外部から繰り返し呼ぶことで1ターンずつ進行する。描画・入力には依存しない
    /// （結果は <see cref="BattleStepResult"/> として返し、描画はホスト側 State が担う）。
    /// </summary>
    public class BattleManager
    {
        private readonly IPlayer _player;
        private readonly IEnemyFactory _enemyFactory;
        private readonly IGameRecord _gameRecord;

        private IEnemy? _enemy;
        private int _turn;

        public BattleManager(IPlayer player, IEnemyFactory enemyFactory, IGameRecord gameRecord)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _enemyFactory = enemyFactory ?? throw new ArgumentNullException(nameof(enemyFactory));
            _gameRecord = gameRecord ?? throw new ArgumentNullException(nameof(gameRecord));
        }

        /// <summary>進行中の敵（戦闘終了後は null）。</summary>
        public IEnemy? CurrentEnemy => _enemy;

        /// <summary>これまでに消化したターン数。</summary>
        public int TurnNumber => _turn;

        /// <summary>戦闘が進行中か（敵生存・プレイヤー生存）。</summary>
        public bool IsBattleActive => _enemy != null && _player.IsAlive && _enemy.IsAlive;

        /// <summary>
        /// 戦闘を開始する。敵を1体生成し、初期 <see cref="BattleStepResult"/>（進行中）を返す。
        /// </summary>
        public BattleStepResult StartBattle()
        {
            try
            {
                _enemy = _enemyFactory.CreateRandomEnemy();
                _turn = 0;

                var messages = new List<GameMessage>
                {
                    GameStateMapper.CreateMessage($"A wild {_enemy.Name} appears!", MessageType.Combat)
                };

                return new BattleStepResult(
                    BattleOutcome.InProgress,
                    BuildBattleState(ended: false, playerWon: false, lastAction: null),
                    _enemy.ToEnemyState(),
                    _player.ToPlayerState(),
                    messages);
            }
            catch (Exception ex)
            {
                _enemy = null;
                var messages = new List<GameMessage>
                {
                    GameStateMapper.CreateMessage($"Error starting battle: {ex.Message}", MessageType.Error)
                };
                return new BattleStepResult(BattleOutcome.Error, null, null, _player.ToPlayerState(), messages);
            }
        }

        /// <summary>
        /// プレイヤー1ターン + 敵1ターンを進める。勝敗が決した場合は決着を <see cref="BattleStepResult.Outcome"/> で返す。
        /// </summary>
        public BattleStepResult SubmitPlayerTurn(AttackAction action)
        {
            var messages = new List<GameMessage>();

            if (_enemy == null)
            {
                messages.Add(GameStateMapper.CreateMessage("No active battle to submit a turn for.", MessageType.Error));
                return new BattleStepResult(BattleOutcome.Error, null, null, _player.ToPlayerState(), messages);
            }

            var enemy = _enemy;
            _turn++;

            // 攻撃戦略の検証（不正なら Default にフォールバック）
            if (action == null)
            {
                messages.Add(GameStateMapper.CreateMessage("Invalid action: null", MessageType.Warning));
                action = new AttackAction(AttackStrategyNames.Default);
            }
            else if (!PlayerActionValidator.IsValid(action, out var errorMessage))
            {
                messages.Add(GameStateMapper.CreateMessage($"Invalid action: {errorMessage}", MessageType.Warning));
                action = new AttackAction(AttackStrategyNames.Default);
            }

            var strategyName = action.StrategyName;
            _player.ChangeAttackStrategy(strategyName);

            // プレイヤーのターン
            int enemyHpBefore = enemy.HP;
            _player.Attack(enemy);
            int damageDealt = Math.Max(0, enemyHpBefore - enemy.HP);
            messages.Add(GameStateMapper.CreateMessage($"{_player.Name} attacks {enemy.Name} with {strategyName}!", MessageType.Combat));

            // 敵が倒された場合（勝利）
            if (!enemy.IsAlive)
            {
                messages.Add(GameStateMapper.CreateMessage($"{enemy.Name} has been defeated!", MessageType.Success));
                _gameRecord.RecordWin();
                messages.AddRange(_gameRecord.GetRecordMessages());
                _player.DefeatEnemy(enemy);

                var victory = new BattleStepResult(
                    BattleOutcome.Victory,
                    BuildBattleState(ended: true, playerWon: true, lastAction: strategyName, damageDealt: damageDealt),
                    enemy.ToEnemyState(),
                    _player.ToPlayerState(),
                    messages);
                _enemy = null;
                return victory;
            }

            // 敵のターン
            int playerHpBefore = _player.HP;
            enemy.Attack(_player);
            int damageTaken = Math.Max(0, playerHpBefore - _player.HP);
            messages.Add(GameStateMapper.CreateMessage($"{enemy.Name} attacks {_player.Name} with {enemy.AttackStrategy.GetAttackStrategyName()}!", MessageType.Combat));

            // プレイヤーが倒された場合（敗北）
            if (!_player.IsAlive)
            {
                messages.Add(GameStateMapper.CreateMessage($"{_player.Name} has fallen...", MessageType.Error));
                _gameRecord.RecordLoss();
                messages.AddRange(_gameRecord.GetRecordMessages());

                var defeat = new BattleStepResult(
                    BattleOutcome.Defeat,
                    BuildBattleState(ended: true, playerWon: false, lastAction: strategyName, damageDealt: damageDealt, damageTaken: damageTaken),
                    enemy.ToEnemyState(),
                    _player.ToPlayerState(),
                    messages);
                _enemy = null;
                return defeat;
            }

            // 戦闘継続
            return new BattleStepResult(
                BattleOutcome.InProgress,
                BuildBattleState(ended: false, playerWon: false, lastAction: strategyName, damageDealt: damageDealt, damageTaken: damageTaken),
                enemy.ToEnemyState(),
                _player.ToPlayerState(),
                messages);
        }

        private BattleState BuildBattleState(bool ended, bool playerWon, string? lastAction, int damageDealt = 0, int damageTaken = 0)
        {
            return new BattleState
            {
                TurnNumber = _turn,
                AvailableStrategies = new List<string>(AttackStrategyNames.All),
                LastPlayerAction = lastAction,
                LastDamageDealt = damageDealt,
                LastDamageTaken = damageTaken,
                PlayerWon = playerWon,
                BattleEnded = ended
            };
        }
    }

    /// <summary>
    /// 1ステップ分の戦闘結果。進行中/勝利/敗北/エラーと、その時点の状態 DTO・メッセージを保持する。
    /// </summary>
    public class BattleStepResult
    {
        public BattleOutcome Outcome { get; }
        public BattleState? Battle { get; }
        public EnemyState? Enemy { get; }
        public PlayerState Player { get; }
        public IReadOnlyList<GameMessage> Messages { get; }

        public BattleStepResult(
            BattleOutcome outcome,
            BattleState? battle,
            EnemyState? enemy,
            PlayerState player,
            IReadOnlyList<GameMessage> messages)
        {
            Outcome = outcome;
            Battle = battle;
            Enemy = enemy;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Messages = messages ?? Array.Empty<GameMessage>();
        }

        public bool IsOver => Outcome != BattleOutcome.InProgress;
        public bool IsVictory => Outcome == BattleOutcome.Victory;
        public bool IsDefeat => Outcome == BattleOutcome.Defeat;
        public bool IsError => Outcome == BattleOutcome.Error;
    }

    /// <summary>
    /// 戦闘ステップの結果種別。
    /// </summary>
    public enum BattleOutcome
    {
        /// <summary>戦闘継続中（次のターン入力が必要）。</summary>
        InProgress,
        Victory,
        Defeat,
        Error
    }
}
