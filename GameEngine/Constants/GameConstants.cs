namespace GameEngine.Constants
{
    /// <summary>
    /// 設定ファイル外の固定ゲーム定数を保持するクラス。
    /// 旧版は <c>GameConfigLoader.Instance</c>（静的シングルトン）へ委譲するラッパーだったが、
    /// 設定値はコンストラクタ注入（<see cref="GameEngine.Configuration.GameConfig"/>）へ移行したため撤去済み。
    /// ここに残すのは設定で外部化しない純粋な固定値のみ。
    /// </summary>
    public static class GameConstants
    {
        // Attack strategy damage ranges（設定外の固定値）
        public static class AttackDamage
        {
            public const int DefaultMin = 8;
            public const int DefaultMax = 10;

            public const int MeleeMin = 10;
            public const int MeleeMax = 16;

            public const int MagicMin = 0;
            public const int MagicMax = 25;
        }
    }
}
