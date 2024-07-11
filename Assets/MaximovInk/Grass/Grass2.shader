Shader "Custom/InstancedIndirectColor" {
   Properties{
     _BaseMap ("BaseMap", 2D) = "" {}
     _Color ("Color", Color) = (1,1,1,1) 
     _ColorTip ("ColorTip", Color) = (1,1,1,1)
     _ColorDistance ("ColorDistance", Color) = (1,1,1,1)
     _DistanceLerp ("DistanceLerp", Float) = 100 
     _MinDistance ("MinDistanceLerp", Float) = 50 
     _TipOffset ("TipOffset", Float) = 0.0 
   }

    SubShader {
        Blend SrcAlpha OneMinusSrcAlpha
       Cull Off 
        //Tags { "RenderType" = "Opaque" }
         // Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
         //Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
            Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

        Pass {
            CGPROGRAM
            #pragma vertex vert alphatest:_Cutoff 
            #pragma fragment frag 

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float3 normal   : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv : TEXCOORD0;
                float dist : TEXCOORD1;
            }; 

            struct MeshProperties {
                float4x4 mat;
                float4 color;
            };

            sampler2D _BaseMap;
            fixed4 _Color;
            fixed4 _ColorTip;
            fixed4 _ColorDistance;
            float _TipOffset;
            float _DistanceLerp;
            float _MinDistance;

            StructuredBuffer<MeshProperties> _Properties;

            const float3 vect3Zero = float3(0.0, 0.0, 0.0);

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                
                o.vertex = UnityObjectToClipPos(pos);
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;

                o.dist = (length(UnityObjectToViewPos(i.vertex)))/_DistanceLerp;

                return o;
            }


            fixed4 frag(v2f i) : SV_Target {
                //return i.color;
                half4 texC = tex2D(_BaseMap, i.uv).rgba;
                clip(texC.a - 0.5);

                float d = i.dist;

                fixed4 lerpedColor = lerp(_Color,_ColorTip,  clamp(i.uv.y + _TipOffset,0,1));

                d = clamp(d - _MinDistance,0,1.0);


               // return lerpedColor * texC * i.color;
                

                return lerp(lerpedColor * texC * i.color, _ColorDistance, d);
              
                //return lerpedColor * texC * i.color;
            }

            ENDCG
        }
    }
}
