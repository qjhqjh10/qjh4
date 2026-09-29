// TraitDisabled.shader —— 场上单位徽标的**未激活态**（灰化）
//
// 原版出处（逐份 JSON 实读，2026-09-29）：
//   · `BoardTraitIcon.disabledMaterial`（字段偏移 `+0x60`）指向的材质资源是
//     `bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1344813161239158161.json`，
//     `m_Name = "Sprite Greyscale"`，属性只有一条 `_GreyScale = 1.0`。
//   · 什么时候换它：`BoardTraitIcon__Initialize.c` 每一轮的
//     `SetMaterial(渲染器, param_4 == 0 ? +0x60 /*disabledMaterial*/ : +0x80 /*originalMaterial*/)`
//     —— `param_4` 就是 `CardTrait.IsActive(card)`。
//
// ⚠️ 我们**没有**沿包加载原版那个 shader（它不在我们已抽出的那份里），而是自建这一个：
//     对外契约与它一致（同一个属性名 `_GreyScale`、同一个语义「0=原色 / 1=全灰」）。
// ⚠️ 混合模式与 `Sprites/Default` 保持一致（`Blend SrcAlpha OneMinusSrcAlpha` + `ZWrite Off`）——
//     徽标是靠 **z** 分层的（`CardView.BadgeIconZ` 那三个常量），改了混合/写深度会破坏层序。
// ⚠️ 必须有 `_Color`：`CardView.ApplyTint` 会把整卡状态色写进 `_layers` 里每一份材质的
//     `Material.color`（= `_Color`）。少了它，卡整体淡出时徽标**不跟着淡**。
Shader "CardPresentation/TraitDisabled"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GreyScale ("Greyscale", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GreyScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // Rec.601 亮度权重 —— 只降饱和、**不动 alpha**（原版那层是「灰但仍然实」）。
                fixed lum = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, lum.xxx, saturate(_GreyScale));
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
