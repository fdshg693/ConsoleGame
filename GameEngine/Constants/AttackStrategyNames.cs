namespace GameEngine.Constants
{
    /// <summary>
    /// 攻撃戦略名の定数定義
    /// AttackStrategy, GameStateMapper, PlayerActionValidator 等で共通利用
    /// </summary>
    public static class AttackStrategyNames
    {
        public const string Default = "Default";
        public const string Melee = "Melee";
        public const string Magic = "Magic";

        /// <summary>
        /// 全ての有効な戦略名のリスト
        /// </summary>
        public static readonly IReadOnlyList<string> All = new[] { Default, Melee, Magic };
    }
}
