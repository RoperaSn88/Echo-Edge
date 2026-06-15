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

        private IStageClearObjective _objective;

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

        public void SetObjective(IStageClearObjective objective)
        {
            _objective = objective;
            RefreshText();
        }

        public void RefreshText()
        {
            if (_baseText != null)
            {
                _baseText.text = _objective?.ObjectiveBaseText ?? string.Empty;
            }

            if (_conditionValueText != null)
            {
                _conditionValueText.text = _objective?.ObjectiveConditionValue ?? string.Empty;
            }
        }
    }
}
