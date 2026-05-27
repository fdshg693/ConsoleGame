namespace GameEngine.DTOs
{
    /// <summary>
    /// ステップ駆動エンジンが「次の <c>Step</c> で受け取るべき入力の種別」を表す。
    /// ホスト（コンソール/API）はこれを見て、対応する行動を取得して <c>Step</c> に渡す。
    /// </summary>
    public enum ExpectedInput
    {
        /// <summary>入力不要（エンジンが自動で次の判断点まで前進する）。</summary>
        None,

        /// <summary>戦闘ターンの攻撃行動（<see cref="AttackAction"/>）。</summary>
        Attack,

        /// <summary>ショップでの行動（<see cref="ShopAction"/>）。</summary>
        Shop,

        /// <summary>休憩でのアイテム使用（<see cref="UseItemAction"/>。null はスキップ）。</summary>
        Rest,

        /// <summary>エンカウント後の進行選択（<see cref="GameActionChoice"/>）。</summary>
        GameAction
    }

    /// <summary>
    /// 1ステップ分の入力を運ぶキャリア。<see cref="ExpectedInput"/> に対応するフィールドのみが設定される。
    /// API ホストはリクエストボディから、コンソールホストは <c>IGameInput</c> の戻り値から組み立てる。
    /// </summary>
    public sealed class PlayerInput
    {
        public AttackAction? Attack { get; }
        public ShopAction? Shop { get; }
        public UseItemAction? Rest { get; }
        public GameActionChoice? Progress { get; }

        private PlayerInput(AttackAction? attack, ShopAction? shop, UseItemAction? rest, GameActionChoice? progress)
        {
            Attack = attack;
            Shop = shop;
            Rest = rest;
            Progress = progress;
        }

        /// <summary>入力不要なステップ用の空入力。</summary>
        public static readonly PlayerInput None = new(null, null, null, null);

        public static PlayerInput ForAttack(AttackAction action) => new(action, null, null, null);
        public static PlayerInput ForShop(ShopAction action) => new(null, action, null, null);

        /// <summary>休憩入力。<paramref name="action"/> が null の場合はスキップを意味する。</summary>
        public static PlayerInput ForRest(UseItemAction? action) => new(null, null, action, null);
        public static PlayerInput ForProgress(GameActionChoice choice) => new(null, null, null, choice);
    }
}
