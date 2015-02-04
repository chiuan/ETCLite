Shader "Unlit/Transparent Colored ETC1"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AlphaTex ("Alpha (A)", 2D) = "white" {}
	}
	
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Offset -1, -1
			ColorMask RGB
			AlphaTest Greater .01
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMaterial AmbientAndDiffuse
			
			CGPROGRAM
            #pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _AlphaTex;
			
			struct appdata {
	        float4 vertex : POSITION;
	        float2 texcoord : TEXCOORD0;
	        fixed4 color : COLOR;
	    };
			
			struct v2f
			{
			float4 pos : SV_POSITION;
	        half2 uv : TEXCOORD0;
	        fixed4 color : COLOR;
	        };
	        
	        v2f vert(appdata v) {
	        v2f result;
	        
	        result.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	        result.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	        result.color = v.color;
	        
	        return result;
	        }
	        
	        float4 frag(v2f i) : COLOR
	        {
	        half4 color;
	        color.rgb = tex2D(_MainTex, i.uv).rgb;
	        color.a = tex2D(_AlphaTex, i.uv).r;
	        
	        return color * i.color;
	        }
			ENDCG
		}
	}
}