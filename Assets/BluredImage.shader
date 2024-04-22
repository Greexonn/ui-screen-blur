Shader "Custom/BluredImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        LOD 100
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Filter.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BlurFinal);
            SAMPLER(sampler_BlurFinal);
            float4 _BlurFinal_ST;

            v2f vert (appdata v)
            {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _BlurFinal);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = SAMPLE_TEXTURE2D_LOD(_BlurFinal, sampler_BlurFinal, i.uv, 0.0);
                col.a = 1.0;
                
                return col * i.color;
            }
            ENDCG
        }
    }
}
