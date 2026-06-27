using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Boss))]
public class BossBrain : MonoBehaviour
{
    Boss      _boss;
    int       _phase = -1;
    bool      _busy;

    Transform _player;
    Coroutine _patternLoop;

    void Awake()
    {
        _boss = GetComponent<Boss>();
    }

    public void StartBattle()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        _busy = true;
        FindFirstObjectByType<BossHealthBar>()?.Show(_boss.Def.displayName, _boss.Def.phases.Length);
        Debug.Log($"[BossBrain] {_boss.Def.introLine}");
        yield return new WaitForSeconds(2f);
        _busy  = false;
        _phase = 0;
        _patternLoop = StartCoroutine(PatternLoop());
    }

    void Update()
    {
        if (_boss == null || _boss.IsDead || _busy || _phase < 0) return;
        FindFirstObjectByType<BossHealthBar>()?.SetHp(_boss.HpFraction);

        int nextPhase = _phase + 1;
        if (nextPhase >= _boss.Def.phases.Length) return;
        if (_boss.HpFraction < _boss.Def.phases[nextPhase].hpThreshold)
            StartCoroutine(PhaseTransition(nextPhase));
    }

    IEnumerator PhaseTransition(int nextPhase)
    {
        _busy = true;
        if (_patternLoop != null) StopCoroutine(_patternLoop);

        var phaseDef = _boss.Def.phases[nextPhase];
        Debug.Log($"[BossBrain] Phase {nextPhase + 1}: {phaseDef.bannerLine}");
        yield return new WaitForSeconds(1.5f);

        _phase = nextPhase;

        if (phaseDef.exposeWeakPointOnEnter)
        {
            var wp = GetComponentInChildren<BossWeakPoint>(includeInactive: true);
            wp?.SetExposed(phaseDef.weakPointExposeDuration);
        }

        _busy = false;
        _patternLoop = StartCoroutine(PatternLoop());
    }

    IEnumerator PatternLoop()
    {
        int cursor = 0;
        while (!_boss.IsDead)
        {
            yield return new WaitUntil(() => !_busy && !UIManager.IsAnyPanelOpen && !_boss.IsDead);
            if (_boss.IsDead) yield break;

            var phaseDef = _boss.Def.phases[_phase];
            if (phaseDef.patterns == null || phaseDef.patterns.Length == 0)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            var module = phaseDef.patterns[cursor % phaseDef.patterns.Length];
            cursor++;

            if (module != null)
            {
                var ctx = new BossContext(_boss, transform, _player);
                yield return StartCoroutine(module.Execute(ctx));
            }

            yield return new WaitForSeconds(phaseDef.patternInterval);
        }
    }

    public void Cleanup()
    {
        _busy = true;
        if (_patternLoop != null) StopCoroutine(_patternLoop);
        StopAllCoroutines();
        foreach (var t in Telegraph.All.ToArray())
            if (t != null) Destroy(t.gameObject);
    }
}
