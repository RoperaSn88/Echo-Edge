namespace UnityEngine
{
    public interface ISelectInterface
    {
        /// <summary>
        /// 選択された場合の処理
        /// </summary>
        public void OnSelect();

        /// <summary>
        /// 選択解除された場合の処理
        /// </summary>
        public void OnDeselect();
    }
}