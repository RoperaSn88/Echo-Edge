using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Preparing
{
    /// <summary>
    /// 選べるテキストをマネージするクラス
    /// </summary>
    public interface ISelectableManager
    {
        /// <summary>
        /// 選び始める時
        /// </summary>
        /// <returns></returns>
        public UniTask<ISelectableManager> Selecting();
    }
}
