Shader "Photo Filters/Amaro"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _Ramp ("Ramp", 3D) = "white" { }
        _VignetteRadius ("Vignette Radius", Range(0, 1)) = 0
        _VignetteSoftness ("Vignette Softness", Range(0, 1)) = 0
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
            sampler3D _Ramp;
            float _VignetteRadius;
            float _VignetteSoftness;
            
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
                col.rgb = BrightnessContrast(col.rgb, 0.2, 1.12);
                col.rgb *= float3(0.95686, 0.92, 0.74);
                col.b = Level1(col.b, 0, 1, 255, 117, 255);
                col = tex3D(_Ramp, saturate(float3(col.r, col.g, col.b)));
                col *= Vignette(i.uv, _VignetteRadius, _VignetteSoftness);
                
                return col;
            }
            ENDCG
        }
    }
}
