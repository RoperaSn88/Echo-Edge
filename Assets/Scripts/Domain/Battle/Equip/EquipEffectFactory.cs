using System;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 装備品のインデックスに対応する効果を生成するファクトリ
    /// </summary>
    public static class EquipEffectFactory
    {
        /// <summary>
        /// インデックスに対応する装備品エフェクトを生成する
        /// </summary>
        public static IEquipEffect Create(int index)
        {
            switch (index)
            {
                case 0: return new MagneticCore();
                case 1: return new EnergyWall();
                case 2: return new Pusher(PusherDirection.Up);
                case 3: return new Pusher(PusherDirection.Down);
                case 4: return new Pusher(PusherDirection.Left);
                case 5: return new Pusher(PusherDirection.Right);
                default: throw new InvalidOperationException($"不明な装備品インデックスです: {index}");
            }
        }
    }
}
