// Se usa la técnica Inverted Hull. Es decir, el shader dibuja una copia del objeto ligeramente
// ensanchada/más grande, y el propio objeto oculta su parte central, dejando visible un borde
// alrededor, un contorno.
Shader "Custom/PickupOutline"
{
    // Parámetros que se pueden modificar desde el Inspector del material
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.8, 0.8, 0.8, 1) // color del contorn
        _OutlineWidth ("Outline Width (world units)", Range(0, 0.02)) = 0.003 // grosor del contorno
    }

    SubShader
    {
        Tags { 
            "RenderType" = "Opaque"  // shader opaco
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry+10"  // se sitúa en una cola ligeramente posterior a la geometría opaca habitual
        }

        Pass
        {
            Name "PickupOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front      // se descartan las caras frontales
            ZTest LEqual    // se respecta la profundidad (si el fragmento puede dibujarse)
            ZWrite Off      // se conserva la profundidad existente (no se modifica el búfer de profundidad)

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // valores que se reciben desde el código del material (color y grosor)
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            // OS = Object Space
            struct Attributes
            {
                float4 positionOS : POSITION;   // posición del vértice en coordenadas locales
                float3 normalOS : NORMAL;       // normal del vértice en coordenadas locales
            };

            // HCS = Homogeneous Clip Space
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // Ensancha la copia del objeto
            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // el grosor se expresa en metros!
                positionWS += normalWS * _OutlineWidth;
                output.positionHCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // se asigna el color sin iluminación, para que también se distinga en zonas
                // oscuras pero no ilumine su alrededor
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off    // significa que no se especifica otra implementación alternativa
}
