using UnityEngine;

public class BossMission : MonoBehaviour
{
    bool _done;

    void OnEnable()
    {
        if (Boss.Active != null)
            Boss.Active.OnDefeated += HandleDefeated;
    }

    void OnDisable()
    {
        if (Boss.Active != null)
            Boss.Active.OnDefeated -= HandleDefeated;
    }

    void HandleDefeated()
    {
        if (_done) return;
        _done = true;
        Debug.Log("[BossMission] 보스 처치 완료 → 던전 클리어");
    }
}
