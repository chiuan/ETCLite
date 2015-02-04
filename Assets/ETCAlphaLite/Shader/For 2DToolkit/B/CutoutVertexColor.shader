// unlit, vertex colour, alpha blended
// cull off

Shader "tk2d/CutoutVertexColor (ETC+Alpha using B channel)" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AlphaTex ("Trans (A)", 2D) = "white" {}
	}

	SubShader
	{
		Tags {"IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		Lighting Off Cull Off Fog { Mode Off } AlphaTest Greater 0
		LOD 110
		
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert_vct
			#pragma fragment frag_mult
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float4 _MainTex_ST;

			struct vin_vct 
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f_vct
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f_vct vert_vct(vin_vct v)
			{
				v2f_vct o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				return o;
			}

			fixed4 frag_mult(v2f_vct i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.texcoord);
				fixed4 cola = tex2D(_AlphaTex, i.texcoord);
				col.a=cola.b;
				return col* i.color;
			}
						
			ENDCG
		} 
	}
 
 		SubShader 
	{
		Tags { "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		AlphaTest Greater 0	Blend Off Cull Off Fog { Mode Off }
		LOD 100

		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "TexCoord", texcoord
			Bind "Color", color
		}

		Pass 
		{
			Lighting Off
			SetTexture [_MainTex] { combine texture * primary } 
		}
	}
}
