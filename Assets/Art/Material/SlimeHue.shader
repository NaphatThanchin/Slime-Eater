// เปลี่ยนเฉดสีโมเดลสไลม์ (ใช้กับบอท) สีเปลี่ยนแต่ลาย แสงเงา และหน้าตาเหมือนเดิม
Shader "SlimeEater/HueShiftLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _Hue("Hue Shift", Range(0, 1)) = 0        // หมุนเฉดสี 0-1 รอบวงสี
        _Saturation("Saturation", Range(0, 2)) = 1.15
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float _Hue; float _Saturation;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; float fog : TEXCOORD3; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float3 HueShift(float3 c, float h) // หมุนสีรอบแกนสีเทา (ดำ/ขาวยังเป็นดำ/ขาว)
            {
                const float3 k = float3(0.57735, 0.57735, 0.57735);
                float a = h * 6.2831853, ca = cos(a);
                return c * ca + cross(k, c) * sin(a) + k * dot(k, c) * (1 - ca);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 c = HueShift(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb, _Hue);
                c = lerp(dot(c, float3(0.299, 0.587, 0.114)).xxx, c, _Saturation);
                float3 n = normalize(i.normalWS);
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float d = saturate(dot(n, l.direction)) * l.shadowAttenuation;
                float3 col = c * (l.color * (0.35 + 0.65 * d) + SampleSH(n) * 0.6);  // แสงแดด + แสงรอบข้าง
                return half4(MixFog(col, i.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"   // ให้บอทมีเงาบนพื้นเหมือนผู้เล่น
    }
}
