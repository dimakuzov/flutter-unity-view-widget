Shader "Unlit/SceneCombineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OriginalTexture ("Texture", 2D) = "white" {}
        _UITexture ("Texture", 2D) = "white" {}
        [Toggle] _EnableComparison("Enable Comparison", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _OriginalTexture;
            sampler2D _UITexture;
            fixed _EnableComparison;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 ui = tex2D(_UITexture, i.uv);
                fixed4 original = tex2D(_OriginalTexture, i.uv);
                col = lerp(col, original, step(i.uv.y + _SinTime.w, i.uv.x) * _EnableComparison);
                col *= 1 - min(step(abs(i.uv.x - (i.uv.y + _SinTime.w)), (_ScreenParams.w - 1.0) * 5), _EnableComparison);
                col = lerp(col, ui, ui.a);
                return col;
            }
            ENDCG
        }
    }
}
