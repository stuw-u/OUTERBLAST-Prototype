using Unity.Mathematics;

namespace Blast.ECS.DefaultComponents {

    public struct Position {
        public float3 value;
    }

    public struct Rotation {
        public quaternion value;
    }

    public struct Velocity {
        public float3 value;
    }

    public struct EnableState {
        public bool isEnabled;
        public int timer;
    }

    public struct LocalToWorld {
        public float4x4 rotationValue;
        public float4x4 value;

        public float3 Up {
            get {
                return math.transform(rotationValue, math.up());
            }
        }

        public float3 Forward {
            get {
                return math.transform(rotationValue, math.forward());
            }
        }

        public float3 Right {
            get {
                return math.transform(rotationValue, math.right());
            }
        }
    }
}
