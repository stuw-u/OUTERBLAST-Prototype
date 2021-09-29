Shader "zhangguangmu/tutorial/lensflare/LensFlareAdditive"
{
    Properties
    {
        _MainTex("Texture",2D)="white"
        _OccludedSizeScale("Cooluded Size scale",Float)=1.0
    }
    SubShader
    {
        Pass
        {
            Tags{"RenderQueue"="Transparent"}
            Blend One One
            ColorMask RGB
            ZWrite Off
            Cull Off
            ZTest Always
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "LensFlareCommon.hlsl"
            real4 frag(v2f i):SV_Target
            {
                float4 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                return col*i.color;
            }
            ENDHLSL
        }
    }
}