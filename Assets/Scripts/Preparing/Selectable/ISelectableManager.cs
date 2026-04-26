using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
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