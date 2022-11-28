using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Utils.Enums;
using Unity.Netcode;

namespace _Scripts.Gameplay.GameplayObjects.Abilities.SpecificAbilities
{
    public class S_QuickSand: ServerTossedLogic
    {
        protected override void OnEnemyHit(ServerCharacter spawner, NetworkObject target, ServerCharacterEffects serverCharacterEffects)
        {
            base.OnEnemyHit(spawner, target, serverCharacterEffects);
            
            if (serverCharacterEffects != null)
            {
                var abilityEffect = GameDataSource.Instance.AbilityEffectsByName[AbilityEffectEnum.TIME_BOMB];
                if (serverCharacterEffects.AbilityEffects.ContainsKey(abilityEffect))
                {
                    serverCharacterEffects.ProcAbilityEffect(abilityEffect);
                }
            }
        }
    }
}