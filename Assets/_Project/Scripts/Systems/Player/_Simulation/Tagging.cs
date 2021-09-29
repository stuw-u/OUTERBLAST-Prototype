using Blast.ECS;
using Unity.Mathematics;

public struct TaggableBounty {
    public int value;
    public ulong claimant;
    public int claimingGroundedTimer;
    public int ticksSinceLastClaimed;
}

public class TaggingSystem : IECSRunSystem {

    public const int defaultBountyValue = 5;
    public const int maxBountyValue = 20;
    public const int ticksToIncreaseBounty = 500;
    public const int defaultMaxGroundedTick = 120;
    private readonly EcsFilter<PlayerComponent, TaggableBounty> taggableEntities = null;

    public static void TagEntity (Entity entity, ulong claimant) {
        if(!NetAssist.IsServer) {
            return;
        }

        ref var player = ref entity.Get<PlayerComponent>();
        ref var taggable = ref entity.Get<TaggableBounty>();

        if(player.clientId == claimant) {
            return;
        }

        taggable.claimant = claimant;
        taggable.claimingGroundedTimer = defaultMaxGroundedTick;
        ScoreManager.UpdateClaimant(player.clientId, (int)taggable.claimant);
    }

    public static void ClaimEntity (Entity entity) {
        if(!NetAssist.IsServer) {
            return;
        }

        ref var player = ref entity.Get<PlayerComponent>();
        ref var taggable = ref entity.Get<TaggableBounty>();

        if(taggable.claimingGroundedTimer > 0) {
            if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {

                ScoreManager.TransferScoreFromBounty(taggable.claimant, player.clientId, taggable.value);
                taggable.claimingGroundedTimer = 0;
                taggable.value = defaultBountyValue;
                ScoreManager.UpdateIndicatorValue(player.clientId, taggable.value);
                ScoreManager.UpdateClaimant(player.clientId, -1);

            } else if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {

                ScoreManager.TransferScoreFromStock(taggable.claimant, player.clientId);
                taggable.value = math.max(0, taggable.value - 1); // Lower stock. If this reach 0, go in spectator
                taggable.claimingGroundedTimer = 0;
                ScoreManager.UpdateIndicatorValue(player.clientId, taggable.value);
                ScoreManager.UpdateClaimant(player.clientId, -1);
                PlayerUtils.SetGhostStatus(player.clientId, taggable.value <= 0);

                ScoreManager.CheckForStockEnd();
            }
        } else {
            if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
                
                taggable.value = math.max(0, taggable.value - 1); // Lower stock. If this reach 0, go in spectator
                ScoreManager.UpdateIndicatorValue(player.clientId, taggable.value);
                PlayerUtils.SetGhostStatus(player.clientId, taggable.value <= 0);

                ScoreManager.CheckForStockEnd();
            }
        }
    }
    
    public void Run () {

        if(!NetAssist.IsServer)
            return;

        float deltaTime = UnityEngine.Time.fixedDeltaTime;
        foreach(var entityIndex in taggableEntities) {
            
            ref var entity = ref taggableEntities.GetEntity(entityIndex);
            ref var taggable = ref entity.Get<TaggableBounty>();
            ref var player = ref entity.Get<PlayerComponent>();

            // Staying on the ground for more than defaultMaxGroundedTick will remove the bounty
            if(taggable.claimingGroundedTimer > 0) {
                if(player.isGrounded) {
                    taggable.claimingGroundedTimer--;

                    if(taggable.claimingGroundedTimer == 0) {
                        ScoreManager.UpdateClaimant(player.clientId, -1);
                    }
                }
                taggable.ticksSinceLastClaimed = 0;
            }

            // After idiling for few seconds without being claimed, the bounty will raise
            if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
                if(taggable.ticksSinceLastClaimed >= ticksToIncreaseBounty) {
                    taggable.value = math.min(taggable.value + 1, maxBountyValue);
                    taggable.ticksSinceLastClaimed = 0;
                    ScoreManager.UpdateIndicatorValue(player.clientId, taggable.value);
                    TabMenu.RefreshTabMenuData();
                }
                if(LobbyWorldInterface.inst.LocalLobbyState == LocalLobbyState.InGame) {
                    taggable.ticksSinceLastClaimed++;
                }
            }
        }
    }
}
