using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] BossBrain     _brain;
    [SerializeField] GameObject[]  _sealDoors;

    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        _brain?.StartBattle();
        foreach (var door in _sealDoors)
            door?.SetActive(true);
    }
}
