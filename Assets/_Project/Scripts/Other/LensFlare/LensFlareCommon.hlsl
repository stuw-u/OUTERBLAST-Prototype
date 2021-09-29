struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;

	// LensFlare Data : 
	//		* X = RayPos 
	//		* Y = Rotation (< 0 = Auto)
	//		* ZW = Size (Width, Height) in Screen Height Ratio
    nointerpolation float4 lensflare_data : TEXCOORD1;
	// World Position (XYZ) and Radius(W) : 
    nointerpolation float4 worldPosRadius : TEXCOORD2;
	// LensFlare FadeData : 
	//		* X = Near Start Distance
	//		* Y = Near End Distance
	//		* Z = Far Start Distance
	//		* W = Far End Distance
    nointerpolation float4 lensflare_fadeData : TEXCOORD3;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
float _OccludedSizeScale;

// thanks, internets
static const uint DEPTH_SAMPLE_COUNT = 32;
static float2 samples[DEPTH_SAMPLE_COUNT] =
{
    float2(0.658752441406, -0.0977704077959),
	float2(0.505380451679, -0.862896621227),
	float2(-0.678673446178, 0.120453640819),
	float2(-0.429447203875, -0.501827657223),
	float2(-0.239791020751, 0.577527523041),
	float2(-0.666824519634, -0.745214760303),
	float2(0.147858589888, -0.304675519466),
	float2(0.0334240831435, 0.263438135386),
	float2(-0.164710089564, -0.17076793313),
	float2(0.289210408926, 0.0226817727089),
	float2(0.109557107091, -0.993980526924),
	float2(-0.999996423721, -0.00266989553347),
	float2(0.804284930229, 0.594243884087),
	float2(0.240315377712, -0.653567194939),
	float2(-0.313934922218, 0.94944447279),
	float2(0.386928111315, 0.480902403593),
	float2(0.979771316051, -0.200120285153),
	float2(0.505873680115, -0.407543361187),
	float2(0.617167234421, 0.247610524297),
	float2(-0.672138273716, 0.740425646305),
	float2(-0.305256098509, -0.952270269394),
	float2(0.493631094694, 0.869671344757),
	float2(0.0982239097357, 0.995164275169),
	float2(0.976404249668, 0.21595069766),
	float2(-0.308868765831, 0.150203511119),
	float2(-0.586166858673, -0.19671548903),
	float2(-0.912466347218, -0.409151613712),
	float2(0.0959918648005, 0.666364192963),
	float2(0.813257217407, -0.581904232502),
	float2(-0.914829492569, 0.403840065002),
	float2(-0.542099535465, 0.432246923447),
	float2(-0.106764614582, -0.618209302425)
};

float GetOcclusion(float2 screenPos, float depth, float radius, float ratio)
{
    float contrib = 0.0f;
    float sample_Contrib = 1.0 / DEPTH_SAMPLE_COUNT;
    float2 ratioScale = float2(1 / ratio, 1.0);
    for(uint i = 0; i < DEPTH_SAMPLE_COUNT; i++)
    {
        float2 pos = screenPos + (samples[i] * radius * ratioScale);
        pos = pos * 0.5 + 0.5;
        pos.y = 1 - pos.y;
        if(pos.x >= 0 && pos.x <= 1 && pos.y >= 0 && pos.y <= 1)
        {
            float sampledDepth = LinearEyeDepth(SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, pos, 0).r, _ZBufferParams);
			//float sampledDepth = LinearEyeDepth(SampleCameraDepth(pos), _ZBufferParams);
			
            if(sampledDepth >= depth)
                contrib += sample_Contrib;
        }
    }
    return contrib;
}

v2f vert(appdata v)
{
    v2f o;

    
    float4 clip = TransformWorldToHClip(GetCameraRelativePositionWS(v.worldPosRadius.xyz));
    float depth = clip.w; // Here w is actually equal to the coordinate z value in camera space, which is the distance from the camera in camera space. Because the matrix from world space to camera space is not scaled, the distance here is equivalent to the distance from the camera in world space.

    float3 cameraUp = normalize(mul(UNITY_MATRIX_V, float4(0, 1, 0, 0))).xyz;

    float4 extent = TransformWorldToHClip(GetCameraRelativePositionWS(v.worldPosRadius.xyz + cameraUp * v.worldPosRadius.w));
    
    // Here is to get the coordinates of the sun center or lensflare center
    float2 screenPos = clip.xy / clip.w; // Divide homogeneously to get the coordinate interval [-1,1]
	// Here are the coordinates of the center point of the upper edge of lensflare
    float2 extentPos = extent.xy / extent.w; // Divide homogeneously to get the coordinate interval [-1,1]

    float radius = distance(screenPos, extentPos);

    // screen aspect ratio w/h
    float ratio = _ScreenParams.x / _ScreenParams.y; // screenWidth/screenHeight
	// Calculate the mask ratio 
    float occlusion = GetOcclusion(screenPos, depth - v.worldPosRadius.w, radius, ratio);

	// Fade in and fade out lens flare according to the set distance
    float4 d = v.lensflare_fadeData;
    float distanceFade = saturate((depth - d.x) / (d.y - d.x));
    distanceFade *= 1.0f - saturate((depth - d.z) / (d.w - d.z));

	// position and rotate
    float angle = v.lensflare_data.y;
    if(angle < 0) // Automatic
    {
        float2 dir = normalize(screenPos);
        angle = atan2(dir.y, dir.x) + 1.57079632675; // arbitrary, we need V to face the source, not U;
    }

    // Calculate the final patch size according to the mask ratio and the set patch size
    float2 quad_size = lerp(_OccludedSizeScale, 1.0f, occlusion) * v.lensflare_data.zw;
    if(distanceFade * occlusion == 0.0f) // if either one or other is zeroed
        quad_size = float2(0, 0); // clip

    float2 local = v.vertex.xy * quad_size;

// Rotate the patch
// This is actually the float2x2 rotation matrix R
// { 
//   cos(θ),-sin(θ)
//   sin(θ),cos(θ) 
// }
// Multiply the screen space coordinates of the vertex
    local = float2(
		local.x * cos(angle) + local.y * (-sin(angle)),
		local.x * sin(angle) + local.y * cos(angle));

	// adjust to correct ratio
    local.x /= ratio; // Apply the zoom corresponding to the screen ratio, otherwise the result will be stretched

    float2 rayOffset = -screenPos * v.lensflare_data.x;
    o.vertex.w = v.vertex.w;
    o.vertex.xy = screenPos + local + rayOffset;

    o.vertex.z = 1;
    o.uv = v.uv;

    o.color = v.color * occlusion * distanceFade * saturate(length(screenPos * 2));
    return o;
}