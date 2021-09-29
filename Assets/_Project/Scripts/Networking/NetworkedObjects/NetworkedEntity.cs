using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Blast.ECS;

namespace Blast.NetworkedEntities {
    public struct NetworkedEntityPriority {
        public bool shouldBeSync;
        public byte syncClock;
    }

    public struct NetworkedEntityComponent {
        public int spawnFrame { get; private set; }
        public byte owner { get; private set; }
        public byte copy { get; private set; }
        public ulong id { get; private set; }
        public int disabledTimer;

        public void SetValues (int spawnFrame, byte owner, byte copy) {
            this.spawnFrame = spawnFrame;
            this.owner = owner;
            this.copy = (byte)math.min(copy, 15);
            id = GenerateID();
        }

        public const byte idBitCount = 44;
        private ulong GenerateID () {
            uint ua = (uint)spawnFrame;
            ulong ub = (ulong)owner;
            ulong uc = (ulong)copy;
            return uc << 40 | ub << 32 | ua;
        }

        public static ulong GenerateID (int spawnFrame, byte owner, byte copy) {
            uint ua = (uint)spawnFrame;
            ulong ub = (ulong)owner;
            ulong uc = (ulong)copy;
            return uc << 40 | ub << 32 | ua;
        }
    }
}
