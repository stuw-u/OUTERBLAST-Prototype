using System;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;

/// <summary>
/// Different mode to control the net assist's behaviour
/// </summary>
[Flags]
public enum NetAssistMode : byte {
    Default = 0,
    Debug = 1,
    HeadlessServer = 2,
    Record = 4
}

// Quaternion code taken from https://gist.github.com/StagPoint/bb7edf61c2e97ce54e3e4561627f6582
/// <summary>
/// Various net optimization and data handeling function
/// </summary>
public static class NetUtils {
    #region Constants and static variables 

    /// <summary>
    /// Used when compressing float values, where the decimal portion of the floating point value
    /// is multiplied by this number prior to storing the result in an Int16. Doing this allows 
    /// us to retain five decimal places, which for many purposes is more than adequate.
    /// </summary>
    private const float FLOAT_PRECISION_MULT = 10000f;

    /// <summary>
    /// The list of possible chars the lobby code generation may use
    /// </summary>
    public const string lobbyCodeChars = "0123456789ABCDEF";

    

    #endregion

    #region Rotation Compression

    /// <summary>
    /// Writes a compressed Quaternion value to the network stream. This function uses the "smallest three"
    /// method, which is well summarized here: http://gafferongames.com/networked-physics/snapshot-compression/
    /// </summary>
    /// <param name="writer">The stream to write the compressed rotation to.</param>
    /// <param name="rotation">The rotation value to be written to the stream.</param>
    public static void WriteCompressedRotation (this NetworkWriter writer, Quaternion rotation) {
        var maxIndex = (byte)0;
        var maxValue = float.MinValue;
        var sign = 1f;

        // Determine the index of the largest (absolute value) element in the Quaternion.
        // We will transmit only the three smallest elements, and reconstruct the largest
        // element during decoding. 
        for(int i = 0; i < 4; i++) {
            var element = rotation[i];
            var abs = math.abs(rotation[i]);
            if(abs > maxValue) {
                // We don't need to explicitly transmit the sign bit of the omitted element because you 
                // can make the omitted element always positive by negating the entire quaternion if 
                // the omitted element is negative (in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                // represent the same rotation.), but we need to keep track of the sign for use below.
                sign = (element < 0) ? -1 : 1;

                // Keep track of the index of the largest element
                maxIndex = (byte)i;
                maxValue = abs;
            }
        }

        // If the maximum value is approximately 1f (such as Quaternion.identity [0,0,0,1]), then we can 
        // reduce storage even further due to the fact that all other fields must be 0f by definition, so 
        // we only need to send the index of the largest field.
        if(Mathf.Approximately(maxValue, 1f)) {
            // Again, don't need to transmit the sign since in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
            // represent the same rotation. We only need to send the index of the single element whose value
            // is 1f in order to recreate an equivalent rotation on the receiver.
            writer.WriteByte((byte)(maxIndex + 4));
            return;
        }

        var a = (short)0;
        var b = (short)0;
        var c = (short)0;

        // We multiply the value of each element by QUAT_PRECISION_MULT before converting to 16-bit integer 
        // in order to maintain precision. This is necessary since by definition each of the three smallest 
        // elements are less than 1.0, and the conversion to 16-bit integer would otherwise truncate everything 
        // to the right of the decimal place. This allows us to keep five decimal places.

        if(maxIndex == 0) {
            a = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
            b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
            c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
        } else if(maxIndex == 1) {
            a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
            b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
            c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
        } else if(maxIndex == 2) {
            a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
            b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
            c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
        } else {
            a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
            b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
            c = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
        }

        writer.WriteByte(maxIndex);
        writer.WriteInt16Packed(a);
        writer.WriteInt16Packed(b);
        writer.WriteInt16Packed(c);
    }

