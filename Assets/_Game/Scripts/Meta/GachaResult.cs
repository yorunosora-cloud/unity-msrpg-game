/// <summary>가챠 뽑기 1회 결과.</summary>
public struct GachaResult
{
    public CharacterDef  def;           // null이면 해당 등급에 캐릭터 없음
    public Rarity        rarity;
    public bool          isNew;         // false = 중복 (dupes 증가)
    public CrystalKind?  crystal;       // 중복 시 지급 결정 종류 (신규 또는 pool 없음이면 null)
    public int           crystalAmount; // 지급 결정 수량 (0이면 미지급)
}
