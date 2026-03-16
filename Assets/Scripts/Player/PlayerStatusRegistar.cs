using Unity.VisualScripting;
using UnityEngine;

public class PlayerStatusRegistar : MonoBehaviour
{
    [SerializeField]
    private BaseStatus _playerStatus;

    private BattleStatus _battleStatus;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _battleStatus = new BattleStatus(_playerStatus);
        BattleManager.RegisterPlayer(_battleStatus);
    }
}
