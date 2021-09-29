using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using KinematicCharacterController;
using Blast.ECS;

public enum PlayerEntityBehaviour {
    Simulated,
    ReplicaSimulated
}

public enum PlayerControlType {
    Self,
    Remote,
    Server
}

public struct PlayerStateData {
    public byte clientId;
    public float jumpTimer;
    public float sliperyTimer;
    public byte coyoteTimer;
    public float damage;
    public bool inAirByJump;
    public byte anchoredClient;

    public float3 position;
    public float3 velocity;
    public quaternion rotation;
    public KinematicCharacterMotorState motorState;
}

public struct PlayerComponent {
    public ulong clientId;
    public float jumpTimer;
    public bool isGrounded;
    public float3 groundNormal;
    public float sliperyTimer;
    public byte coyoteTimer;
    public float damage;
    public bool inAirByJump;
    public byte anchoredClient;
    public KinematicCharacterMotorState motorState;

    public PlayerEntityBehaviour behaviour;
}

public struct Ghost { }



public struct PlayerEffect {
    public byte id;
    public byte level;
    public ushort timer;
}

public struct InputControlledComponent {
    public int inputFrame;
    public byte lastButtonRaw;
    public InputSnapshot inputSnapshot;
}


public struct PlayerEffects {
    private Dictionary<byte, PlayerEffect> effects;
    public static readonly HashSet<byte> syncToClient = new HashSet<byte>{
        0,  // Speed
        1,  // Freeze
      //2,     Heal
    };

    public void SetEffect (byte playerId, byte id, byte level, ushort timer) {
        if(effects == null)
            effects = new Dictionary<byte, PlayerEffect>();

        if(NetAssist.IsServer)
            LobbyManager.EnqueueEventApplyPlayerEffect(playerId, id, level);

        if(effects.TryGetValue(id, out PlayerEffect oldEffect)) {
            if(oldEffect.timer != 0) {
                effects[id] = new PlayerEffect() {
                    id = id,
                    level = level,
                    timer = (ushort)math.max((uint)oldEffect.level, level),
                };
                return;
            }
        }
        effects[id] = new PlayerEffect() {
            id = id,
            level = level,
            timer = timer,
        };
    }

    public byte IsEffectApplied (byte id) {
        if(effects == null)
            return 0;
        if(effects.TryGetValue(id, out PlayerEffect effect))
            if(effect.timer > 0)
                return effect.level;
        return 0;
    }

    public static byte IsEffectApplied (byte id, ref Entity playerEntity) {
        ref var effects = ref playerEntity.Get<PlayerEffects>();
        if(effects.effects == null)
            return 0;
        if(effects.effects.TryGetValue(id, out PlayerEffect effect))
            if(effect.timer > 0)
                return effect.level;
        return 0;
    }

    public void UpdateTimers (byte playerId) {
        if(effects == null)
            return;
        foreach(KeyValuePair<byte, PlayerEffect> kvp in effects.ToList()) {
            if(kvp.Value.timer == 0)
                continue;
            effects[kvp.Key] = new PlayerEffect() {
                id = kvp.Value.id,
                level = kvp.Value.level,
                timer = (ushort)(kvp.Value.timer - 1),
            };
            if(effects[kvp.Key].timer == 0) {
                effects.Remove(kvp.Key);
                if(NetAssist.IsServer)
                    LobbyManager.EnqueueEventRevokePlayerEffect(playerId, kvp.Value.id);
            }
        }
    }
}

public struct PlayerEffectEvent {
    public byte playerId;
    public bool applyOrRevoke;
    public byte id;
    public byte level;
}
