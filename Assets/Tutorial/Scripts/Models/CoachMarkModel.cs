using UnityEngine;

namespace CommonUI.Tutorial.Models
{
    /// <summary>
    /// コーチマークの形や半径を設定するモデル
    /// </summary>
    [System.Serializable]
    public class CoachMarkModel
    {
        [SerializeField, Tooltip("コーチマークの形を指定するもの")]
        private ShapeKinds _shape;
        /// <summary>
        /// コーチマークの形を指定するもの
        /// </summary>
        public ShapeKinds Shape => _shape;

        [SerializeField, Tooltip("グラデーションにするかどうか")]
        private bool _isGradiate;
        /// <summary>
        /// グラデーションにするかどうか
        /// </summary>
        public bool IsGradiate => _isGradiate;

        [SerializeField, Tooltip("コーチマークの半径")]
        private float _radius;
        /// <summary>
        /// コーチマークの半径
        /// </summary>
        public float Radius => _radius;

        [SerializeField, Tooltip("コーチマークの大きさに加えてどれくらい余白を取るかの割合(グラデ以外使用時無効)"), Range(0f,1f)]
        private float _paddingRadius;
        /// <summary>
        /// 余白の割合
        /// </summary>
        public float PaddingRadius => _paddingRadius;

        [SerializeField, Tooltip("グラデーションの割合(グラデ以外使用時無効)"), Range(0f,1f)]
        private float _gradiateRadius;
        /// <summary>
        /// グラデーション用の半径
        /// </summary>
        public float GradiateRadius => _gradiateRadius;

        [SerializeField, Tooltip("コーチマークの対象になるオブジェクトの名前")]
        private string _targetObjectName;
        /// <summary>
        /// コーチマークの対象になるオブジェクトの名前
        /// </summary>
        public string TargetObjectName => _targetObjectName;
    }
}