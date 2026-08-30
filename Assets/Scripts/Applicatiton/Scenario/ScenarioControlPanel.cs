using Domain.Scenario.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// 早送り・スキップ・ログ表示を操作するボタン群を扱うクラス。
    /// 早送りボタンはクリックするたびに OFF → 速度1 → 速度2 → OFF と循環し、
    /// スキップボタンはクリックするたびに ON/OFF を切り替えるトグルとして振る舞う。
    /// 実際の挙動の切り替えは <see cref="ScenarioScreen"/> に委譲する。
    /// </summary>
    public class ScenarioControlPanel : MonoBehaviour
    {
        [SerializeField] private ScenarioScreen _screen;

        [SerializeField] private Button _fastForwardButton;
        [SerializeField] private TextMeshProUGUI _fastForwardLabel;

        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipLabel;

        [SerializeField] private Button _logButton;

        private FastForwardMode _fastForwardMode = FastForwardMode.Off;
        private bool _isSkipOn;

        private void Awake()
        {
            _fastForwardButton.onClick.AddListener(OnClickFastForward);
            _skipButton.onClick.AddListener(OnClickSkip);
            _logButton.onClick.AddListener(OnClickLog);

            UpdateLabels();
        }

        private void OnClickFastForward()
        {
            _fastForwardMode = _fastForwardMode switch
            {
                FastForwardMode.Off => FastForwardMode.Speed1,
                FastForwardMode.Speed1 => FastForwardMode.Speed2,
                _ => FastForwardMode.Off,
            };
            _screen.SetFastForward(_fastForwardMode);
            UpdateLabels();
        }

        private void OnClickSkip()
        {
            _isSkipOn = !_isSkipOn;
            _screen.SetSkip(_isSkipOn);
            UpdateLabels();
        }

        private void OnClickLog()
        {
            _screen.ToggleLog();
        }

        private void UpdateLabels()
        {
            if (_fastForwardLabel != null)
            {
                // OFF → 0、速度1 → 1、速度2 → 2 のように数字を振って表示する。
                _fastForwardLabel.text = $"早送り{(int)_fastForwardMode}";
            }

            if (_skipLabel != null)
            {
                _skipLabel.text = _isSkipOn ? "スキップ ON" : "スキップ OFF";
            }
        }
    }
}
