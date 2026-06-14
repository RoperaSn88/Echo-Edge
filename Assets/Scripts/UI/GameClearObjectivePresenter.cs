using TMPro;
using UnityEngine;

namespace UI
{
    public class GameClearObjectivePresenter : MonoBehaviour
    {
        private static GameClearObjectivePresenter _instance;

        [SerializeField]
        private TextMeshProUGUI _baseText;

        [SerializeField]
        private TextMeshProUGUI _conditionValueText;

        [SerializeField]
        private string _baseMessage = "残りの敵はあと";

        [SerializeField]
        private int _conditionValue;

        public static GameClearObjectivePresenter Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            RefreshText();
        }

        public void SetConditionValue(int conditionValue)
        {
            _conditionValue = Mathf.Max(0, conditionValue);
            RefreshText();
        }

        private void RefreshText()
        {
            if (_baseText != null)
            {
                _baseText.text = _baseMessage;
            }

            if (_conditionValueText != null)
            {
                _conditionValueText.text = _conditionValue.ToString();
            }
        }
    }
}
