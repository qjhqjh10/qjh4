// Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.shader
// 名称 = `Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Aeldari Dust · 输出材质）
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/a291e5dde50874f8bb531e73b0985af1.shader`
//   原生 shader 本体另在随包 bundle 里（`StreamingAssets/WarpforgeVFX/wf_arena_shaders.bundle` /
//   `wf_shaders_extra.bundle`，按名可取）；但 Unity 拒绝把 bundle 里的 Shader 序列化成工程资产
//   （项目已知坑），而 `.mat` 的 `m_Shader` 只认工程资产 ⇒ 按 R4-04 的「手写 6 个 shader」路线，
//   把原版反编译体**原样搬进工程**。
//
// 只做了**可移植性归一**（逐条都可逆、无行为差异）：
//   ① 去掉 `#pragma use_dxc` —— 不是 Unity 认的 pragma（Unity 行的是 `never_use_dxc`），让 Unity 自选编译器；
//   ② `register(tN, space0)` → `register(tN)` —— register space 是 DXC/SPIR-V 写法，FXC 不认（槽位不变）；
//   ③ `precise float` → `float` —— 指令级精度提示，不改变数值结果；
//   ④ `float(uint64_t(x))` → `float(x)` —— 同一个 uint→float 转换，去掉 64 位依赖；
//   ⑤ `SPIRV_Cross_BaseVertex/BaseInstance` → 0 —— VFX 走 DrawProcedural，基础顶点/实例恒为 0。
//   ⑥ `ByteAddressBuffer.Load<uint>(x)` → `.Load(x)` —— 同一语义（非模板重载返回 uint），FXC 不认模板形式；
//   ⑦ 片元块里的全局 `static float4 SV_Target;` → `spv_Color` —— HLSLSupport.cginc 在部分编译档把 `SV_Target`
//      宏映射成 `COLOR`，会与本块已有的 `static float4 COLOR;` 重定义；只改**变量名**，输出语义（SV_Target0）不变。
//   **未改**：Shader 名 · SubShader Tags · Pass 名/Tags · Blend/ZWrite/Cull/ZClip · 顶点/片元算式。
//
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`f2642f618e317484b9b0a2930a6690b6`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad" {
    SubShader {
        Tags {
            "IGNOREPROJECTOR"="true"
            "QUEUE"="Transparent+0"
            "RenderType"="Transparent"
        }
        Pass {
            Name "UniversalForwardOnly"
            Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="UniversalForwardOnly"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[23u] : packoffset(c0);
                float4 _ScreenParams : packoffset(c23);
                float4 _Globals_Unmapped_at_384[38u] : packoffset(c24);
                column_major float4x4 glstate_matrix_projection : packoffset(c62);
                float4 _Globals_Unmapped_at_1056[12u] : packoffset(c66);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_outputParamsConst : register(b2)
            {
                float4 instancingConstants : packoffset(c0);
            };

            cbuffer type_PerInstance : register(b3)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t4);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint spvPackHalf2x16(float2 value)
            {
                uint2 Packed = f32tof16(value);
                return Packed.x | (Packed.y << 16);
            }

            float2 spvUnpackHalf2x16(uint value)
            {
                return f16tof32(uint2(value & 0xffff, value >> 16));
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _71(uint _70)
            {
                return float(_70);
            }

            float _79(float3 _77, float3 _78)
            {
                float _83 = _77.x * _78.x;
                return mad(_77.z, _78.z, mad(_77.y, _78.y, _83));
            }

            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _109 = asuint(_InstanceActiveIndex);
                uint _110 = _109 * 224u;
                uint _129 = asuint(_InstanceIndex);
                bool _135 = _103 >= (graphValuesBuffer.Load(ByteAddressMask(_110 >> 2u, 4u) * 4 + 0) - deadList.Load(_129 * 4 + 0));
                if (_135)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_103 >= indirectBuffer.Load(_109 * 4 + 0))
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _182 = ByteAddressMask((_110 + 144u) >> 2u, 4u);
                uint4 _195 = uint4(graphValuesBuffer.Load(_182 * 4 + 0), graphValuesBuffer.Load((_182 + 1u) * 4 + 0), graphValuesBuffer.Load((_182 + 2u) * 4 + 0), graphValuesBuffer.Load((_182 + 3u) * 4 + 0));
                uint _199 = _195.w;
                uint _203 = ByteAddressMask((_110 + 160u) >> 2u, 4u);
                uint4 _215 = uint4(graphValuesBuffer.Load(_203 * 4 + 0), graphValuesBuffer.Load((_203 + 1u) * 4 + 0), graphValuesBuffer.Load((_203 + 2u) * 4 + 0), graphValuesBuffer.Load((_203 + 3u) * 4 + 0));
                uint _219 = _215.w;
                uint _221 = _129 * 170816u;
                uint _229 = _103 + ((_109 * 10000u) + asuint(instancingConstants.z));
                uint _235 = (_221 + indirectBuffer.Load(_229 * 4 + 0)) << 2u;
                uint _246 = (_221 + (indirectBuffer.Load(_229 * 4 + 0) << 2u)) << 2u;
                float _261 = asfloat(attributeBuffer.Load(ByteAddressMask((_235 + 522496u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load(ByteAddressMask((_235 + 321536u) >> 2u, 4u) * 4 + 0));
                float2 _262 = spvUnpackHalf2x16(_195.x);
                float _266 = mad(_261, _262.x, _262.y);
                float _278 = 0.0f;
                switch (_199 >> 2u)
                {
                    case 1u:
                    {
                        _278 = mad(frac(min(_266, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _278 = mad(frac(max(_266, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _278 = mad(clamp(_266, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _278 = _266;
                        break;
                    }
                }
                float2 _279 = spvUnpackHalf2x16(_215.x);
                float _283 = mad(_261, _279.x, _279.y);
                float _292 = 0.0f;
                switch (_219 >> 2u)
                {
                    case 1u:
                    {
                        _292 = mad(frac(min(_283, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _292 = mad(frac(max(_283, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _292 = mad(clamp(_283, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _292 = _283;
                        break;
                    }
                }
                if (attributeBuffer.Load(ByteAddressMask((_246 + 361740u) >> 2u, 4u) * 4 + 0) == 0u)
                {
                    gl_Position.x = _135 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _315 = ByteAddressMask((_110 + 192u) >> 2u, 4u);
                uint2 _321 = uint2(graphValuesBuffer.Load(_315 * 4 + 0), graphValuesBuffer.Load((_315 + 1u) * 4 + 0));
                uint _327 = ByteAddressMask((_110 + 204u) >> 2u, 4u);
                uint3 _337 = uint3(graphValuesBuffer.Load(_327 * 4 + 0), graphValuesBuffer.Load((_327 + 1u) * 4 + 0), graphValuesBuffer.Load((_327 + 2u) * 4 + 0));
                uint _339 = _337.y;
                uint _348 = ByteAddressMask((_246 + 361728u) >> 2u, 4u);
                uint3 _357 = uint3(attributeBuffer.Load(_348 * 4 + 0), attributeBuffer.Load((_348 + 1u) * 4 + 0), attributeBuffer.Load((_348 + 2u) * 4 + 0));
                uint _369 = ByteAddressMask((((_221 + (indirectBuffer.Load(_229 * 4 + 0) * 3u)) << 2u) + 562688u) >> 2u, 4u);
                uint3 _378 = uint3(attributeBuffer.Load(_369 * 4 + 0), attributeBuffer.Load((_369 + 1u) * 4 + 0), attributeBuffer.Load((_369 + 2u) * 4 + 0));
                COLOR.x = asfloat(_378.x);
                COLOR.y = asfloat(_378.y);
                COLOR.z = asfloat(_378.z);
                uint _391 = ByteAddressMask((_246 + 120576u) >> 2u, 4u);
                uint4 _403 = uint4(attributeBuffer.Load(_391 * 4 + 0), attributeBuffer.Load((_391 + 1u) * 4 + 0), attributeBuffer.Load((_391 + 2u) * 4 + 0), attributeBuffer.Load((_391 + 3u) * 4 + 0));
                uint _409 = _199 & 3u;
                float4 _416 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_278, asfloat(_195.z)), 0.0f);
                float _432 = ((_409 == 3u) ? _416.w : ((_409 == 2u) ? _416.z : ((_409 == 1u) ? _416.y : ((_409 == 0u) ? _416.x : 0.0f)))) * asfloat(_195.y);
                float _434 = mad(clamp(_261, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                uint _458 = ((_339 >> 16u) ^ _339) * 73244475u;
                uint _462 = (_458 ^ (_458 >> 16u)) * 73244475u;
                float _470 = asfloat(((_462 ^ (_462 >> 16u)) >> 9u) + 1065353216u) - 1.0f;
                float _474 = asfloat(_357.y);
                float _489 = asfloat(_357.x);
                float _498 = asfloat(_357.z);
                float _562 = (mad(_470, asfloat(_337.z), asfloat(_337.x)) * (mad(asfloat(asuint(unity_MatrixVP[2u].w)), mad(unity_ObjectToWorld[2u].z, _498, mad(unity_ObjectToWorld[0u].z, _489, _474 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z, mad(asfloat(asuint(unity_MatrixVP[0u].w)), mad(unity_ObjectToWorld[2u].x, _498, mad(unity_ObjectToWorld[0u].x, _489, _474 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x, (mad(unity_ObjectToWorld[2u].y, _498, mad(unity_ObjectToWorld[0u].y, _489, _474 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y) * asfloat(asuint(unity_MatrixVP[1u].w)))) + asfloat(asuint(unity_MatrixVP[3u].w)))) / (min(abs(asfloat(asuint(_ScreenParams.y)) * asfloat(asuint(glstate_matrix_projection[1u].y))), abs(asfloat(asuint(_ScreenParams.x)) * asfloat(asuint(glstate_matrix_projection[0u].x)))) * (_432 * 0.5f));
                uint _563 = _219 & 3u;
                float4 _566 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_292, asfloat(_215.z)), 0.0f);
                float _582 = ((_563 == 3u) ? _566.w : ((_563 == 2u) ? _566.z : ((_563 == 1u) ? _566.y : ((_563 == 0u) ? _566.x : 0.0f)))) * asfloat(_215.y);
                float _583 = mad(_470, 720.0f, -360.0f);
                float _599 = asfloat((((_403.x * 1664525u) + 1013904223u) >> 9u) + 1065353216u) - 1.0f;
                float _604 = _71(_94 & 1u);
                float _605 = _71(_94 & 2u);
                float _609 = _562 * _432;
                float _610 = mad(_582, _583, asfloat(_403.y)) * 0.01745329238474369049072265625f;
                float _612 = mad(_582, _583, asfloat(_403.z)) * 0.01745329238474369049072265625f;
                float _613 = mad(_582, _583, asfloat(_403.w)) * 0.01745329238474369049072265625f;
                float _614 = sin(_610);
                float _615 = cos(_610);
                float _616 = sin(_612);
                float _617 = cos(_612);
                float _618 = sin(_613);
                float _619 = cos(_613);
                float _620 = _619 * _617;
                float3 _643 = float3(_604 - 0.5f, mad(_605, 0.5f, -0.5f), 1.0f);
                float _644 = _79(float3(_609 * mad(_616 * _614, _618, _620), _609 * mad(_614 * _619, _616, -(_618 * _617)), _489), _643);
                float _646 = _79(float3(_609 * (_618 * _615), _609 * (_619 * _615), _474), _643);
                float _648 = _79(float3(_609 * mad(-_619, _616, _618 * (_614 * _617)), _609 * mad(_620, _614, _618 * _616), _498), _643);
                float _658 = mad(unity_ObjectToWorld[2u].x, _648, mad(unity_ObjectToWorld[0u].x, _644, _646 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _659 = mad(unity_ObjectToWorld[2u].y, _648, mad(unity_ObjectToWorld[0u].y, _644, _646 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _660 = mad(unity_ObjectToWorld[2u].z, _648, mad(unity_ObjectToWorld[0u].z, _644, _646 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _660, mad((unity_MatrixVP[0u].x), _658, _659 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _660, mad((unity_MatrixVP[0u].y), _658, _659 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _660, mad((unity_MatrixVP[0u].z), _658, _659 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _660, mad((unity_MatrixVP[0u].w), _658, _659 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _729 = 1.0f / _609;
                float3 _738 = float3(-(_729 * (_616 * _615)), -(_729 * (-_614)), -(_729 * (_617 * _615)));
                float _745 = _79(_738, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _753 = _79(_738, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _761 = _79(_738, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _762 = float3(_745, _753, _761);
                float _764 = rsqrt(_79(_762, _762));
                float _771 = ((_609 * (_609 * _609)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_764 * _745) * _771;
                TEXCOORD_6.y = (_764 * _753) * _771;
                TEXCOORD_6.z = (_764 * _761) * _771;
                float _783 = trunc(mad(_599, 4.0f, -frac(_599 * 4.0f))) * 0.5f;
                float _784 = frac(_783);
                TEXCOORD.x = (_604 + (_784 + _784)) * 0.5f;
                TEXCOORD.y = (mad(_605, 0.5f, -floor(_783)) + 1.0f) * 0.5f;
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_321.y) > 0.5f) ? (mad(-frac(_434 * 128.0f), 0.0078125f, _434) + 0.00390625f) : _434, asfloat(_321.x)), 0.0f).w;
                INDEX.x = 0u;
                INDEX.y = _109;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 spv_Color;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                spv_Color.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                spv_Color.x = _44.x * COLOR.x;
                spv_Color.y = _44.y * COLOR.y;
                spv_Color.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = spv_Color;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="Universal2D"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[23u] : packoffset(c0);
                float4 _ScreenParams : packoffset(c23);
                float4 _Globals_Unmapped_at_384[38u] : packoffset(c24);
                column_major float4x4 glstate_matrix_projection : packoffset(c62);
                float4 _Globals_Unmapped_at_1056[12u] : packoffset(c66);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_outputParamsConst : register(b2)
            {
                float4 instancingConstants : packoffset(c0);
            };

            cbuffer type_PerInstance : register(b3)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t4);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint spvPackHalf2x16(float2 value)
            {
                uint2 Packed = f32tof16(value);
                return Packed.x | (Packed.y << 16);
            }

            float2 spvUnpackHalf2x16(uint value)
            {
                return f16tof32(uint2(value & 0xffff, value >> 16));
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _71(uint _70)
            {
                return float(_70);
            }

            float _79(float3 _77, float3 _78)
            {
                float _83 = _77.x * _78.x;
                return mad(_77.z, _78.z, mad(_77.y, _78.y, _83));
            }

            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _109 = asuint(_InstanceActiveIndex);
                uint _110 = _109 * 224u;
                uint _129 = asuint(_InstanceIndex);
                bool _135 = _103 >= (graphValuesBuffer.Load(ByteAddressMask(_110 >> 2u, 4u) * 4 + 0) - deadList.Load(_129 * 4 + 0));
                if (_135)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_103 >= indirectBuffer.Load(_109 * 4 + 0))
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _182 = ByteAddressMask((_110 + 144u) >> 2u, 4u);
                uint4 _195 = uint4(graphValuesBuffer.Load(_182 * 4 + 0), graphValuesBuffer.Load((_182 + 1u) * 4 + 0), graphValuesBuffer.Load((_182 + 2u) * 4 + 0), graphValuesBuffer.Load((_182 + 3u) * 4 + 0));
                uint _199 = _195.w;
                uint _203 = ByteAddressMask((_110 + 160u) >> 2u, 4u);
                uint4 _215 = uint4(graphValuesBuffer.Load(_203 * 4 + 0), graphValuesBuffer.Load((_203 + 1u) * 4 + 0), graphValuesBuffer.Load((_203 + 2u) * 4 + 0), graphValuesBuffer.Load((_203 + 3u) * 4 + 0));
                uint _219 = _215.w;
                uint _221 = _129 * 170816u;
                uint _229 = _103 + ((_109 * 10000u) + asuint(instancingConstants.z));
                uint _235 = (_221 + indirectBuffer.Load(_229 * 4 + 0)) << 2u;
                uint _246 = (_221 + (indirectBuffer.Load(_229 * 4 + 0) << 2u)) << 2u;
                float _261 = asfloat(attributeBuffer.Load(ByteAddressMask((_235 + 522496u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load(ByteAddressMask((_235 + 321536u) >> 2u, 4u) * 4 + 0));
                float2 _262 = spvUnpackHalf2x16(_195.x);
                float _266 = mad(_261, _262.x, _262.y);
                float _278 = 0.0f;
                switch (_199 >> 2u)
                {
                    case 1u:
                    {
                        _278 = mad(frac(min(_266, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _278 = mad(frac(max(_266, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _278 = mad(clamp(_266, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _278 = _266;
                        break;
                    }
                }
                float2 _279 = spvUnpackHalf2x16(_215.x);
                float _283 = mad(_261, _279.x, _279.y);
                float _292 = 0.0f;
                switch (_219 >> 2u)
                {
                    case 1u:
                    {
                        _292 = mad(frac(min(_283, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _292 = mad(frac(max(_283, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _292 = mad(clamp(_283, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _292 = _283;
                        break;
                    }
                }
                if (attributeBuffer.Load(ByteAddressMask((_246 + 361740u) >> 2u, 4u) * 4 + 0) == 0u)
                {
                    gl_Position.x = _135 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _315 = ByteAddressMask((_110 + 192u) >> 2u, 4u);
                uint2 _321 = uint2(graphValuesBuffer.Load(_315 * 4 + 0), graphValuesBuffer.Load((_315 + 1u) * 4 + 0));
                uint _327 = ByteAddressMask((_110 + 204u) >> 2u, 4u);
                uint3 _337 = uint3(graphValuesBuffer.Load(_327 * 4 + 0), graphValuesBuffer.Load((_327 + 1u) * 4 + 0), graphValuesBuffer.Load((_327 + 2u) * 4 + 0));
                uint _339 = _337.y;
                uint _348 = ByteAddressMask((_246 + 361728u) >> 2u, 4u);
                uint3 _357 = uint3(attributeBuffer.Load(_348 * 4 + 0), attributeBuffer.Load((_348 + 1u) * 4 + 0), attributeBuffer.Load((_348 + 2u) * 4 + 0));
                uint _369 = ByteAddressMask((((_221 + (indirectBuffer.Load(_229 * 4 + 0) * 3u)) << 2u) + 562688u) >> 2u, 4u);
                uint3 _378 = uint3(attributeBuffer.Load(_369 * 4 + 0), attributeBuffer.Load((_369 + 1u) * 4 + 0), attributeBuffer.Load((_369 + 2u) * 4 + 0));
                COLOR.x = asfloat(_378.x);
                COLOR.y = asfloat(_378.y);
                COLOR.z = asfloat(_378.z);
                uint _391 = ByteAddressMask((_246 + 120576u) >> 2u, 4u);
                uint4 _403 = uint4(attributeBuffer.Load(_391 * 4 + 0), attributeBuffer.Load((_391 + 1u) * 4 + 0), attributeBuffer.Load((_391 + 2u) * 4 + 0), attributeBuffer.Load((_391 + 3u) * 4 + 0));
                uint _409 = _199 & 3u;
                float4 _416 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_278, asfloat(_195.z)), 0.0f);
                float _432 = ((_409 == 3u) ? _416.w : ((_409 == 2u) ? _416.z : ((_409 == 1u) ? _416.y : ((_409 == 0u) ? _416.x : 0.0f)))) * asfloat(_195.y);
                float _434 = mad(clamp(_261, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                uint _458 = ((_339 >> 16u) ^ _339) * 73244475u;
                uint _462 = (_458 ^ (_458 >> 16u)) * 73244475u;
                float _470 = asfloat(((_462 ^ (_462 >> 16u)) >> 9u) + 1065353216u) - 1.0f;
                float _474 = asfloat(_357.y);
                float _489 = asfloat(_357.x);
                float _498 = asfloat(_357.z);
                float _562 = (mad(_470, asfloat(_337.z), asfloat(_337.x)) * (mad(asfloat(asuint(unity_MatrixVP[2u].w)), mad(unity_ObjectToWorld[2u].z, _498, mad(unity_ObjectToWorld[0u].z, _489, _474 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z, mad(asfloat(asuint(unity_MatrixVP[0u].w)), mad(unity_ObjectToWorld[2u].x, _498, mad(unity_ObjectToWorld[0u].x, _489, _474 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x, (mad(unity_ObjectToWorld[2u].y, _498, mad(unity_ObjectToWorld[0u].y, _489, _474 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y) * asfloat(asuint(unity_MatrixVP[1u].w)))) + asfloat(asuint(unity_MatrixVP[3u].w)))) / (min(abs(asfloat(asuint(_ScreenParams.y)) * asfloat(asuint(glstate_matrix_projection[1u].y))), abs(asfloat(asuint(_ScreenParams.x)) * asfloat(asuint(glstate_matrix_projection[0u].x)))) * (_432 * 0.5f));
                uint _563 = _219 & 3u;
                float4 _566 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_292, asfloat(_215.z)), 0.0f);
                float _582 = ((_563 == 3u) ? _566.w : ((_563 == 2u) ? _566.z : ((_563 == 1u) ? _566.y : ((_563 == 0u) ? _566.x : 0.0f)))) * asfloat(_215.y);
                float _583 = mad(_470, 720.0f, -360.0f);
                float _599 = asfloat((((_403.x * 1664525u) + 1013904223u) >> 9u) + 1065353216u) - 1.0f;
                float _604 = _71(_94 & 1u);
                float _605 = _71(_94 & 2u);
                float _609 = _562 * _432;
                float _610 = mad(_582, _583, asfloat(_403.y)) * 0.01745329238474369049072265625f;
                float _612 = mad(_582, _583, asfloat(_403.z)) * 0.01745329238474369049072265625f;
                float _613 = mad(_582, _583, asfloat(_403.w)) * 0.01745329238474369049072265625f;
                float _614 = sin(_610);
                float _615 = cos(_610);
                float _616 = sin(_612);
                float _617 = cos(_612);
                float _618 = sin(_613);
                float _619 = cos(_613);
                float _620 = _619 * _617;
                float3 _643 = float3(_604 - 0.5f, mad(_605, 0.5f, -0.5f), 1.0f);
                float _644 = _79(float3(_609 * mad(_616 * _614, _618, _620), _609 * mad(_614 * _619, _616, -(_618 * _617)), _489), _643);
                float _646 = _79(float3(_609 * (_618 * _615), _609 * (_619 * _615), _474), _643);
                float _648 = _79(float3(_609 * mad(-_619, _616, _618 * (_614 * _617)), _609 * mad(_620, _614, _618 * _616), _498), _643);
                float _658 = mad(unity_ObjectToWorld[2u].x, _648, mad(unity_ObjectToWorld[0u].x, _644, _646 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _659 = mad(unity_ObjectToWorld[2u].y, _648, mad(unity_ObjectToWorld[0u].y, _644, _646 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _660 = mad(unity_ObjectToWorld[2u].z, _648, mad(unity_ObjectToWorld[0u].z, _644, _646 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _660, mad((unity_MatrixVP[0u].x), _658, _659 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _660, mad((unity_MatrixVP[0u].y), _658, _659 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _660, mad((unity_MatrixVP[0u].z), _658, _659 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _660, mad((unity_MatrixVP[0u].w), _658, _659 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _729 = 1.0f / _609;
                float3 _738 = float3(-(_729 * (_616 * _615)), -(_729 * (-_614)), -(_729 * (_617 * _615)));
                float _745 = _79(_738, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _753 = _79(_738, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _761 = _79(_738, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _762 = float3(_745, _753, _761);
                float _764 = rsqrt(_79(_762, _762));
                float _771 = ((_609 * (_609 * _609)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_764 * _745) * _771;
                TEXCOORD_6.y = (_764 * _753) * _771;
                TEXCOORD_6.z = (_764 * _761) * _771;
                float _783 = trunc(mad(_599, 4.0f, -frac(_599 * 4.0f))) * 0.5f;
                float _784 = frac(_783);
                TEXCOORD.x = (_604 + (_784 + _784)) * 0.5f;
                TEXCOORD.y = (mad(_605, 0.5f, -floor(_783)) + 1.0f) * 0.5f;
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_321.y) > 0.5f) ? (mad(-frac(_434 * 128.0f), 0.0078125f, _434) + 0.00390625f) : _434, asfloat(_321.x)), 0.0f).w;
                INDEX.x = 0u;
                INDEX.y = _109;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 spv_Color;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                spv_Color.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                spv_Color.x = _44.x * COLOR.x;
                spv_Color.y = _44.y * COLOR.y;
                spv_Color.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = spv_Color;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
    }
}
