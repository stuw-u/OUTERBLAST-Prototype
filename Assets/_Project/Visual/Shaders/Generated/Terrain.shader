Shader "Terrain"
{
    Properties
    {
        TopColorA("TopColorA", Color) = (0.3250979, 0.8301887, 0.3679773, 0)
        TopColorB("TopColorB", Color) = (0.3064468, 0.764151, 0.5423112, 0)
        SideColorA("SideColorA", Color) = (0.8018868, 0.6532211, 0.5409641, 0)
        SideColorB("SideColorB", Color) = (0.6320754, 0.3469485, 0.3250364, 0)
        BurntColor("BurntColor", Color) = (0.09141865, 0, 0.1415094, 0.8431373)
        SideColorRange("SideColorRange", Vector) = (32, -32, 0, 0)
        TriplanarBlend("Blend", Float) = 8
        EmitValue("Emit", Float) = 0.16
        TopPatternScale("Scale", Float) = 1
        WarpValue("WarpValue", Float) = 0.2
        WarpScale("WarpScale", Float) = 0.25

		TopNormalMap("TopNormalMap", 2D) = "bump" {}
		TopNormalAmplitude("TopNormalAmplitude", Float) = 1.0
		SideNormalMap("SideNormalMap", 2D) = "bump" {}
		SideOcclusionMap("SideOcclusionMap", 2D) = "white" {}
		SideNormalAmplitude("SideNormalAmplitude", Float) = 1.0
		SideOcclusionAmplitude("SideOcclusionAmplitude", Float) = 1.0
		NormalScale("NormalScale", Float) = 1.0
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
            Cull Back
            ZTest LEqual
            ZWrite On
            
        
            HLSLPROGRAM
            
        
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
        
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            
            // Defines
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_POSITION_WS 
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
            #pragma multi_compile_instancing
            #define SHADERPASS_FORWARD

            
            // Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			// Shader Properties
            CBUFFER_START(UnityPerMaterial)
            float4 TopColorA;
            float4 TopColorB;
            float4 SideColorA;
            float4 SideColorB;
            float4 BurntColor;
            float2 SideColorRange;
            float TriplanarBlend;
            float EmitValue;
            float TopPatternScale;
            float WarpValue;
            float TopNormalAmplitude;
            float SideNormalAmplitude;
            float SideOcclusionAmplitude;
            float NormalScale;
            CBUFFER_END
			TEXTURE2D(TopNormalMap);
			SAMPLER(sampler_TopNormalMap);
			TEXTURE2D(SideNormalMap);
			SAMPLER(sampler_SideNormalMap);
			TEXTURE2D(SideOcclusionMap);
			SAMPLER(sampler_SideOcclusionMap);



			struct Attributes {
				float3 positionOS	: POSITION;
				float3 normalOS		: NORMAL;
				float4 tangentOS	: TANGENT;
				float4 uv			: TEXCOORD0;
				float4 uvLM			: TEXCOORD1;
			};

			struct Varyings {
				float2 uv                       : TEXCOORD0;
				float2 uvLM                     : TEXCOORD1;
				float4 positionWSAndFogFactor   : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
				float4 positionCS				: SV_POSITION;
				float4 vertexColor				: COLOR;
				half3 normalWS					: TEXCOORD3;
				half3 tangentWS					: TEXCOORD4;
				half3 bitangentWS               : TEXCOORD5;
				float3 viewDirectionWS			: TEXCOORD6;
				float4 screenPos				: TEXCOORD7;

				#ifdef _MAIN_LIGHT_SHADOWS
				float4 shadowCoord              : TEXCOORD8; // compute shadow coord per-vertex for the main light
				#endif

				#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
				uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
				#endif
				#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
				uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
				#endif
				#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
				FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
				#endif
			};

            

            

			float4 ObjectToClipPos(float3 pos) {
				return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4 (pos, 1)));
			}

			InputData InitializeInputData(Varyings IN, half3 normalTS) {
				InputData inputData = (InputData)0;

				#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
				inputData.positionWS = IN.positionWSAndFogFactor.xyz;
				#endif

				half3 viewDirWS = SafeNormalize(IN.viewDirectionWS);
				inputData.normalWS = IN.normalWS;

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = viewDirWS;
				//inputData.shadowCoord = IN.shadowCoord.xy;

				inputData.fogCoord = IN.positionWSAndFogFactor.w;
				//inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				return inputData;
			}

			Varyings vert(Attributes IN) {
				Varyings output;
				float3 WorldSpacePosition = TransformObjectToWorld(IN.positionOS.xyz);
				float3 localVertexPosition = IN.positionOS.xyz;//TransformWorldToObject(worldVertexPosition);
				float2 scaleWorldPosXZ = WorldSpacePosition.xz * TopPatternScale.xx;


				// Prepare expensive noise samples in vertex
				float voronoiSum = 0.0;
				voronoiSum += voronoi(scaleWorldPosXZ + float2(0.0, -0.0), 6) * 0.55;
				voronoiSum += voronoi(scaleWorldPosXZ + float2(5000.0, -5000.0), 12) * 0.25;
				voronoiSum += voronoi(scaleWorldPosXZ + float2(-5000.0, 5000.0), 24) * 0.20;
				float4 topColor = lerp(TopColorA, TopColorB, voronoiSum);
				output.vertexColor = topColor;


				VertexPositionInputs vertexInput = GetVertexPositionInputs(localVertexPosition.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

#ifdef _MAIN_LIGHT_SHADOWS
				output.shadowCoord = GetShadowCoord(vertexInput);
#endif

				float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				output.positionCS = ObjectToClipPos(localVertexPosition);
				float4 screenPos = ComputeScreenPos(output.positionCS);
				output.screenPos = screenPos;
				output.normalWS = vertexNormalInput.normalWS;
				output.uv = IN.uv;
				output.uvLM = IN.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);

				output.tangentWS = vertexNormalInput.tangentWS;
				output.bitangentWS = vertexNormalInput.bitangentWS;

				return output;
			}

			float4 frag(Varyings IN) : SV_Target {

				SurfaceData surfaceData = (SurfaceData)0;
				InitializeStandardLitSurfaceData(IN.uv, surfaceData);

				float3 positionWS = IN.positionWSAndFogFactor.xyz;
				float3 worldPos = positionWS;
				half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
				float2 screenSpace = float4(IN.screenPos.xy / IN.screenPos.w, 0, 0);
				float3 lookDir = -1 * mul((float3x3)UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))[2].xyz);

				
				float noiseValue = noise(positionWS * 1.5) * 0.3 + noise(positionWS * 3)*0.15 + noise(positionWS * 9)*0.05;			// General Noise Value
				float4 topColor = IN.vertexColor;																					// Top Color
				float4 sideColor = lerp(SideColorA, SideColorB, saturate(unlerp(SideColorRange.x, SideColorRange.y, worldPos.y)));	// Side Color
				float cutValue = 1.0 - step(IN.uv.g, 0.5 + noiseValue * 0.2);														// Cut Value
				float smoothCutValue = 1.0 - saturate(IN.uv.g + noiseValue * 0.2);													// Cut Value
				float4 trueTopColor = lerp(topColor, sideColor, cutValue);															// True Top Color


				half3 normalWS = normalize(IN.normalWS);
				// Triplanar normals
				float blendNormal = 4.0;
				float3 TriplanarNormal_UV = GetAbsolutePositionWS(worldPos) * NormalScale;

				float4 powBlendOcclusion = SafePositivePow_float(float4(normalWS.x, max(0.0, -normalWS.y), max(0.0, normalWS.y), normalWS.z), min(blendNormal, floor(log2(Min_float()) / log2(1 / sqrt(3)))));
				float4 triplanarBlendOcclusion = powBlendOcclusion / dot(powBlendOcclusion, float4(1, 1, 1, 1));

				float4 TriplanarNormal_Blend = SafePositivePow_float(float4(normalWS.x, max(0.0, -normalWS.y), max(0.0, normalWS.y), normalWS.z), min(blendNormal, floor(log2(Min_float()) / log2(1 / sqrt(3)))));
				TriplanarNormal_Blend /= (TriplanarNormal_Blend.x + TriplanarNormal_Blend.y + TriplanarNormal_Blend.z + TriplanarNormal_Blend.w).xxxx;

				float3 TriplanarNormal_X = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.zy)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_YBottom = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.xz)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_YTop = UnpackNormal(SAMPLE_TEXTURE2D(TopNormalMap, sampler_TopNormalMap, TriplanarNormal_UV.xz)) * float4(TopNormalAmplitude, TopNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_Z = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.xy)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);

				float TriplanarOcclusion_X = 1.0-SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.zy).r;
				float TriplanarOcclusion_YBottom = 1.0-SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.xz).r;
				float TriplanarOcclusion_Z = 1.0-SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.xy).r;

				TriplanarNormal_X = float3(TriplanarNormal_X.xy + normalWS.zy, abs(TriplanarNormal_X.z) * normalWS.x);
				TriplanarNormal_YBottom = float3(TriplanarNormal_YBottom.xy + normalWS.xz, abs(TriplanarNormal_YBottom.z) * normalWS.y);
				TriplanarNormal_YTop = float3(TriplanarNormal_YTop.xy + normalWS.xz, abs(TriplanarNormal_YTop.z) * normalWS.y);
				TriplanarNormal_Z = float3(TriplanarNormal_Z.xy + normalWS.xy, abs(TriplanarNormal_Z.z) * normalWS.z);
				normalWS = float4(normalize(
					TriplanarNormal_X.zyx * TriplanarNormal_Blend.x + 
					TriplanarNormal_YBottom.xzy * TriplanarNormal_Blend.y + 
					lerp( TriplanarNormal_YTop.xzy * TriplanarNormal_Blend.z, TriplanarNormal_YBottom.xzy * TriplanarNormal_Blend.z, cutValue) +
					TriplanarNormal_Z.xyz * TriplanarNormal_Blend.w), 1);
				float occlusion = (triplanarBlendOcclusion.x * TriplanarOcclusion_X + triplanarBlendOcclusion.y * TriplanarOcclusion_YBottom + triplanarBlendOcclusion.z * 0.0 + triplanarBlendOcclusion.w * TriplanarOcclusion_Z) * SideOcclusionAmplitude;
				

				float2 scaleWorldPosXZ = worldPos.xz * TopPatternScale.xx;


				// Triplanar Blend Values
				float4 powBlend = pow(float4(abs(normalWS.x), max(0.0, normalWS.y), max(0.0, -normalWS.y), abs(normalWS.z)), TriplanarBlend);
				powBlend.y = 1.0-saturate(step(powBlend.y, noiseValue));
				float4 triplanarBlend = powBlend / dot(powBlend, float4(1, 1, 1, 1));

				// Base Terrain Color
				float3 mulR = triplanarBlend.r * sideColor.rgb;
				float3 mulG = triplanarBlend.g * trueTopColor.rgb;
				float3 mulB = triplanarBlend.b * sideColor.rgb;
				float3 mulA = triplanarBlend.a * sideColor.rgb;
				float3 baseTerrainColor = mulR + mulG + mulB + mulA;

				// True Terrain Color
				float burntValue = lerp(0.0, BurntColor.a, IN.uv.r);
				float3 trueTerrainColor = lerp(baseTerrainColor, BurntColor.rgb, burntValue);

				// Emission
				float fresnelValue = fresnelEffect(normalWS, viewDirectionWS, 1.0);
				float3 baseEmitColor = lerp(baseTerrainColor * EmitValue.xxx, BurntColor.rgb, burntValue);
				float3 trueEmitColor = baseEmitColor * fresnelValue.xxx;

				// BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
				// It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
				// below with your own.


				half alpha = 1.0;
				BRDFData brdfData;
				InitializeBRDFData(trueTerrainColor, 0, 0, 0, alpha, brdfData);

				#ifdef _MAIN_LIGHT_SHADOWS
				Light mainLight = GetMainLight(IN.shadowCoord);
				#else
				Light mainLight = GetMainLight();
				#endif
				
				InputData inputData;
				inputData = InitializeInputData(IN, float3(0.0, 1.0, 0.0));

				half3 color = UniversalFragmentPBR(inputData, surfaceData).xyz;

				//half3 color = LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);
				//half3 color = trueTerrainColor;
				color = max(color, trueEmitColor) * max(saturate(1.0 - occlusion), 0.4);
				color = MixFog(color, IN.positionWSAndFogFactor.w);
				return float4(color, 1);
			}
        
            ENDHLSL
        }
        
		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			ENDHLSL
		}
        
		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
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
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}


// Hash without Sine
// MIT License...
/* Copyright (c)2014 David Hoskins.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/