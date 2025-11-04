Shader "Photo Filters/Color Remap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
		_Ramp("Ramp", 3D) = "white" { }
		_Intensivity ("Intensivity", Range(0, 1)) = 1
        [HideInInspector] __dirty( "", Int ) = 1
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

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform sampler3D _Ramp;
			uniform float _Intensivity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = saturate(tex2D(_MainTex, i.uv));
                col = lerp(col, tex3D(_Ramp, saturate(float3(col.r, col.g, col.b))), _Intensivity);
                return col;
            }
            ENDCG
        }
    }
}
