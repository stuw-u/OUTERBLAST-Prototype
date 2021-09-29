using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public static class PlayerUtils {

    const float floorLimit = 9f;
    public static bool IsFarFromGround (ref Entity player) {
        ref var pos = ref player.Get<Position>();
        ref var _localToWorld = ref player.Get<LocalToWorld>();

        return !Physics.Raycast(new Ray(pos.value, -_localToWorld.Up), floorLimit, 1 << 9);
    }

    const float lowFloorLimit = 4f;
    public static bool IsSlightlyAboveGround (ref Entity player) {
        ref var pos = ref player.Get<Position>();
        ref var _localToWorld = ref player.Get<LocalToWorld>();

        return !Physics.Raycast(new Ray(pos.value, -_localToWorld.Up), lowFloorLimit, 1 << 9);
    }

    /// <summary>
    /// Returns a ray that starts at the player's eyes and points where it's looking
    /// </summary>
    public static void GetPlayerRay (ref Entity player, out float3 rayDirection, out float3 rayOrigin, float2 offset = new float2()) {
        ref var pos = ref player.Get<Position>();
        ref var input = ref player.Get<InputControlledComponent>();
        ref var _localToWorld = ref player.Get<LocalToWorld>();
        
        rayDirection = math.mul(
            math.mul(new quaternion(_localToWorld.rotationValue), quaternion.Euler(
                math.radians(input.inputSnapshot.lookAxis.y + offset.y),
                math.radians(input.inputSnapshot.lookAxis.x + offset.x),
                0f
            )), math.forward());
        rayOrigin = pos.value + _localToWorld.Up * 0.5f;
    }

    /// <summary>
    /// Returns a ray that starts at the player's eyes and points where it's looking
    /// </summary>
    public static Ray GetPlayerRay (ref Entity player) {
        GetPlayerRay(ref player, out float3 rayDirection, out float3 rayOrigin);
        return new Ray(rayOrigin, rayDirection);
    }

    /// <summary>
    /// Sets the ghost status of a given player
    /// </summary>
    public static void SetGhostStatus (ulong playerId, bool shouldBeGhost) {
        if(shouldBeGhost) {
            if(!SimulationManager.inst.playerSystem.playerEntities[playerId].Has<Ghost>()) {
                SimulationManager.inst.playerSystem.playerEntities[playerId].Get<Ghost>();
            }
        } else {
            if(SimulationManager.inst.playerSystem.playerEntities[playerId].Has<Ghost>()) {
                SimulationManager.inst.playerSystem.playerEntities[playerId].Del<Ghost>();
            }
        }
        TabMenu.RefreshTabMenuData();
    }


    /// <summary>
    /// Checks if a player has a shield activated and if it would block anything
    /// </summary>
    public static bool DoDamageShield (ref Entity player) {
        if(player.Has<PortableShieldUser>()) {
            ref var shieldUser = ref player.Get<PortableShieldUser>();
            return shieldUser.isEnabled;
        } else {
            return false;
        }
    }


    /// <summary>
    /// Damages the shield on a player by a certain value (Only do this when DoDamageShield returned true)
    /// </summary>
    public static void DamageShield (ref Entity player, float damage) {
        ref var shieldUser = ref player.Get<PortableShieldUser>();
        shieldUser.storedDamage = (short)math.round(shieldUser.storedDamage + damage * 4f);
    }


    /// <summary>
    /// Applies damages on a player
    /// </summary>
    public const float maxDamage = 200f;
    public static void ApplyDamage (ref PlayerComponent player, float damage) {
        player.damage = math.clamp(math.round(player.damage + damage), 0f, maxDamage);
    }


    /// <summary>
    /// Set the taggle component value (either)
    /// </summary>
    public static void SetTaggableValue (ulong playerId, int value) {
        if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(playerId, out Entity entity)) {
            entity.Get<TaggableBounty>().value = value;
        }
    }

    /// <summary>
    /// Calculate how much knockback a player should get from its 
    /// </summary>
    public static float DamageToKnockbackMultiplayer (ref PlayerComponent player, bool disableBoosts = false) {
        return 1f + math.unlerp(0f, 100f, player.damage) * 0.4f * math.select(0f, 1f, LobbyWorldInterface.inst.matchRulesInfo.damageMode && !disableBoosts);
    }
}