    /// <summary>
    /// Reads a compressed rotation value from the network stream. This value must have been previously written
    /// with WriteCompressedRotation() in order to be properly decompressed.
    /// </summary>
    /// <param name="reader">The network stream to read the compressed rotation value from.</param>
    /// <returns>Returns the uncompressed rotation value as a Quaternion.</returns>
    public static Quaternion ReadCompressedRotation (this NetworkReader reader) {
        // Read the index of the omitted field from the stream.
        var maxIndex = reader.ReadByte();

        // Values between 4 and 7 indicate that only the index of the single field whose value is 1f was
        // sent, and (maxIndex - 4) is the correct index for that field.
        if(maxIndex >= 4 && maxIndex <= 7) {
            var x = (maxIndex == 4) ? 1f : 0f;
            var y = (maxIndex == 5) ? 1f : 0f;
            var z = (maxIndex == 6) ? 1f : 0f;
            var w = (maxIndex == 7) ? 1f : 0f;

            return new Quaternion(x, y, z, w);
        }

        // Read the other three fields and derive the value of the omitted field
        var a = (float)reader.ReadInt16Packed() / FLOAT_PRECISION_MULT;
        var b = (float)reader.ReadInt16Packed() / FLOAT_PRECISION_MULT;
        var c = (float)reader.ReadInt16Packed() / FLOAT_PRECISION_MULT;
        var d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

        if(maxIndex == 0)
            return new Quaternion(d, a, b, c);
        else if(maxIndex == 1)
            return new Quaternion(a, d, b, c);
        else if(maxIndex == 2)
            return new Quaternion(a, b, d, c);

        return new Quaternion(a, b, c, d);
    }

    #endregion

    #region Compress Position and Velocity
    public const float minPosition = -512f;
    public const float maxPosition = 511f;
    public static readonly float3 minPositionFloat3 = new float3(-512f, -512f, -512f);
    public static readonly float3 maxPositionFloat3 = new float3(511f, 511f, 511f);
    public const float minVelocity = -64f;
    public const float maxVelocity = 63f;
    public static readonly float3 minVelocityFloat3 = new float3(-64f, -64f, -64f);
    public static readonly float3 maxVelocityFloat3 = new float3(63f, 63f, 63f);

    public static bool IsPlayerValid (float3 position, bool isGhost) {
        if(isGhost)
            return false;
        return math.all(position == math.clamp(position, minPositionFloat3, maxPositionFloat3));
    }

    public static ushort RangedFloatToUint16Pos (float value) {
        return (ushort)math.floor(math.saturate(math.unlerp(minPosition, maxPosition, value)) * ushort.MaxValue);
    }

    public static float Uint16ToRangedFloatPos (ushort value) {
        return math.lerp(minPosition, maxPosition, (float)value / ushort.MaxValue);
    }

    public static float CompressFloatToRangePos (float value) {
        return Uint16ToRangedFloatPos(RangedFloatToUint16Pos(value));
    }

    public static float3 CompressFloat3ToRangePos (float3 value) {
        return new float3(
            Uint16ToRangedFloatPos(RangedFloatToUint16Pos(value.x)),
            Uint16ToRangedFloatPos(RangedFloatToUint16Pos(value.y)),
            Uint16ToRangedFloatPos(RangedFloatToUint16Pos(value.z)));
    }

    public static ushort RangedFloatToUint16Vel (float value) {
        return (ushort)math.floor(math.saturate(math.unlerp(minVelocity, maxVelocity, value)) * ushort.MaxValue);
    }

    public static float Uint16ToRangedFloatVel (ushort value) {
        return math.lerp(minVelocity, maxVelocity, (float)value / ushort.MaxValue);
    }

    public static float CompressFloatToRangeVel (float value) {
        return Uint16ToRangedFloatVel(RangedFloatToUint16Vel(value));
    }

    public static float3 CompressFloat3ToRangeVel (float3 value) {
        return new float3(
            Uint16ToRangedFloatVel(RangedFloatToUint16Vel(value.x)),
            Uint16ToRangedFloatVel(RangedFloatToUint16Vel(value.y)),
            Uint16ToRangedFloatVel(RangedFloatToUint16Vel(value.z)));
    }
    #endregion
}