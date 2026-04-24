namespace UnityEngine
{
    public interface ISelectInterface
    {
        /// <summary>
        /// カーソルで選択された場合の処理
        /// </summary>
        public void OnSelect();

        /// <summary>
        /// クリックで決定された場合の処理
        /// </summary>
        public void OnDecide();

        /// <summary>
        /// カーソルの選択が外れた場合の処理
        /// </summary>
        public void OnDeselect();
    }
}