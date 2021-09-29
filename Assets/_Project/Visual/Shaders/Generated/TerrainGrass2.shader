Shader "TerrainGrass2"
{
	Properties 
	{
		TopColorA("Top Color A", Color) = (0.3250979, 0.8301887, 0.3679773, 0)
		TopColorB("Top Color B", Color) = (0.3064468, 0.764151, 0.5423112, 0)
		TopPatternScale("Top Pattern Scale", Float) = 1
		BurntColor("Burnt Color", Color) = (0.09141865, 0, 0.1415094, 0.8431373)
		TriplanarColorBlend("Color Triplanar Blend", Float) = 8
		EmitValue("Emit", Float) = 0.16

		Smoothness("Smoothness", Float) = 0
		WindNoiseScale("Wind Noise Scale", Float) = 4
		WindSpeed("Wind Speed", Vector) = (1, 0.2, 0, 0)
		WindAmplitude("Wind Amplitude", Float) = 0.1
		WindHeightInfluence("Wind Height Influence", Float) = 1
		BlastAmplitude("Blast Amplitude", Float) = 1
		BlastLenght("Blast Lenght", Float) = 1
		BlastTimeRatio("Blast TimeRatio", Float) = 0.1
	}

	SubShader 
	{
		Tags 
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry+0"
		}

		// Surface
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

			// Pragmas, Includes and Attributes / Varyings
			#if 1 
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local_fragment _EMISSION
			//#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
			//#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			//#pragma shader_feature_local_fragment _OCCLUSIONMAP
			//#pragma shader_feature_local _PARALLAXMAP
			//#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
			//#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			//#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			//#pragma shader_feature_local_fragment _SPECULAR_SETUP
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF

			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK

			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			//#include "TerrainUtils.hlsl"
			//#include "Assets/_Project/Visual/Shaders/Generated/TerrainUtils.hlsl"
			#include "Assets/_Project/Visual/Shaders/Generated/TerrainLighting.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			// Shader Properties
			CBUFFER_START(UnityPerMaterial)
			float4 TopColorA;
			float4 TopColorB;
			float TopPatternScale;
			float4 BurntColor;
			float EmitValue;
			float Smoothness;
			float WindNoiseScale;
			float4 WindSpeed;
			float WindAmplitude;
			float WindHeightInfluence;
			float BlastAmplitude;
			float BlastLenght;
			float BlastTimeRatio;
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
				float4 uv           : TEXCOORD0;
				float2 lightmapUV   : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS               : SV_POSITION;
				float4 topPatternColor          : COLOR;
				float4 emitColor				: TEXCOORD9;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				// Note this macro is using TEXCOORD1
				#ifdef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
				float3 positionWS               : TEXCOORD2;
				#endif
				float3 normalWS                 : TEXCOORD3;
				#ifdef _NORMALMAP
				float4 tangentWS                : TEXCOORD4;
				#endif
				float3 viewDirWS                : TEXCOORD5;
				half4 fogFactorAndVertexLight   : TEXCOORD6;
				// x: fogFactor, yzw: vertex light
				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				float4 shadowCoord              : TEXCOORD7;
				#endif
				float smoothnessValue			: TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			#endif

			void setup() {
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				unity_ObjectToWorld = matriciesBuffer[unity_InstanceID];
				unity_WorldToObject = invMatriciesBuffer[unity_InstanceID];
			#endif
			}

			// Init Input
			#if 1
			InputData InitializeInputData(Varyings IN, float3 normalWS) {
				InputData inputData = (InputData)0;

				#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
				inputData.positionWS = IN.positionWS;
				#endif

				half3 viewDirWS = SafeNormalize(IN.viewDirWS);
				inputData.normalWS = normalWS;

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = viewDirWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				inputData.shadowCoord = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
				inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
				return inputData;
			}
			#endif

			// Surface Data
			#if 1
			SurfaceData InitializeSurfaceData(Varyings IN) {
				SurfaceData surfaceData = (SurfaceData)0;
				// Note, we can just use SurfaceData surfaceData; here and not set it.
				// However we then need to ensure all values in the struct are set before returning.
				// By casting 0 to SurfaceData, we automatically set all the contents to 0.


				// Not supporting the metallic/specular map or occlusion map
				// for an example of that see : /Shaders/LitInput.hlsl
				surfaceData.albedo = IN.topPatternColor.rgb;
				surfaceData.alpha = 1.0;
				surfaceData.smoothness = lerp(0.0, Smoothness, IN.smoothnessValue);
				surfaceData.normalTS = normalize(IN.normalWS); //NOTE: surfaceTS in this case is in WS!
				surfaceData.emission = IN.emitColor;
				surfaceData.occlusion = 0.0;

				return surfaceData;
			}
			#endif

			// Verts
			#if 1
			Varyings vert(Attributes IN, uint instanceID: SV_InstanceID) {
				Varyings OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);


				// Vertex Position
				VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = positionInputs.positionCS;
				#ifdef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
				OUT.positionWS = positionInputs.positionWS;
				#endif


				float3 TimeParameters = _TimeParameters.xyz;
				float3 positionWS = positionInputs.positionWS;

				// Determines the offsets to the local verticies needed to simulate wind
				float windAmpMultiplier = IN.positionOS.y * WindHeightInfluence; // Determines how strong the wind should effect this vertex
				float2 windUV = (WindSpeed.xy * TimeParameters.x.xx) + (float2(positionWS.x, positionWS.z) * WindNoiseScale); // Noise scale should NOT affect speed
				float2 windNoise = float2(gradientValue(windUV, 1), gradientValue(windUV + float2(900, -200), 1)); // Generates a coherent wind direction vector
				windNoise = windNoise * WindAmplitude * windAmpMultiplier;

				// Blast Probles
				float3 blastModif = float3(0, 0, 0);
				float blastValue = 0;
				float4 objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));

				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				for(int b = 0; b < blastProbeCount; b++) {
					float4 probeData = float4(blastProbeData[b].origin, blastProbeData[b].radius);
					float probeTime = saturate((TimeParameters.x - blastProbeData[b].explosionTime) / BlastLenght);

					float probeRaiseTime = unlerp(0, BlastTimeRatio, probeTime);
					float probeLowerTime = unlerp(1, BlastTimeRatio, probeTime);
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
				blastModif = blastModif * BlastAmplitude * windAmpMultiplier;


				// Offsets the local vertex position with the wind offsets to create new positions
				float3 worldVertexPosition = positionWS + float3(windNoise.x, 0, windNoise.y) + float3(blastModif.x, 0, blastModif.z);
				float3 localVertexPosition = TransformWorldToObject(worldVertexPosition);
				localVertexPosition.y = lerp(localVertexPosition.y, clamp(0.4, 1.5, localVertexPosition.y - blastValue * 0.4), blastValue);
				OUT.positionCS = ObjectToClipPos(localVertexPosition);


				// UVs & Vertex Colour
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				float burntValue = grassDataBuffer[instanceID].burntValue * BurntColor.a;
#else
				float burntValue = 0.0;
#endif
				float lighten = saturate(IN.positionOS.y * 1) * (1.0 - burntValue);
				OUT.smoothnessValue = saturate(IN.positionOS.y * 2) * (1.0-burntValue);
				OUT.topPatternColor.xyz = saturate(sampleGroundColor(OUT.positionWS.xz * TopPatternScale.xx, TopColorA.rgb, TopColorB.rgb) + lighten * 0.12);
				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				OUT.topPatternColor.xyz = lerp(OUT.topPatternColor.xyz, BurntColor.rgb, burntValue);
				#endif

				// View Direction
				OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

				// Normals & Tangents
				VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
				OUT.normalWS = normalInputs.normalWS;
				#ifdef _NORMALMAP
				real sign = IN.tangentOS.w * GetOddNegativeScale();
				OUT.tangentWS = half4(normalInputs.tangentWS.xyz, sign);
				#endif

				float fresnelValue = fresnelEffect(OUT.normalWS, OUT.viewDirWS, 1.0);
				float3 baseEmitColor = lerp(OUT.topPatternColor.xyz * EmitValue.xxx, BurntColor.rgb, burntValue);
				float3 trueEmitColor = baseEmitColor.xyz * fresnelValue.xxx;
				OUT.emitColor = trueEmitColor.xyzx;


				// Vertex Lighting & Fog
				half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
				half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
				OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);


				// Baked Lighting & SH (used for Ambient if there is no baked)
				OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
				OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);


				// Shadow Coord
				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				OUT.shadowCoord = GetShadowCoord(positionInputs);
				#endif
				return OUT;
			}
			#endif

			// Frag
			#if 1
			half4 frag(Varyings IN) : SV_Target{
				UNITY_SETUP_INSTANCE_ID(IN);

				SurfaceData surfaceData = InitializeSurfaceData(IN);
				InputData inputData = InitializeInputData(IN, surfaceData.normalTS); //NOTE: surfaceTS in this case is in WS!

				half4 color = Modif_UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);

				return color;
			}
			#endif

			ENDHLSL
		}


		// DepthOnly
		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			Cull Off
			ColorMask 0

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Assets/_Project/Visual/Shaders/Generated/TerrainLighting.hlsl"

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// Shader Properties
			CBUFFER_START(UnityPerMaterial)
			float4 TopColorA;
			float4 TopColorB;
			float TopPatternScale;
			float4 BurntColor;
			float EmitValue;
			float Smoothness;
			float WindNoiseScale;
			float4 WindSpeed;
			float WindAmplitude;
			float WindHeightInfluence;
			float BlastAmplitude;
			float BlastLenght;
			float BlastTimeRatio;
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

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			

			struct Attributes
			{
				float4 positionOS     : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			void setup() {
				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				unity_ObjectToWorld = matriciesBuffer[unity_InstanceID];
				unity_WorldToObject = invMatriciesBuffer[unity_InstanceID];
				#endif
			}

			Varyings DepthOnlyVertex(Attributes IN, uint instanceID: SV_InstanceID) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				// Vertex Position
				VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = positionInputs.positionCS;


				float3 TimeParameters = _TimeParameters.xyz;
				float3 positionWS = positionInputs.positionWS;

				// Determines the offsets to the local verticies needed to simulate wind
				float windAmpMultiplier = IN.positionOS.y * WindHeightInfluence; // Determines how strong the wind should effect this vertex
				float2 windUV = (WindSpeed.xy * TimeParameters.x.xx) + (float2(positionWS.x, positionWS.z) * WindNoiseScale); // Noise scale should NOT affect speed
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

					float probeRaiseTime = unlerp(0, BlastTimeRatio, probeTime);
					float probeLowerTime = unlerp(1, BlastTimeRatio, probeTime);
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
				blastModif = blastModif * BlastAmplitude * windAmpMultiplier;


				// Offsets the local vertex position with the wind offsets to create new positions
				float3 worldVertexPosition = positionWS + float3(windNoise.x, 0, windNoise.y) + float3(blastModif.x, 0, blastModif.z);
				float3 localVertexPosition = TransformWorldToObject(worldVertexPosition);
				localVertexPosition.y = lerp(localVertexPosition.y, clamp(0.4, 1.5, localVertexPosition.y - blastValue * 0.4), blastValue);
				OUT.positionCS = ObjectToClipPos(localVertexPosition);
				return OUT;
			}

			half4 DepthOnlyFragment(Varyings input) : SV_TARGET {
				return 0;
			}
			ENDHLSL
		}
	}
}