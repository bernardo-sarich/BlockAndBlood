/// <summary>
/// Optional contract for damageable entities that expose current HP.
/// Used by HeroBehaviour when PrioritizeHighestHp is true (Instinto cazador card).
/// EnemyBehaviour implements this alongside IDamageable.
/// </summary>
public interface IHasHp
{
    float CurrentHp { get; }
}
