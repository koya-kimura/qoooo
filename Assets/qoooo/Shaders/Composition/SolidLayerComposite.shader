Shader "Hidden/Qoooo/VJ/SolidLayerComposite"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 _LayerColor;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                float2 position = float2(
                    vertexID == 1 ? 3.0 : -1.0,
                    vertexID == 2 ? 3.0 : -1.0);
                output.positionCS = float4(position, 0.0, 1.0);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return _LayerColor;
            }
            ENDHLSL
        }
    }
}
