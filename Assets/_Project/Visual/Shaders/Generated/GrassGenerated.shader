Shader "GrassGenerated"
{
    Properties
    {
        Color_A("ColorA", Color) = (0, 0, 0, 0)
        Color_B("ColorB", Color) = (0, 0, 0, 0)
        Color_Burnt("BurntColor", Color) = (0, 0, 0, 0)
        ColorNoiseScale("Scale", Float) = 0
        Smoothness("Smoothness", Float) = 0
        MinCamDist("MinDistance", Float) = 0
        MaxCamDist("MaxDistance", Float) = 2
        WindNoiseScale("WindScale", Float) = 4
        WindSpeed2D("WindSpeed", Vector) = (1, 0.2, 0, 0)
        WindAmplitude("WindAmplitude", Float) = 0.1
        WindHeightInfl("HeightInfluence", Float) = 1
		BlastAmpl("BlastAmplitude", Float) = 1
		BlastLenght("BlastLenght", Float) = 1
		BlastTimeRatio("BlastTimeRatio", Float) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry+0"
        }

        
        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }
           
            // Render State
            Blend Off
            Cull Off
            ZTest LEqual
            ZWrite On
            
            HLSLPROGRAM

		    // -------------------------------------
			// Required to compile gles 2.0 with standard SRP library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
			
			// -------------------------------------
			// Material Keywords
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICSPECGLOSSMAP
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _OCCLUSIONMAP

			// -------------------------------------
			// Universal Render Pipeline keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			// Shader Properties
            CBUFFER_START(MyCBuffer)
            float4 Color_A;
            float4 Color_B;
            float4 Color_Burnt;
            float ColorNoiseScale;
            float Smoothness;
            float MinCamDist;
            float MaxCamDist;
            float WindNoiseScale;
            float2 WindSpeed2D;
            float WindAmplitude;
            float WindHeightInfl;
            float BlastAmpl;
            float BlastTimeRatio;
            float BlastLenght;
			CBUFFER_END
			int blastProbeCount;

			struct GrassData {
				float4 rootPosScale;
				float4 rootRot;
				float burntValue;
			};

			struct BlastProbeData {
				float3 origin;
				float radius;
				float explosionTime;
			};

			// Instancing Properties
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				StructuredBuffer<float4x4> matriciesBuffer;
				StructuredBuffer<float4x4> invMatriciesBuffer;
				StructuredBuffer<GrassData> grassDataBuffer;
				StructuredBuffer<BlastProbeData> blastProbeData;
			#endif



			struct Attributes {
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float2 uv           : TEXCOORD0;
				float2 uvLM         : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float2 uv                       : TEXCOORD0;
				float2 uvLM                     : TEXCOORD1;
				float4 positionWSAndFogFactor   : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
				float value		                : TEXCOORD4;
				half3  normalWS                 : TEXCOORD3;

				#ifdef _MAIN_LIGHT_SHADOWS
				float4 shadowCoord              : TEXCOORD6; // compute shadow coord per-vertex for the main light
				#endif
				float4 positionCS               : SV_POSITION;
				float4 vertexColor				: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};



			inline float invLerp(float A, float B, float T) {
                return (T - A)/(B - A);
            }

            inline float2 gradientRandomVector (float2 p)
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
            
			// Hash without sin: 2 in 2 out
			inline float2 hash2_2(float2 uv) {
				float3 p3 = frac(float3(uv.xyx) * float3(.1031, .1030, .0973));
				p3 += dot(p3, p3.yzx + 33.33);
				return frac((p3.xx + p3.yz)*p3.zy);
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

			float4 ObjectToClipPos(float3 pos)
			{
				return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4 (pos, 1)));
			}



			void setup()
			{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				unity_ObjectToWorld = matriciesBuffer[unity_InstanceID];
				unity_WorldToObject = invMatriciesBuffer[unity_InstanceID];
			#endif
			}

			
			Varyings vert(Attributes IN, uint instanceID: SV_InstanceID) {
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, output);

				float3 TimeParameters = _TimeParameters.xyz;
				float3 WorldSpacePosition = TransformObjectToWorld(IN.positionOS.xyz);

				// Determines the offsets to the local verticies needed to simulate wind
				float windAmpMultiplier = IN.positionOS.y * WindHeightInfl; // Determines how strong the wind should effect this vertex
				float2 windUV = (WindSpeed2D * TimeParameters.x.xx) + (float2(WorldSpacePosition.x, WorldSpacePosition.z) * WindNoiseScale); // Noise scale should NOT affect speed
				float2 windNoise = float2(gradientValue(windUV, 1), gradientValue(windUV + float2(900, -200), 1)); // Generates a coherent wind direction vector
				windNoise = windNoise * WindAmplitude * windAmpMultiplier;

				// Blast Probles
				float3 blastModif = float3(0, 0, 0);
				float blastValue = 0;
				float4 objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));

				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				for (int b = 0; b < blastProbeCount; b++) {
					float4 probeData = float4(blastProbeData[b].origin, blastProbeData[b].radius);
					float probeTime = saturate((TimeParameters.x - blastProbeData[b].explosionTime) / BlastLenght);

					float probeRaiseTime = invLerp(0, BlastTimeRatio, probeTime);
					float probeLowerTime = invLerp(1, BlastTimeRatio, probeTime);
					float probeTimeAmpl = smoothstep(0, 1, lerp(probeRaiseTime, probeLowerTime, step(BlastTimeRatio, probeTime)));

					float blastDist = distance(objectOrigin.xyz, probeData.xyz);
					float3 blastDir = (objectOrigin - probeData.xyz) / blastDist;
					float blast01 = clamp((probeData.w - blastDist) / probeData.w, 0, 1);

					blastModif += blastDir * blast01 * probeTimeAmpl;
					blastValue += blast01 * probeTimeAmpl;
				}
				//blastModif /= max(1, blastProbeCount);			
				//blastValue /= max(1, blastProbeCount);
				blastModif *= 0.5;
				blastValue *= 0.5;
				#endif
				blastModif = blastModif * BlastAmpl * windAmpMultiplier;

				// Offsets the local vertex position with the wind offsets to create new positions
				float3 worldVertexPosition = WorldSpacePosition + float3(windNoise.x, 0, windNoise.y) + float3(blastModif.x, 0, blastModif.z);
				float3 localVertexPosition = TransformWorldToObject(worldVertexPosition);
				localVertexPosition.y = lerp(localVertexPosition.y, clamp(0.4, 1.5, localVertexPosition.y - blastValue * 0.4), blastValue);

				// Scales the whole mesh based on camera distance
				//float distToCamera = distance(_WorldSpaceCameraPos, WorldSpacePosition);
				//float cameraDistScaler = saturate(invLerp(MinCamDist, MaxCamDist, distToCamera)); // Determines the size the grass should have at this distance from the camera
				//localVertexPosition = localVertexPosition * cameraDistScaler;

				// Top Color
				float2 noiseUV = (ColorNoiseScale.xx) * WorldSpacePosition.xz;
				float voronoiSum = 0.0;
				voronoiSum += voronoi(noiseUV + float2(0.0, -0.0), 6) * 0.55;
				voronoiSum += voronoi(noiseUV + float2(5000.0, -5000.0), 12) * 0.25;
				voronoiSum += voronoi(noiseUV + float2(-5000.0, 5000.0), 24) * 0.20;
				float4 grassColor = lerp(Color_A, Color_B, voronoiSum);

				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				float burntValue = grassDataBuffer[instanceID].burntValue;
				#else
				float burntValue = 0;
				#endif
				float burntFader = lerp(0, Color_Burnt.a, burntValue);

				output.vertexColor = lerp(grassColor, Color_Burnt, burntFader);

				output.value = saturate(IN.positionOS.y * 2);
				VertexPositionInputs vertexInput = GetVertexPositionInputs(localVertexPosition.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

				#ifdef _MAIN_LIGHT_SHADOWS
				output.shadowCoord = GetShadowCoord(vertexInput);
				#endif

				output.positionCS = ObjectToClipPos(localVertexPosition);
				output.normalWS = vertexNormalInput.normalWS;

				output.uv = TRANSFORM_TEX(float2(0, 0), _BaseMap);

				float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

				output.uvLM = IN.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);

				return output;
			}

			float4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);

				SurfaceData surfaceData;
				InitializeStandardLitSurfaceData(IN.uv, surfaceData);
				half3 normalWS = IN.normalWS;
				normalWS = normalize(normalWS);
				float3 positionWS = IN.positionWSAndFogFactor.xyz;
				half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);


				float3 grassColor = IN.vertexColor.xyz;




                // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
                // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
                // below with your own.

				half alpha = 1.0;
                BRDFData brdfData;
                InitializeBRDFData(grassColor.xyz, 0, 0, lerp(0, Smoothness, IN.value), alpha, brdfData);

				#ifdef _MAIN_LIGHT_SHADOWS
                Light mainLight = GetMainLight(IN.shadowCoord);
				#else
                Light mainLight = GetMainLight();
				#endif

                // LightingPhysicallyBased computes direct light contribution.
				half3 color = LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

				
				color = MixFog(color, IN.positionWSAndFogFactor.w);
				return float4(color, 1);
			}

            ENDHLSL
        }

		Pass
		{
			Name "DepthOnly"
			Tags
		{
			"LightMode" = "DepthOnly"
		}

			// Render State
			Blend Off
			Cull Off
			ZTest LEqual
			ZWrite On
			ColorMask 0


			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Pragmas
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// Defines
			#define _NORMAL_DROPOFF_TS 1
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#pragma multi_compile_instancing
			#define SHADERPASS_DEPTHONLY

			// Includes
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


			//--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			// Shader Properties
            CBUFFER_START(MyCBuffer)
            float4 Color_A;
            float4 Color_B;
            float4 Color_Burnt;
            float ColorNoiseScale;
            float Smoothness;
            float MinCamDist;
            float MaxCamDist;
            float WindNoiseScale;
            float2 WindSpeed2D;
            float WindAmplitude;
            float WindHeightInfl;
            float BlastAmpl;
            float BlastTimeRatio;
            float BlastLenght;
			CBUFFER_END
			int blastProbeCount;

			struct GrassData {
				float4 rootPosScale;
				float4 rootRot;
				float burntValue;
			};

			struct BlastProbeData {
				float3 origin;
				float radius;
				float explosionTime;
			};

			// Instancing Properties
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				StructuredBuffer<float4x4> matriciesBuffer;
				StructuredBuffer<float4x4> invMatriciesBuffer;
				StructuredBuffer<GrassData> grassDataBuffer;
				StructuredBuffer<BlastProbeData> blastProbeData;
			#endif


			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				half3  normalWS                 : TEXCOORD3;
				float4 positionCS               : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


            // Utils Functions
			inline float invLerp(float A, float B, float T) {
                return (T - A)/(B - A);
            }
            
            inline float2 gradientRandomVector (float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            inline float2 voronoiRandomVector (float2 UV, float offset)
            {
                float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
                UV = frac(sin(mul(UV, m)) * 46839.32);
                return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
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
            
			inline float voronoiValue(float2 UV, float AngleOffset, float CellDensity)
            {
                float2 g = floor(UV * CellDensity);
                float2 f = frac(UV * CellDensity);
                float t = 8.0;
                float3 res = float3(8.0, 0.0, 0.0);
				
				float smallest = 0;
                for(int y=-1; y<=1; y++)
                {
                    for(int x=-1; x<=1; x++)
                    {
                        float2 lattice = float2(x,y);
                        float2 offset = voronoiRandomVector(lattice + g, AngleOffset);
                        float d = distance(lattice + offset, f);
            
                        if(d < res.x)
                        {
                            res = float3(d, offset.x, offset.y);
							smallest = res.x;
                        }
                    }
                }
				return smallest;
            }

			float4 ObjectToClipPos(float3 pos)
			{
				return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4 (pos, 1)));
			}


			void setup()
			{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				unity_ObjectToWorld = matriciesBuffer[unity_InstanceID];
				unity_WorldToObject = invMatriciesBuffer[unity_InstanceID];
			#endif
			}

			
			Varyings vert(Attributes IN, uint instanceID: SV_InstanceID)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(IN);

				float3 TimeParameters = _TimeParameters.xyz;
				float3 WorldSpacePosition = TransformObjectToWorld(IN.positionOS.xyz);

				// Determines the offsets to the local verticies needed to simulate wind
				float windAmpMultiplier = IN.positionOS.y * WindHeightInfl; // Determines how strong the wind should effect this vertex
				float2 windUV = (WindSpeed2D * TimeParameters.x.xx) + (float2(WorldSpacePosition.x, WorldSpacePosition.z) * WindNoiseScale); // Noise scale should NOT affect speed
				float2 windNoise = float2(gradientValue(windUV, 1), gradientValue(windUV + float2(900, -200), 1)); // Generates a coherent wind direction vector
				windNoise = windNoise * WindAmplitude * windAmpMultiplier;

				// Blast Probles
				float3 blastModif = float3(0, 0, 0);
				float blastValue = 0;
				float4 objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				for (int b = 0; b < blastProbeCount; b++) {
					float4 probeData = float4(blastProbeData[b].origin, blastProbeData[b].radius);
					float probeTime = saturate((TimeParameters.x - blastProbeData[b].explosionTime) / BlastLenght);

					float probeRaiseTime = invLerp(0, BlastTimeRatio, probeTime);
					float probeLowerTime = invLerp(1, BlastTimeRatio, probeTime);
					float probeTimeAmpl = smoothstep(0, 1, lerp(probeRaiseTime, probeLowerTime, step(BlastTimeRatio, probeTime)));

					float blastDist = distance(objectOrigin.xyz, probeData.xyz);
					float3 blastDir = (objectOrigin - probeData.xyz) / blastDist;
					float blast01 = clamp((probeData.w - blastDist) / probeData.w, 0, 1);

					blastModif += blastDir * blast01 * probeTimeAmpl;
					blastValue += blast01 * probeTimeAmpl;
				}
				blastModif *= 0.5;
				blastValue *= 0.5;
#endif
				blastModif = blastModif * BlastAmpl * windAmpMultiplier;

				// Offsets the local vertex position with the wind offsets to create new positions
				float3 worldVertexPosition = WorldSpacePosition + float3(windNoise.x, 0, windNoise.y) + float3(blastModif.x, 0, blastModif.z);
				float3 localVertexPosition = TransformWorldToObject(worldVertexPosition);
				localVertexPosition.y = lerp(localVertexPosition.y, clamp(0.4, 1.5, localVertexPosition.y - blastValue * 0.4), blastValue);

				// Scales the whole mesh based on camera distance
				float distToCamera = distance(_WorldSpaceCameraPos, WorldSpacePosition);
				float cameraDistScaler = saturate(invLerp(MinCamDist, MaxCamDist, distToCamera)); // Determines the size the grass should have at this distance from the camera
				localVertexPosition = localVertexPosition * cameraDistScaler;


				VertexPositionInputs vertexInput = GetVertexPositionInputs(localVertexPosition.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

				output.positionCS = ObjectToClipPos(localVertexPosition);
				output.normalWS = vertexNormalInput.normalWS;

				return output;
			}

			float frag(Attributes IN) : SV_Target {
				return 1;
			}


		ENDHLSL
		}
        
		Pass
		{
			Name "DepthNormals"
			Tags{"LightMode" = "DepthNormals"}

			ZWrite On
			Cull[_Cull]

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
			ENDHLSL
		}
    }
	Fallback Off
}
