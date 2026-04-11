using System;
using System.Threading;
using UI.Weapon;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

public class WeaponPresenter : MonoBehaviour
{
    private int _selectWeaponModelNumber;
    
    [SerializeField, Tooltip("武器の情報を表示するパネルのイメージ")]
    private Image _weaponPanelImage;
    
    [SerializeField, Tooltip("武器の名前を表示するテキスト")]
    private TextMeshProUGUI _weaponNameText;
    
    [SerializeField, Tooltip("武器の説明を表示するテキスト")]
    private TextMeshProUGUI _weaponDescriptionText;
    
    [SerializeField, Tooltip("武器のアイコンを表示するイメージ")]
    private Image _weaponIconImage;
    
    [SerializeField, Tooltip("武器のアイコンのマスクイメージ")]
    private Image _weaponIconMaskImage;
    
    [SerializeField, Tooltip("武器のコストを表示するクラス")]
    private WeaponCostClass _weaponCostClass;
    
    [SerializeField, Tooltip("武器のコストを表示するテキスト")]
    private TextMeshProUGUI _weaponCostText;
    
    private const float AppearTime = 0.5f;
    
    /// <summary>
    /// UIを表示させる
    /// </summary>
    public async UniTask AppearUIs(CancellationToken ct)
    {
        gameObject.SetActive(true);
        try
        {
            await UniTask.WhenAll(
                _weaponPanelImage.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponNameText.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponDescriptionText.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponIconImage.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponIconMaskImage.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponCostText.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct),
                _weaponCostClass.AppearWeaponCost(ct)
            );
        }
        catch
        {
            // キャンセルされたときはUIを瞬時に表示する
            _weaponPanelImage.color = new Color(_weaponPanelImage.color.r, _weaponPanelImage.color.g, _weaponPanelImage.color.b, 1);
            _weaponNameText.color = new Color(_weaponNameText.color.r, _weaponNameText.color.g, _weaponNameText.color.b, 1);
            _weaponDescriptionText.color = new Color(_weaponDescriptionText.color.r, _weaponDescriptionText.color.g, _weaponDescriptionText.color.b, 1);
            _weaponIconImage.color = new Color(_weaponIconImage.color.r, _weaponIconImage.color.g, _weaponIconImage.color.b, 1);
            _weaponIconMaskImage.color = new Color(_weaponIconMaskImage.color.r, _weaponIconMaskImage.color.g, _weaponIconMaskImage.color.b, 1);
            _weaponCostText.color = new Color(_weaponCostText.color.r, _weaponCostText.color.g, _weaponCostText.color.b, 1);
        }
    }
    
    /// <summary>
    /// UIを非表示させる
    /// </summary>
    public async UniTask DisappearUIs(CancellationToken ct)
    {
        try
        {
            await UniTask.WhenAll(
                _weaponPanelImage.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponNameText.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponDescriptionText.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponIconImage.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponIconMaskImage.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponCostText.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct),
                _weaponCostClass.DisappearWeaponCost(ct)
            );
        }
        catch
        {
            _weaponPanelImage.color = new Color(_weaponPanelImage.color.r, _weaponPanelImage.color.g,
                _weaponPanelImage.color.b, 0);
            _weaponNameText.color =
                new Color(_weaponNameText.color.r, _weaponNameText.color.g, _weaponNameText.color.b, 0);
            _weaponDescriptionText.color = new Color(_weaponDescriptionText.color.r, _weaponDescriptionText.color.g,
                _weaponDescriptionText.color.b, 0);
            _weaponIconImage.color = new Color(_weaponIconImage.color.r, _weaponIconImage.color.g,
                _weaponIconImage.color.b, 0);
            _weaponIconMaskImage.color = new Color(_weaponIconMaskImage.color.r, _weaponIconMaskImage.color.g,
                _weaponIconMaskImage.color.b, 0);
            _weaponCostText.color =
                new Color(_weaponCostText.color.r, _weaponCostText.color.g, _weaponCostText.color.b, 0);
        }
        finally
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// UIを瞬時に非表示させる
    /// </summary>
    public void HideUIs()
    {
        _weaponPanelImage.color = new Color(_weaponPanelImage.color.r, _weaponPanelImage.color.g, _weaponPanelImage.color.b, 0);
        _weaponNameText.color = new Color(_weaponNameText.color.r, _weaponNameText.color.g, _weaponNameText.color.b, 0);
        _weaponDescriptionText.color = new Color(_weaponDescriptionText.color.r, _weaponDescriptionText.color.g, _weaponDescriptionText.color.b, 0);
        _weaponIconImage.color = new Color(_weaponIconImage.color.r, _weaponIconImage.color.g, _weaponIconImage.color.b, 0);
        _weaponIconMaskImage.color = new Color(_weaponIconMaskImage.color.r, _weaponIconMaskImage.color.g, _weaponIconMaskImage.color.b, 0);
        _weaponCostText.color = new Color(_weaponCostText.color.r, _weaponCostText.color.g, _weaponCostText.color.b, 0);
        _weaponCostClass.HideWeaponCost();
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 選んだ武器の情報を表示する
    /// </summary>
    /// <param name="model"></param>
    public void SetWeapon(WeaponModel model)
    {
        _weaponNameText.text = model.WeaponName;
        _weaponDescriptionText.text = model.Description;
        _weaponIconImage.sprite = model.WeaponSprite;
        
        _weaponCostClass.SetWeaponCost(model.WeaponCost);
        _weaponCostText.text = "コスト: " + model.WeaponCost;
    }
    
    /// <summary>
    /// コスト回りをまとめたクラス
    /// </summary>
    [Serializable]
    private class WeaponCostClass
    {
        [Tooltip("武器のコストの配列")]
        public Image[] WeaponCostImages = new Image[10];
    
        [Tooltip("コスト有効化の画像")]
        public Sprite WeaponCostImage;
    
        [Tooltip("コスト無効化の画像")]
        public Sprite WeaponCostDisableImage;
        
        public void SetWeaponCost(int cost)
        {
            for (int i = 0; i < WeaponCostImages.Length; i++)
            {
                if (i < cost)
                {
                    WeaponCostImages[i].sprite = WeaponCostImage;
                }
                else
                {
                    WeaponCostImages[i].sprite = WeaponCostDisableImage;
                }
            }
        }
        
        public async UniTask AppearWeaponCost(CancellationToken ct)
        {
            try
            {
                foreach (var weaponCostImage in WeaponCostImages)
                {
                    weaponCostImage.DOFade(1, AppearTime).From(0).ToUniTask(cancellationToken: ct);
                }
            }
            catch
            {
                foreach (var weaponCostImage in WeaponCostImages)
                {
                    weaponCostImage.color = new Color(weaponCostImage.color.r, weaponCostImage.color.g, weaponCostImage.color.b, 1);
                }
            }
        }
        
        public async UniTask DisappearWeaponCost(CancellationToken ct)
        {
            try
            {
                foreach (var weaponCostImage in WeaponCostImages)
                {
                    weaponCostImage.DOFade(0, AppearTime).From(1).ToUniTask(cancellationToken: ct);
                }
            }
            catch
            {
                foreach (var VARIABLE in WeaponCostImages)
                {
                    VARIABLE.color = new Color(VARIABLE.color.r, VARIABLE.color.g, VARIABLE.color.b, 0);
                }
            }
        }
        
        public void HideWeaponCost()
        {
            foreach (var weaponCostImage in WeaponCostImages)
            {
                weaponCostImage.color = new Color(weaponCostImage.color.r, weaponCostImage.color.g, weaponCostImage.color.b, 0);
            }
        }
    }
}
