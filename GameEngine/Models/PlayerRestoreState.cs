namespace GameEngine.Models
{
    /// <summary>
    /// セーブデータから <see cref="Player"/> を復元する際に、設定既定値の代わりに用いる基礎ステータス。
    /// <see cref="Player"/> のコンストラクタに任意で渡し、null の場合は <c>GameConfig</c> の初期値で生成する
    /// （新規プレイヤー）。値は装備武器ボーナスを含まない「基礎値」で表す。
    /// </summary>
    public sealed class PlayerRestoreState
    {
        /// <summary>武器ボーナスを除いた基礎攻撃力。</summary>
        public required int BaseAP { get; init; }

        /// <summary>武器ボーナスを除いた基礎最大 HP（= 保存時 MaxHP − 装備武器 HP）。</summary>
        public required int BaseHP { get; init; }

        /// <summary>武器ボーナスを除いた基礎防御力。</summary>
        public required int BaseDP { get; init; }

        /// <summary>復元時の現在 HP。</summary>
        public required int CurrentHP { get; init; }
    }
}
