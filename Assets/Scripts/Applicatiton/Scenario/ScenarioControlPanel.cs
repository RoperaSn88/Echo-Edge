using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// 自動再生・早送り・スキップ・ログ表示を操作するボタン群を扱うクラス。
    /// 自動再生・早送り・スキップの各ボタンはクリックするたびに ON/OFF を切り替えるトグルとして振る舞い、
    /// 実際の挙動の切り替えは <see cref="ScenarioScreen"/> に委譲する。
    /// </summary>
    public class ScenarioControlPanel : MonoBehaviour
    {
        [SerializeField] private ScenarioScreen _screen;

        [SerializeField] private Button _autoPlayButton;
        [SerializeField] private TextMeshProUGUI _autoPlayLabel;

        [SerializeField] private Button _fastForwardButton;
        [SerializeField] private TextMeshProUGUI _fastForwardLabel;

        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _skipLabel;

        [SerializeField] private Button _logButton;

        private bool _isAutoPlayOn;
        private bool _isFastForwardOn;
        private bool _isSkipOn;

        private void Awake()
        {
            _autoPlayButton.onClick.AddListener(OnClickAutoPlay);
            _fastForwardButton.onClick.AddListener(OnClickFastForward);
            _skipButton.onClick.AddListener(OnClickSkip);
            _logButton.onClick.AddListener(OnClickLog);

            UpdateLabels();
        }

        private void OnClickAutoPlay()
        {
            _isAutoPlayOn = !_isAutoPlayOn;
            _screen.SetAutoPlay(_isAutoPlayOn);
            UpdateLabels();
        }

        private void OnClickFastForward()
        {
            _isFastForwardOn = !_isFastForwardOn;
            _screen.SetFastForward(_isFastForwardOn);
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
            _autoPlayLabel.text = _isAutoPlayOn ? "自動再生:ON" : "自動再生:OFF";
            _fastForwardLabel.text = _isFastForwardOn ? "早送り:ON" : "早送り:OFF";
            _skipLabel.text = _isSkipOn ? "スキップ:ON" : "スキップ:OFF";
        }
    }
}
