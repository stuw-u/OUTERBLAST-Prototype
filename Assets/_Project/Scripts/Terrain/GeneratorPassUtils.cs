using System.Runtime.CompilerServices;
using Unity.Mathematics;

public static class GeneratorPassUtils {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 OctaveNoiseBillowStrange (float x, float y, float z, float frequency, int octaveCount, int seed) {
        float value = 0;
        float amp = 1f;
        float freq = frequency;

        for(int i = 0; i < octaveCount; i++) {
            // (x+1)/0.5 because noise.snoise returns a value from -1 to 1 so it needs to be scaled to go from 0 to 1.
            if(i == 0) {
                value += math.min(1f, 4f*math.abs(noise.snoise(new float3(x * freq, y * freq, z * freq)))) * amp;
            } else {
                value += ((noise.snoise(new float3(x * freq, y * freq, z * freq)) + 1) * 0.5f) * amp;
            }
            freq *= 2.4f;
            amp *= 0.5f;
        }

        return value;
    }

    public static float4 OctaveNoiseBillowRidged (float x, float y, float z, float frequency, int octaveCount, int seed, float billowRidgedMix, float freqMul = 2.0f, float ampMul = 0.5f) {
        float value = 0;
        float amp = 1f;
        float freq = frequency;

        for(int i = 0; i < octaveCount; i++) {
            // (x+1)/0.5 because noise.snoise returns a value from -1 to 1 so it needs to be scaled to go from 0 to 1.
            if(i == 0) {
                float noiseValueBillow = math.abs(noise.snoise(new float3(x * freq, y * freq, z * freq)));
                float noiseValueRidged = 1f-math.abs(noise.snoise(new float3(x * freq + 1f, y * freq + 1f, z * freq + 1f)));
                value += math.lerp(noiseValueBillow, noiseValueRidged, billowRidgedMix) * amp;
            } else {
                value += math.unlerp(-1f, 1f, noise.snoise(new float3(x * freq, y * freq, z * freq))) * amp;
            }
            freq *= freqMul;
            amp *= ampMul;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float OctaveNoise (float x, float y, float z, float frequency, int octaveCount, int seed, float freqMul = 2.0f, float ampMul = 0.5f) {
        float value = 0;
        float amp = 1f;
        float freq = frequency;

        for(int i = 0; i < octaveCount; i++) {
            // (x+1)/0.5 because noise.snoise returns a value from -1 to 1 so it needs to be scaled to go from 0 to 1.
            value += ((noise.snoise(new float4(x * freq, y * freq, z * freq, seed * 0.39f + 0.02f)) + 1) * 0.5f) * amp;
            freq *= freqMul;
            amp *= ampMul;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ChangeSteepness (float x, float s) {
        return s * (x + 0.5f) - s + 0.5f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ArchCarve (float posX, float posY, float height, float radius) {
        float2 lockedPos = new float2(posX, math.min(posY + height, 0f));
        return math.select(0f, math.saturate(1f - (math.length(lockedPos) * radius)), posY < 0.5f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float nfmod (float a) {
        return math.frac(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float coneDist (float3 p, float min, float max, float baseRadius, float outline = 4f, float falloff = 0.25f) {
        float cDist = math.sqrt(p.x * p.x + p.z * p.z);
        float2 xzVector = math.select(p.xz / cDist, float2.zero, cDist < 1f);
        float clamped01Y = math.unlerp(min, max, p.y);//math.saturate(math.unlerp(min, max, p.y));
        float radAtY = baseRadius * clamped01Y;
        float clampedCDist = math.min(cDist, radAtY);
        return math.saturate(outline-(math.distance(p, new float3(xzVector.x * clampedCDist, math.clamp(p.y, min, max), xzVector.y * clampedCDist)) * falloff));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 hash2_2 (float2 x) {
        float3 p3 = math.frac(new float3(x.xyx) * new float3(.1031f, .1030f, .0973f));
        p3 += math.dot(p3, p3.yzx + 33.33f);
        return math.frac((p3.xx + p3.yz) * p3.zy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 voronoiDistance (float2 x) {
        int2 p = new int2(math.floor(x));
        float2 f = math.frac(x);

        int2 mb = int2.zero;
        float2 mr = float2.zero;
        float cell = 0f;

        float res = 8.0f;
        for(int j = -1; j <= 1; j++)
            for(int i = -1; i <= 1; i++) {
                int2 b = new int2(i, j);
                float2 off = hash2_2(p + b);
                float2 r = new float2(b) + off - f;
                float d = math.dot(r, r);

                if(d < res) {
                    res = d;
                    mr = r;
                    mb = b;
                    cell = off.y;
                }
            }

        res = 8.0f;
        for(int j = -2; j <= 2; j++)
            for(int i = -2; i <= 2; i++) {
                int2 b = mb + new int2(i, j);
                float2 r = new float2(b) + hash2_2(p + b) - f;
                float d = math.dot(0.5f * (mr + r), math.normalize(r - mr));

                res = math.min(res, d);
            }

        return new float2(res, cell);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float builder_CylinderY (float3 p, float3 anchor, float height, float radiusBottom, float radiusTop) {
        float xzDist = 1f - math.saturate(math.distance(p.xz, anchor.xz) / math.lerp(radiusBottom, radiusTop, math.saturate(math.unlerp(anchor.y, anchor.y+height, p.y))));
        return xzDist * math.select(0f, 1f, p.y >= anchor.y && p.y <= anchor.y + height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float builder_Box (float3 p, float3 anchor, float3 size) {
        float valueX = math.select(0f, 1f, p.x >= anchor.x - size.x * 0.5f && p.x <= anchor.x + size.x * 0.5f);
        float valueY = math.select(0f, 1f, p.y >= anchor.y && p.y <= anchor.y + size.y);
        float valueZ = math.select(0f, 1f, p.z >= anchor.z - size.z * 0.5f && p.z <= anchor.z + size.z * 0.5f);
        return valueX * valueY * valueZ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float builder_TrianglePrismX (float3 p, float3 anchor, float3 size) {
        float valueZ = math.select(0f, 1f, p.z >= anchor.z - size.z * 0.5f && p.z <= anchor.z + size.z * 0.5f);
        float valueY = math.select(0f, 1f, p.y >= anchor.y && p.y <= anchor.y + size.y);

        float heightValue = math.saturate(math.unlerp(anchor.y, anchor.y + size.y, p.y));
        heightValue = math.select(heightValue, 1f, heightValue == 0f);
        float prismValue = (size.y * (1f - math.abs((p.x - anchor.x) / size.x * 2f))) - (p.y - anchor.y);
        return math.saturate(prismValue * valueY * valueZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float builder_BoxDentedX (float3 p, float3 anchor, float3 size, float dentSize, float dentDepth) {
        float valueX = math.select(0f, 1f, p.x >= anchor.x - size.x * 0.5f && p.x <= anchor.x + size.x * 0.5f);
        float valueY = math.select(0f, 1f, p.y >= anchor.y && p.y <= anchor.y + size.y - math.round(nfmod((p.x - anchor.x) * dentSize)) * dentDepth);
        float valueZ = math.select(0f, 1f, p.z >= anchor.z - size.z * 0.5f && p.z <= anchor.z + size.z * 0.5f);
        return valueX * valueY * valueZ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float builder_BoxDentedZ (float3 p, float3 anchor, float3 size, float dentSize, float dentDepth) {
        float valueX = math.select(0f, 1f, p.x >= anchor.x - size.x * 0.5f && p.x <= anchor.x + size.x * 0.5f);
        float valueY = math.select(0f, 1f, p.y >= anchor.y && p.y <= anchor.y + size.y - math.round(nfmod((p.z - anchor.z) * dentSize)) * dentDepth);
        float valueZ = math.select(0f, 1f, p.z >= anchor.z - size.z * 0.5f && p.z <= anchor.z + size.z * 0.5f);
        return valueX * valueY * valueZ;
    }
}
