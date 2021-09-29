
// Hash without sin: 2 in 2 out
inline float2 hash2_2(float2 uv) {
    float3 p3 = frac(uv.xyx * float3(.1031, .1030, .0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.xx + p3.yz)*p3.zy);
}

// Hash without sin: 3 in 1 out
inline float3 hash1_3(float3 p3) {
	p3 = frac(p3 * .1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return frac((p3.x + p3.y) * p3.z);
}

// Smooth hash
inline float noise(float3 x) {
	float3 i = floor(x);
	float3 f = frac(x);
	f = f * f*(3.0 - 2.0*f);

	return lerp(lerp(lerp(hash1_3(i + float3(0, 0, 0)),
		hash1_3(i + float3(1, 0, 0)), f.x),
		lerp(hash1_3(i + float3(0, 1, 0)),
			hash1_3(i + float3(1, 1, 0)), f.x), f.y),
		lerp(lerp(hash1_3(i + float3(0, 0, 1)),
			hash1_3(i + float3(1, 0, 1)), f.x),
			lerp(hash1_3(i + float3(0, 1, 1)),
				hash1_3(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

// Voronoi Noise Function (Value Only)
float voronoi(float2 uv, float scale) {
	float2 g = floor(uv * scale);
	float2 f = frac(uv * scale);
	float t = 8.0;
	float3 res = float3(8.0, 0.0, 0.0);

	float value = 0.0;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			float2 lattice = float2(x, y);
			float2 offset = hash2_2(lattice + g);
			float d = distance(lattice + offset, f);

			if (d < res.x)
			{
				res = float3(d, offset.x, offset.y);
				value = d;
			}
		}
	}
	return value;
}

// Fresnel Effect
inline float fresnelEffect(float3 normal, float3 viewDir, float power) {
	return pow((1.0 - saturate(dot(normalize(normal), normalize(viewDir)))), power);
}

// Unlerp / InvLerp
inline float unlerp(float from, float to, float value) {
	return (value - from) / (to - from);
}

// Unlerp / InvLerp
inline half3 unlerp(half from, half to, half3 value)
{
    return (value - from) / (to - from);
}

float4 ObjectToClipPos(float3 pos)
{
    return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(pos, 1)));
}

float3 sampleGroundColor(float2 pos, float3 colorA, float3 colorB)
{
    float voronoiSum = 0.0;
    voronoiSum += voronoi(pos + float2(0.0, -0.0), 6) * 0.55;
    voronoiSum += voronoi(pos + float2(5000.0, -5000.0), 12) * 0.25;
    voronoiSum += voronoi(pos + float2(-5000.0, 5000.0), 24) * 0.20;
    return lerp(colorA, colorB, saturate(unlerp(0.35f, 0.65f, voronoiSum)));
}

inline float2 gradientRandomVector(float2 p)
{
    // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

inline float gradientValue(float2 UV, float Scale)
{
    float2 p = UV * Scale;
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(gradientRandomVector(ip), fp);
    float d01 = dot(gradientRandomVector(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(gradientRandomVector(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(gradientRandomVector(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}