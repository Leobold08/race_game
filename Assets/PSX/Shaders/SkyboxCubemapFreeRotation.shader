Shader "Custom/Skybox/Cubemap Free Rotation"
{
    Properties
    {
        _Tint("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1

        _Yaw("Yaw (Y)", Range(-180, 180)) = 0
        _Pitch("Pitch (X)", Range(-90, 90)) = 0
        _Roll("Roll (Z)", Range(-180, 180)) = 0

        [NoScaleOffset] _Tex("Cubemap (HDR)", Cube) = "grey" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            fixed4 _Tint;
            half _Exposure;
            float _Yaw;
            float _Pitch;
            float _Roll;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float3x3 RotationYawPitchRoll(float yawDeg, float pitchDeg, float rollDeg)
            {
                float yaw = radians(yawDeg);
                float pitch = radians(pitchDeg);
                float roll = radians(rollDeg);

                float sy, cy;
                float sx, cx;
                float sz, cz;
                sincos(yaw, sy, cy);
                sincos(pitch, sx, cx);
                sincos(roll, sz, cz);

                float3x3 rotY = float3x3(
                    cy, 0, sy,
                    0, 1, 0,
                    -sy, 0, cy
                );

                float3x3 rotX = float3x3(
                    1, 0, 0,
                    0, cx, -sx,
                    0, sx, cx
                );

                float3x3 rotZ = float3x3(
                    cz, -sz, 0,
                    sz, cz, 0,
                    0, 0, 1
                );

                return mul(rotY, mul(rotX, rotZ));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 rotated = mul(RotationYawPitchRoll(_Yaw, _Pitch, _Roll), v.vertex.xyz);
                o.vertex = UnityObjectToClipPos(float4(rotated, 1.0));
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 encoded = texCUBE(_Tex, i.texcoord);
                half3 color = DecodeHDR(encoded, _Tex_HDR);
                color *= _Tint.rgb * _Exposure;
                return half4(color, 1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}
