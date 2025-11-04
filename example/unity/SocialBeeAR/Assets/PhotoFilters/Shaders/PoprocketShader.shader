Shader "Photo Filters/Poprocket"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _ColorInner ("Inner color", Color) = (0.8, 0.15, 0.275, 1.0)
        _ColorOuter ("Outer color", Color) = (0.06, 0.02, 0.18, 1.0)
		[IntRange] _StencilRef ("Stencil Reference Value", Range(0,255)) = 1
        [IntRange] _CompareFunction ("CompareFunction", Range(0,8)) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Plane" }
        Stencil
		    {
			Ref [_StencilRef]
			Comp [_CompareFunction]
            Pass keep 
            ZFail decrWrap
		    }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "PhotoFilterHelper.cginc"

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
            float4 _ColorInner;
            float4 _ColorOuter;

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
                
                float innerGradient = (1 - RadialGradient(i.uv, 1));
                float4 inner = ScreenBlend(col, innerGradient * _ColorInner);
                col = lerp(col, inner, innerGradient);
                
                float outerGradient = RadialGradient(i.uv, 1);
                float4 outer = OverlayBlend(col, outerGradient * _ColorOuter);
                
                col = lerp(col, saturate(outer), outerGradient);
                
                return col;
            }
            ENDCG
        }
    }
}
