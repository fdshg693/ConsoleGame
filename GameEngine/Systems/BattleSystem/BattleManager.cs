using GameEngine.Constants;
using GameEngine.DTOs;
using GameEngine.Factory;
using GameEngine.Interfaces;
using GameEngine.Mappers;
using GameEngine.Models;
using GameEngine.Systems;

namespace GameEngine.Systems.BattleSystem
{
    /// <summary>
    /// 戦闘全体の管理を行うクラス
    /// </summary>
    public class BattleManager
    {
        private readonly IPlayer _player;
        private readonly IGameInput _input;

        public BattleManager(IPlayer player, IGameInput input)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <summary>
        /// 戦闘を開始する
        /// </summary>
        public BattleResult StartBattle()
        {
            var messages = new List<GameMessage>();
            try
            {
                IEnemy enemy = EnemyFactory.CreateRandomEnemy();
                messages.Add(GameStateMapper.CreateMessage($"A wild {enemy.Name} appears!", MessageType.Combat));

                return ExecuteBattle(enemy, messages);
            }
            catch (Exception ex)
            {
                messages.Add(GameStateMapper.CreateMessage($"Error starting battle: {ex.Message}", MessageType.Error));
                return new BattleResult(BattleOutcome.Error, null, messages);
            }
        }

        /// <summary>
        /// 指定された敵との戦闘を実行する
        /// </summary>
        private BattleResult ExecuteBattle(IEnemy enemy, List<GameMessage> messages)
        {
            var battleTurn = 0;

            while (_player.IsAlive && enemy.IsAlive)
            {
                battleTurn++;

                // Clear screen and show status panel at start of each turn
                ConsoleRenderer.ClearScreen($"BATTLE - Turn {battleTurn}");
                var playerState = _player.ToPlayerState();
                var enemyState = enemy.ToEnemyState();
                ConsoleRenderer.RenderStatusPanel(playerState, enemyState);

                // プレイヤーのターン
            ExecutePlayerTurn(enemy, battleTurn, messages);

                // 敵が倒された場合
                if (!enemy.IsAlive)
                {
                    return HandleVictory(enemy, messages);
                }

                // 敵のターン
                ExecuteEnemyTurn(enemy, messages);

                // プレイヤーが倒された場合
                if (!_player.IsAlive)
                {
                    return HandleDefeat(enemy, messages);
                }

                // ターン終了時の状態表示
                DisplayBattleStatus(enemy, messages);
            }

            // 通常はここには到達しない
            return new BattleResult(BattleOutcome.Error, enemy, messages);
        }

        /// <summary>
        /// プレイヤーのターンを実行
        /// </summary>
        private void ExecutePlayerTurn(IEnemy enemy, int battleTurn, List<GameMessage> messages)
        {
            var battleState = new BattleState
            {
                TurnNumber = battleTurn,
                AvailableStrategies = new List<string>(AttackStrategyNames.All)
            };
            var playerState = _player.ToPlayerState();
            var enemyState = enemy.ToEnemyState();

            // 攻撃戦略をプレイヤーに選択させる
            var attackAction = _input.SelectAttackAction(battleState, playerState, enemyState);
            if (!PlayerActionValidator.IsValid(attackAction, out var errorMessage))
            {
                messages.Add(GameStateMapper.CreateMessage($"Invalid action: {errorMessage}", MessageType.Warning));
                attackAction = new AttackAction("Default");
            }

            var attackStrategyName = attackAction.StrategyName;
            _player.ChangeAttackStrategy(attackStrategyName);

            // 攻撃実行
            _player.Attack(enemy);
            messages.Add(GameStateMapper.CreateMessage($"{_player.Name} attacks {enemy.Name} with {attackStrategyName}!", MessageType.Combat));
        }

        /// <summary>
        /// 敵のターンを実行
        /// </summary>
        private void ExecuteEnemyTurn(IEnemy enemy, List<GameMessage> messages)
        {
            enemy.Attack(_player);
            messages.Add(GameStateMapper.CreateMessage($"{enemy.Name} attacks {_player.Name} with {enemy.AttackStrategy.GetAttackStrategyName()}!", MessageType.Combat));
        }

        /// <summary>
        /// 戦闘状態を表示
        /// </summary>
        private void DisplayBattleStatus(IEnemy enemy, List<GameMessage> messages)
        {
            ConsoleRenderer.WriteSeparator();
            ConsoleRenderer.RenderHPBar(_player.Name, _player.HP, _player.MaxHP);
            ConsoleRenderer.RenderHPBar(enemy.Name, enemy.HP, enemy.MaxHP);
        }

        /// <summary>
        /// 勝利時の処理
        /// </summary>
        private BattleResult HandleVictory(IEnemy enemy, List<GameMessage> messages)
        {
            messages.Add(GameStateMapper.CreateMessage($"{enemy.Name} has been defeated!", MessageType.Success));
            GameRecord.RecordWin();
            messages.AddRange(GameRecord.GetRecordMessages());

            _player.DefeatEnemy(enemy);

            ConsoleRenderer.WriteResultBox("VICTORY!", new[] { $"{enemy.Name} has been defeated!" }, true);
            ConsoleRenderer.WaitForKeyPress();

            return new BattleResult(BattleOutcome.Victory, enemy, messages);
        }

        /// <summary>
        /// 敗北時の処理
        /// </summary>
        private BattleResult HandleDefeat(IEnemy enemy, List<GameMessage> messages)
        {
            messages.Add(GameStateMapper.CreateMessage($"{_player.Name} has fallen...", MessageType.Error));
            GameRecord.RecordLoss();
            messages.AddRange(GameRecord.GetRecordMessages());

            ConsoleRenderer.WriteResultBox("DEFEAT", new[] { $"{_player.Name} has fallen..." }, false);
            ConsoleRenderer.WaitForKeyPress();

            return new BattleResult(BattleOutcome.Defeat, enemy, messages);
        }
    }

    /// <summary>
    /// 戦闘結果を表すクラス
    /// </summary>
    public class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public IEnemy? Enemy { get; }
        public IReadOnlyList<GameMessage> Messages { get; }

        public BattleResult(BattleOutcome outcome, IEnemy? enemy, IReadOnlyList<GameMessage> messages)
        {
            Outcome = outcome;
            Enemy = enemy;
            Messages = messages ?? Array.Empty<GameMessage>();
        }

        public bool IsVictory => Outcome == BattleOutcome.Victory;
        public bool IsDefeat => Outcome == BattleOutcome.Defeat;
        public bool IsError => Outcome == BattleOutcome.Error;
    }

    /// <summary>
    /// 戦闘の結果を表す列挙型
    /// </summary>
    public enum BattleOutcome
    {
        Victory,
        Defeat,
        Error
    }
}
