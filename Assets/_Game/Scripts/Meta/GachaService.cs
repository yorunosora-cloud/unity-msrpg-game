using System;

/// <summary>
/// 가챠 추첨 서비스 (설계 §7-1).
/// RNG를 생성자 주입해 EditMode 테스트에서 시드를 고정할 수 있습니다.
/// </summary>
public class GachaService
{
    readonly CharacterDatabase _db;
    readonly Wallet            _wallet;
    readonly Roster            _roster;
    readonly GachaState        _state;
    readonly CrystalWallet     _crystals;
    readonly Random            _rng;

    public GachaService(CharacterDatabase db, Wallet wallet, Roster roster,
                        GachaState state, CrystalWallet crystals = null, Random rng = null)
    {
        _db       = db;
        _wallet   = wallet;
        _roster   = roster;
        _state    = state;
        _crystals = crystals;
        _rng      = rng ?? new Random();
    }

    // ── 단일 뽑기 ──────────────────────────────────────────────────────────

    /// <summary>논문 10 소모해 1회 추첨. 잔액 부족 시 null 반환.</summary>
    public GachaResult? RollOne()
    {
        if (!_wallet.TrySpend(CurrencyKind.Paper, GachaConfig.CostSingle))
            return null;

        _state.IncrementPity();
        Rarity rarity = DetermineRarity(forceAtLeastSr: false);
        return Finalize(rarity);
    }

    // ── 10연 뽑기 ──────────────────────────────────────────────────────────

    /// <summary>논문 100 소모해 10회 추첨. SR 보장 적용. 잔액 부족 시 null 반환.</summary>
    public GachaResult[] RollTen()
    {
        if (!_wallet.CanAfford(CurrencyKind.Paper, GachaConfig.CostTen))
            return null;

        _wallet.TrySpend(CurrencyKind.Paper, GachaConfig.CostTen);

        var  results   = new GachaResult[10];
        bool hadSrPlus = false;

        for (int i = 0; i < 10; i++)
        {
            _state.IncrementPity();
            bool isLast          = (i == 9);
            bool forceAtLeastSr  = isLast && !hadSrPlus && GachaConfig.TenPullSrGuarantee;

            Rarity rarity = DetermineRarity(forceAtLeastSr);
            results[i]    = Finalize(rarity);

            if (rarity >= Rarity.SR) hadSrPlus = true;
        }

        return results;
    }

    // ── 내부: 등급 결정 ───────────────────────────────────────────────────

    Rarity DetermineRarity(bool forceAtLeastSr)
    {
        // 하드 천장: PityCap 도달 시 SSR 확정 (Finalize에서 피티 리셋)
        if (_state.PityCounter >= GachaConfig.PityCap)
            return Rarity.SSR;

        if (forceAtLeastSr)
        {
            // SR 이상 강제 — 원래 비율 유지해 재비례 추첨
            float total = GachaConfig.WeightSR + GachaConfig.WeightSSR + GachaConfig.WeightUR;
            float roll  = (float)_rng.NextDouble() * total;
            if (roll < GachaConfig.WeightUR)  return Rarity.UR;
            roll -= GachaConfig.WeightUR;
            if (roll < GachaConfig.WeightSSR) return Rarity.SSR;
            return Rarity.SR;
        }

        // 일반 가중치 추첨 (UR → SSR → SR → R → N 순 체크)
        float r = (float)_rng.NextDouble() * 100f;
        if (r < GachaConfig.WeightUR)  return Rarity.UR;
        r -= GachaConfig.WeightUR;
        if (r < GachaConfig.WeightSSR) return Rarity.SSR;
        r -= GachaConfig.WeightSSR;
        if (r < GachaConfig.WeightSR)  return Rarity.SR;
        r -= GachaConfig.WeightSR;
        if (r < GachaConfig.WeightR)   return Rarity.R;
        return Rarity.N;
    }

    // ── 내부: 캐릭터 선택 + 로스터 등록 ─────────────────────────────────

    GachaResult Finalize(Rarity rarity)
    {
        // SSR 이상 획득 시 피티 리셋
        if (rarity >= Rarity.SSR) _state.ResetPity();

        CharacterDef[] pool  = _db.ByRarity(rarity);
        CharacterDef   def   = pool.Length > 0 ? pool[_rng.Next(pool.Length)] : null;
        bool           isNew = def != null && _roster.Add(def.id);

        // ── 중복 → 결정 지급 ─────────────────────────────────────────────
        CrystalKind? crystal       = null;
        int          crystalAmount = 0;

        if (def != null && !isNew)
        {
            crystal = CrystalCatalog.KindFor(def);
            if (crystal.HasValue)
            {
                crystalAmount = GachaConfig.CrystalForRarity(rarity);
                _crystals?.Add(crystal.Value, crystalAmount);
            }
        }

        return new GachaResult
        {
            def           = def,
            rarity        = rarity,
            isNew         = isNew,
            crystal       = crystal,
            crystalAmount = crystalAmount,
        };
    }
}
