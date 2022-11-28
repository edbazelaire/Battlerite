using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Utils.Enums;
using Unity.Netcode;

namespace _Scripts.Gameplay.GameplayObjects.Abilities.SpecificAbilities
{
    public class S_SnowStorm: ServerTossedLogic
    {
        protected override void OnEnemyHit(ServerCharacter spawner, NetworkObject target, ServerCharacterEffects serverCharacterEffects)
        {
            base.OnEnemyHit(spawner, target, serverCharacterEffects);
            
            if (serverCharacterEffects != null)
            {
                var abilityEffect = GameDataSource.Instance.AbilityEffectsByName[AbilityEffectEnum.FROST];
                if (serverCharacterEffects.AbilityEffects.ContainsKey(abilityEffect))
                {
                    // proc the frost ability
                    serverCharacterEffects.ProcAbilityEffect(abilityEffect);
                    
                    // freeze the target
                    serverCharacterEffects.ApplyStateEffects(new StateEffect[] { StateEffect.Freeze(3, 25) });
                }
            }
        }
    }
}