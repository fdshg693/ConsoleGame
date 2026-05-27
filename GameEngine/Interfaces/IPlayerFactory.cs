using GameEngine.DTOs;

namespace GameEngine.Interfaces
{
    /// <summary>
    /// <see cref="IPlayer"/> の生成・復元を担うファクトリ。新規プレイヤーの生成（設定既定値）と、
    /// セーブデータ（<see cref="PlayerSaveData"/>）からの完全復元の両経路を提供する。
    /// 合成起点での手組みを集約し、セッション復元（フェーズ3）で再利用する。
    /// </summary>
    public interface IPlayerFactory
    {
        /// <summary>設定の初期値から新規プレイヤーを生成する。</summary>
        IPlayer CreateNew(string name);

        /// <summary>
        /// セーブデータからプレイヤーを復元する。HP・レベル・経験値・ゴールド・ポーション・
        /// 装備武器・攻撃戦略・基礎ステータスを保存時の状態へ再構築する。
        /// </summary>
        IPlayer Restore(PlayerSaveData data);
    }
}
