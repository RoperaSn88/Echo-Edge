using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using UnityEngine;

public class PlayerView: MonoBehaviour
{
    public static PlayerView Instance;

    [SerializeField]
    private Transform _transform;
    public Transform Transform => _transform;

    [SerializeField]
    private Animator _animator;

    public Animator Animator => _animator; 

    [SerializeField]
    private SpriteRenderer _renderer;

    /// <summary>
    /// 現在のプレイヤーのスプライト
    /// </summary>
    public Sprite CurrentSprite => _renderer.sprite;

    private readonly Vector3 SkillRotationAngle = new Vector3(0, -40f, 0);
    private const float AnimTime = 0.6f;

    /// <summary>
    /// 最後に選んだスキル番号を保持するフィールド
    /// </summary>
    private int _skillNumber;

    void Awake()
    {
        Instance = this;
    }
    public void SkillAnim()
    {
        _animator.SetBool($"Skill{_skillNumber}", true);
        _transform.DORotate(SkillRotationAngle, AnimTime).SetEase(Ease.OutQuad);
        
    }

    public void ResetRotateAnim()
    {
        _animator.SetBool($"Skill{_skillNumber}", false);
        _transform.DORotate(Vector3.zero, AnimTime).SetEase(Ease.OutQuad);
    }
}