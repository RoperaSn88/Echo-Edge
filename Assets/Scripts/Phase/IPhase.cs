using Cysharp.Threading.Tasks;
public interface IPhase 
{
    /// <summary>
    /// 待機
    /// </summary>
    public UniTask<IPhase> WaitPhase();
}
