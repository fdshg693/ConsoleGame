using System;
using GameEngine.Constants;

namespace GameEngine.DTOs
{
    /// <summary>
    /// プレイヤーの行動を表す抽象クラス
    /// UI層からコアロジック層へのコマンドとして使用
    /// </summary>
    public abstract class PlayerAction
    {
        public ActionType Type { get; protected set; }
    }

    /// <summary>
    /// アクションの種類
    /// </summary>
    public enum ActionType
    {
        Attack,      // 攻撃
        UseItem,     // アイテム使用
        Shop,        // ショップアクション
        Continue,    // 継続
        Quit,        // 終了
        Save,        // セーブ
        Load,        // ロード
        Rest         // 休憩
    }

    /// <summary>
    /// 戦闘アクション
    /// </summary>
    public class AttackAction : PlayerAction
    {
        public string StrategyName { get; set; } = string.Empty;

        public AttackAction()
        {
            Type = ActionType.Attack;
        }

        public AttackAction(string strategyName)
        {
            Type = ActionType.Attack;
            StrategyName = strategyName;
        }
    }

    /// <summary>
    /// アイテム使用アクション
    /// </summary>
    public class UseItemAction : PlayerAction
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public UseItemAction()
        {
            Type = ActionType.UseItem;
        }

        public UseItemAction(string itemName, int quantity = 1)
        {
            Type = ActionType.UseItem;
            ItemName = itemName;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// ショップアクション
    /// </summary>
    public class ShopAction : PlayerAction
    {
        public ShopActionType ShopType { get; set; }
        public string? ItemName { get; set; }
        public int Quantity { get; set; }

        public ShopAction()
        {
            Type = ActionType.Shop;
        }

        public ShopAction(ShopActionType shopType, string? itemName = null, int quantity = 1)
        {
            Type = ActionType.Shop;
            ShopType = shopType;
            ItemName = itemName;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// ショップアクションの種類
    /// </summary>
    public enum ShopActionType
    {
        BuyPotion,   // ポーション購入
        BuyWeapon,   // 武器購入
        SellItem,    // アイテム売却（将来の拡張用）
        Exit         // ショップを出る
    }

    /// <summary>
    /// ゲーム制御アクション（継続/終了/セーブ/ロード）
    /// </summary>
    public class GameControlAction : PlayerAction
    {
        public GameControlAction(ActionType type)
        {
            if (type != ActionType.Continue &&
                type != ActionType.Quit &&
                type != ActionType.Save &&
                type != ActionType.Load)
            {
                throw new ArgumentException(
                    $"Invalid action type for GameControlAction: {type}. " +
                    "Only Continue, Quit, Save, and Load are allowed.");
            }
            Type = type;
        }
    }

    /// <summary>
    /// 休憩アクション
    /// </summary>
    public class RestAction : PlayerAction
    {
        public RestAction()
        {
            Type = ActionType.Rest;
        }
    }

    /// <summary>
    /// アクションの検証ヘルパークラス
    /// </summary>
    public static class PlayerActionValidator
    {
        /// <summary>
        /// AttackActionの検証
        /// </summary>
        public static bool IsValid(AttackAction action, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(action.StrategyName))
            {
                errorMessage = "Strategy name cannot be empty";
                return false;
            }

            if (!AttackStrategyNames.All.Any(s => s.Equals(action.StrategyName, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"Invalid strategy name: {action.StrategyName}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// UseItemActionの検証
        /// </summary>
        public static bool IsValid(UseItemAction action, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(action.ItemName))
            {
                errorMessage = "Item name cannot be empty";
                return false;
            }

            if (action.Quantity <= 0)
            {
                errorMessage = "Quantity must be greater than 0";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// ShopActionの検証
        /// </summary>
        public static bool IsValid(ShopAction action, out string? errorMessage)
        {
            if (action.ShopType == ShopActionType.BuyWeapon && string.IsNullOrWhiteSpace(action.ItemName))
            {
                errorMessage = "Weapon name is required for BuyWeapon action";
                return false;
            }

            if (action.Quantity <= 0)
            {
                errorMessage = "Quantity must be greater than 0";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
