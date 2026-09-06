Shader "Game/AssignedItemOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.9, 0.05, 0.05, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.05)) = 0.012
        _OutlinePixels ("Outline Pixels", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+1"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AssignedItemOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlinePixels;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                if (_OutlinePixels > 0.001)
                {
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                    float4 positionCS = TransformWorldToHClip(positionWS);
                    float worldPerPixel = (2.0 * positionCS.w) /
                                          (max(_ScreenParams.y, 1.0) * abs(UNITY_MATRIX_P._m11));
                    output.positionCS = TransformWorldToHClip(
                        positionWS + (normalWS * (_OutlinePixels * worldPerPixel)));
                    return output;
                }

                float3 expanded = input.positionOS.xyz +
                                  (normalize(input.normalOS) * _OutlineWidth);
                output.positionCS = TransformObjectToHClip(expanded);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
