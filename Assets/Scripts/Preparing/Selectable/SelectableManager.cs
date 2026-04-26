using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// このクラスのSelectingが呼ばれた時、このクラスの変数で指定されているオブジェクトしか選択できないようにして。
    /// </summary>
    public class SelectableManager: MonoBehaviour, ISelectableManager
    {
        public UniTask<ISelectableManager> Selecting()
        {
            throw new System.NotImplementedException();
        }
    }
}