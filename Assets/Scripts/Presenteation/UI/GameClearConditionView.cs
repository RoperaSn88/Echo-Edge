using TMPro;
using UnityEngine;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// ゲームクリアの条件を表示するUIのプレゼンタークラス。
    /// </summary>
    public class GameClearConditionView : MonoBehaviour
    {
        private static GameClearConditionView _instance;

        [SerializeField]
        private TextMeshProUGUI _baseText;

        [SerializeField]
        private TextMeshProUGUI _conditionValueText;

        public static GameClearConditionView Instance => _instance;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _instance = null;
        }

        public void RefreshText(string context, int value)
        {
            _baseText.text = context;
            _conditionValueText.text = value.ToString();
        }
    }
}
