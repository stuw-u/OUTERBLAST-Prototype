Shader "Hidden/Custom/AO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
			Tags{ "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
				
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
            TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif
            SAMPLER(sampler_CameraDepthTexture);

			TEXTURE2D(_NoiseTex);
			SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

			#define SAMPLE_DEPTH_AO(uv) LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r, _ZBufferParams);

			float4 _NoiseTex_TexelSize;

			//Common Settings
			half _AO_Intensity;
			half _AO_Noise_Intensity;
			half _AO_Radius;

			// SSAO Settings
			int _SSAO_Samples;
			float _SSAO_Area;

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				half4  positionCS   : SV_POSITION;
				half2  uv           : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
			};

			float3 NormalFromDepth(float depth, float2 texcoords) 
			{
				const float2 offset = float2(0.002,0);

				float depthX = SAMPLE_DEPTH_AO(texcoords + offset.xy);
				float depthY = SAMPLE_DEPTH_AO(texcoords + offset.yx);

				float3 pX = float3(offset.xy, depthX - depth);
				float3 pY = float3(offset.yx, depthY - depth);

				float3 normal = cross(pY, pX);
				normal.z = -normal.z;

				return normalize(normal);

			}

			float SSAO(float2 coords) {

				float area = _SSAO_Area;
				const float falloff = 0.05;
				float radius = _AO_Radius;

				const int samples = _SSAO_Samples;
				const float3 sample_sphere[64] = {
					float3(-0.5336, 0.5567, -0.6366),float3(-0.9180, 0.0143, 0.3963),
					float3(0.6868, 0.0527, 0.7250),float3(-0.2803, -0.6194, -0.7333),
					float3(-0.1525, -0.8989, 0.4108),float3(0.4106, 0.6043, -0.6828),
					float3(0.1554, 0.8929, 0.4227),float3(0.1702, 0.1926, 0.9664),
					float3(0.0364, -0.8263, 0.5621),float3(0.0747, -0.9955, -0.0584),
					float3(0.3520, -0.7933, 0.4968),float3(-0.7535, 0.6230, -0.2101),
					float3(0.2615, 0.2139, -0.9412),float3(-0.4500, -0.6741, -0.5858),
					float3(0.5478, -0.1549, 0.8221),float3(-0.2290, -0.8845, -0.4065),
					float3(0.2226, -0.9683, 0.1135),float3(-0.3315, 0.2970, 0.8955),
					float3(0.9495, 0.0246, -0.3127),float3(-0.2282, 0.6582, -0.7174),
					float3(-0.4992, 0.8588, -0.1151),float3(0.0646, 0.9227, -0.3801),
					float3(0.5599, -0.7460, 0.3606),float3(0.0744, 0.9822, -0.1724),
					float3(0.1814, -0.6955, 0.6953),float3(-0.6335, 0.7693, -0.0826),
					float3(0.4391, 0.3376, -0.8326),float3(0.4423, -0.8446, 0.3017),
					float3(-0.1982, -0.6957, -0.6904),float3(0.9251, 0.0127, 0.3795),
					float3(-0.8062, 0.4958, 0.3228),float3(0.6963, -0.4809, 0.5328),
					float3(0.7885, 0.5446, 0.2860),float3(0.6315, -0.5558, -0.5406),
					float3(0.6647, -0.0027, 0.7471),float3(-0.4170, 0.2648, 0.8695),
					float3(0.5386, -0.7524, 0.3792),float3(0.9699, 0.2417, 0.0283),
					float3(0.3630, 0.8072, -0.4655),float3(0.7671, -0.5554, 0.3210),
					float3(-0.2903, 0.0927, 0.9524),float3(0.7460, -0.4753, -0.4664),
					float3(0.4253, -0.8465, -0.3203),float3(-0.6023, -0.3654, 0.7097),
					float3(0.3374, 0.9371, 0.0891),float3(0.3313, -0.5128, 0.7920),
					float3(0.9208, 0.2094, 0.3290),float3(-0.0432, 0.8994, -0.4349),
					float3(0.6808, 0.7106, -0.1773),float3(-0.2856, 0.7787, 0.5586),
					float3(-0.4944, 0.0357, -0.8685),float3(-0.0258, 0.8591, -0.5112),
					float3(0.2578, -0.3172, 0.9127),float3(-0.4813, -0.7105, -0.5134),
					float3(0.6196, -0.7736, -0.1331),float3(0.1036, 0.7639, 0.6370),
					float3(-0.7483, -0.4364, 0.4996),float3(0.4386, -0.7450, 0.5026),
					float3(0.1603, -0.9097, 0.3830),float3(-0.8169, -0.2582, -0.5157),
					float3(-0.3949, 0.9097, -0.1286),float3(-0.1569, 0.5902, 0.7919),
					float3(-0.3859, 0.2161, 0.8969),float3(0.5752, 0.1028, 0.8115)
				};

				//Random Noise vector
				float3 random = normalize(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, coords * ( _ScreenParams.xy / _NoiseTex_TexelSize.zw)).rgb) * _AO_Noise_Intensity;

				//Depth texture
				float depth = SAMPLE_DEPTH_AO(coords);

				float3 position = float3(coords, depth);

				// Reconstruct normals
				float3 normal = NormalFromDepth(depth, coords);

				float radius_depth = radius;///depth;
				float occlusion = 0.0;

				for(int i = 0; i < samples; i++) {

					float3 ray = radius_depth * reflect(sample_sphere[i], random);
					float3 hemi_ray = position + sign(dot(ray,normal)) * ray;

					float occ_depth = SAMPLE_DEPTH_AO(saturate(hemi_ray.xy));
					float difference = depth - occ_depth;

					occlusion += step(falloff, difference) * (1.0-smoothstep(falloff, area, difference));

				}

				float ao = 1.0 - _AO_Intensity * occlusion * (1.0 / samples);

				return ao;
			}

			Varyings Vertex(Attributes input) {
			
				Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

				output.uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

				return output;
            }

			half4 Fragment(Varyings input) : SV_Target {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				float ao = SSAO(input.uv);
				return col *= ao;
			}

            ENDHLSL
        }
    }
}

