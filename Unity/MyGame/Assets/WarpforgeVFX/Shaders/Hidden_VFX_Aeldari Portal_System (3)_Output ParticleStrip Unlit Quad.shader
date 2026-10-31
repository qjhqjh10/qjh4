// Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad.shader
// 名称 = `Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Aeldari Portal · System (3)（ParticleStrip））
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/1e59d1729d67847d186ed2dfccb91988.shader`
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
//   **未改**：Shader 名 · SubShader Tags · Pass 名/Tags · Blend/ZWrite/Cull/ZClip · 顶点/片元算式。
//
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`d9ff19e6ac6ccf6459aa15abf82f8871`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad" {
    SubShader {
        Tags {
            "IGNOREPROJECTOR"="true"
            "QUEUE"="Transparent+0"
            "RenderType"="Transparent"
        }
        Pass {
            Name "UniversalForwardOnly"
            Blend SrcAlpha One, SrcAlpha One
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
            ByteAddressBuffer stripDataBuffer : register(t2);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[3] : packoffset(c22);
                float4 unity_OrthoParams : packoffset(c25);
                float4 _Globals_Unmapped_at_416[40u] : packoffset(c26);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
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

            Texture2D<float4> bakedTexture : register(t3);
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

            float _73(float3 _71, float3 _72)
            {
                float _77 = _71.x * _72.x;
                return mad(_71.z, _72.z, mad(_71.y, _72.y, _77));
            }

            float _87(uint _86)
            {
                return float(_86);
            }

            float _95(float2 _93, float2 _94)
            {
                float _99 = _93.x * _94.x;
                return mad(_93.y, _94.y, _99);
            }

            void vert_main()
            {
                uint _106 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _115 = (_106 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _121 = asuint(_InstanceActiveIndex);
                uint _122 = _121 * 48u;
                if (_115 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_122 >> 2u, 4u) * 4 + 0) - 100u))
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
                uint _163 = _115 / 19u;
                uint _167 = asuint(_InstanceIndex);
                uint _175 = ((_163 + (_167 * 100u)) * 5u) + asuint(instancingConstants.z);
                uint _178 = _175 + 1u;
                uint _183 = (_163 * 4294967277u) + _115;
                if ((_183 + 1u) >= stripDataBuffer.Load<uint>(_178 * 4 + 0))
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
                uint _205 = ByteAddressMask((_122 + 16u) >> 2u, 4u);
                uint4 _218 = uint4(graphValuesBuffer.Load<uint>(_205 * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 2u) * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 3u) * 4 + 0));
                uint _222 = _218.w;
                uint _229 = _183 + (_106 & 1u);
                uint _230 = stripDataBuffer.Load<uint>(_175 * 4 + 0) + _229;
                uint _233 = _163 * 20u;
                uint _234 = (_230 % 20u) + _233;
                uint _235 = _167 * 24576u;
                uint _260 = ByteAddressMask((((_235 + (_234 * 3u)) << 2u) + 73728u) >> 2u, 4u);
                float3 _271 = asfloat(uint3(attributeBuffer.Load<uint>(_260 * 4 + 0), attributeBuffer.Load<uint>((_260 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_260 + 2u) * 4 + 0)));
                COLOR.x = _271.x;
                COLOR.y = _271.y;
                COLOR.z = _271.z;
                uint _281 = ByteAddressMask((((_235 + (_234 << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float4 _294 = asfloat(uint4(attributeBuffer.Load<uint>(_281 * 4 + 0), attributeBuffer.Load<uint>((_281 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_281 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_281 + 3u) * 4 + 0)));
                float _295 = _294.x;
                float _296 = _294.y;
                float _297 = _294.z;
                uint _309 = ByteAddressMask((((_235 + ((_233 + ((_230 - 1u) % 20u)) << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float3 _319 = asfloat(uint3(attributeBuffer.Load<uint>(_309 * 4 + 0), attributeBuffer.Load<uint>((_309 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_309 + 2u) * 4 + 0)));
                float _324 = _297 - _319.z;
                float _325 = _295 - _319.x;
                float _326 = _296 - _319.y;
                float3 _327 = float3(_324, _325, _326);
                float _328 = _73(_327, _327);
                float _331 = rsqrt(_328);
                bool _335 = (_229 > 0u) && (_328 > 1.0000000133514319600180897396058e-10f);
                uint _336 = _229 + 1u;
                uint _347 = ByteAddressMask((((_235 + ((_233 + ((stripDataBuffer.Load<uint>(_175 * 4 + 0) + _336) % 20u)) << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float3 _357 = asfloat(uint3(attributeBuffer.Load<uint>(_347 * 4 + 0), attributeBuffer.Load<uint>((_347 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_347 + 2u) * 4 + 0)));
                float _362 = _357.z - _297;
                float _363 = _357.x - _295;
                float _364 = _357.y - _296;
                float3 _365 = float3(_362, _363, _364);
                float _366 = _73(_365, _365);
                float _368 = rsqrt(_366);
                bool _372 = (_336 < stripDataBuffer.Load<uint>(_178 * 4 + 0)) && (_366 > 1.0000000133514319600180897396058e-10f);
                float _373 = _372 ? (_368 * _362) : 0.0f;
                float _374 = _372 ? (_368 * _363) : 0.0f;
                float _375 = _372 ? (_368 * _364) : 0.0f;
                float _377 = _335 ? ((_331 * _324) + _373) : _373;
                float _379 = _335 ? ((_331 * _325) + _374) : _374;
                float _381 = _335 ? ((_331 * _326) + _375) : _375;
                float3 _382 = float3(_377, _379, _381);
                float _386 = rsqrt(max(_73(_382, _382), 1.1754943508222875079687365372222e-38f));
                float _387 = _386 * _377;
                float _388 = _386 * _379;
                float _389 = _386 * _381;
                bool _395 = asfloat(asuint(unity_OrthoParams.w)) == 0.0f;
                float _400 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _419 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _433 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _453 = asfloat(asuint(unity_MatrixV[1u].z));
                float _461 = asfloat(asuint(unity_MatrixV[0u].z));
                float _469 = asfloat(asuint(unity_MatrixV[2u].z));
                float _493 = -mad(_469, unity_ObjectToWorld[2u].z, mad(_461, unity_ObjectToWorld[2u].x, _453 * unity_ObjectToWorld[2u].y));
                float _494 = -mad(_469, unity_ObjectToWorld[0u].z, mad(_461, unity_ObjectToWorld[0u].x, _453 * unity_ObjectToWorld[0u].y));
                float _495 = -mad(_469, unity_ObjectToWorld[1u].z, mad(_461, unity_ObjectToWorld[1u].x, _453 * unity_ObjectToWorld[1u].y));
                float3 _496 = float3(_493, _494, _495);
                float _498 = rsqrt(_73(_496, _496));
                float _502 = _395 ? (_297 - (mad(unity_WorldToObject[2u].z, _433, mad(unity_WorldToObject[0u].z, _419, _400 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z)) : (_498 * _493);
                float _503 = _395 ? (_295 - (mad(unity_WorldToObject[2u].x, _433, mad(unity_WorldToObject[0u].x, _419, _400 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x)) : (_498 * _494);
                float _504 = _395 ? (_296 - (mad(unity_WorldToObject[2u].y, _433, mad(unity_WorldToObject[0u].y, _419, _400 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y)) : (_498 * _495);
                float _511 = mad(_504, _387, -(_389 * _502));
                float _512 = mad(_502, _388, -(_387 * _503));
                float _513 = mad(_503, _389, -(_388 * _504));
                float3 _514 = float3(_511, _512, _513);
                float _515 = _73(_514, _514);
                bool _516 = _515 < 9.9999997473787516355514526367188e-06f;
                float _518 = rsqrt(_515);
                float _519 = _518 * _511;
                float _521 = _518 * _513;
                float _522 = _516 ? 1.0f : (_518 * _512);
                float _529 = -(_387 * _522);
                float _530 = -(_516 ? 0.0f : (_388 * _521));
                float2 _538 = spvUnpackHalf2x16(_218.x);
                float _542 = mad(_294.w / asfloat(attributeBuffer.Load<uint>(ByteAddressMask((((_234 + _235) << 2u) + 65536u) >> 2u, 4u) * 4 + 0)), _538.x, _538.y);
                float _553 = 0.0f;
                switch (_222 >> 2u)
                {
                    case 1u:
                    {
                        _553 = mad(frac(min(_542, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _553 = mad(frac(max(_542, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _553 = mad(clamp(_542, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _553 = _542;
                        break;
                    }
                }
                uint _554 = _222 & 3u;
                float4 _561 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_553, asfloat(_218.z)), 0.0f);
                float _575 = (_554 == 3u) ? _561.w : ((_554 == 2u) ? _561.z : ((_554 == 1u) ? _561.y : ((_554 == 0u) ? _561.x : 0.0f)));
                float _576 = asfloat(_218.y);
                TEXCOORD.x = _87(_229) / _87(stripDataBuffer.Load<uint>(_178 * 4 + 0) - 1u);
                float _583 = _87(_106 & 2u);
                float _591 = mad(_576, _575, ((_575 * _576) < 0.0f) ? (-9.9999997473787516355514526367188e-06f) : 9.9999997473787516355514526367188e-06f);
                float2 _598 = float2(mad(_583, 0.5f, -0.5f), 1.0f);
                float _599 = _95(float2(_516 ? 0.0f : (_591 * _519), _295), _598);
                float _601 = _95(float2(_591 * _522, _296), _598);
                float _603 = _95(float2(_516 ? 0.0f : (_591 * _521), _297), _598);
                float _633 = mad(unity_ObjectToWorld[2u].x, _603, mad(unity_ObjectToWorld[0u].x, _599, _601 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _634 = mad(unity_ObjectToWorld[2u].y, _603, mad(unity_ObjectToWorld[0u].y, _599, _601 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _635 = mad(unity_ObjectToWorld[2u].z, _603, mad(unity_ObjectToWorld[0u].z, _599, _601 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _635, mad((unity_MatrixVP[0u].x), _633, _634 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _635, mad((unity_MatrixVP[0u].y), _633, _634 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _635, mad((unity_MatrixVP[0u].z), _633, _634 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _635, mad((unity_MatrixVP[0u].w), _633, _634 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _704 = 1.0f / _591;
                float3 _711 = float3(-(_704 * (_516 ? _529 : mad(_389, _521, _529))), -(_704 * (_516 ? _530 : mad(_387, _519, _530))), -(_704 * mad(_388, _522, -(_516 ? 0.0f : (_389 * _519)))));
                float _718 = _73(_711, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _725 = _73(_711, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _732 = _73(_711, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _733 = float3(_718, _725, _732);
                float _735 = rsqrt(_73(_733, _733));
                float _742 = ((_591 * (_591 * _591)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_735 * _718) * _742;
                TEXCOORD_6.y = (_735 * _725) * _742;
                TEXCOORD_6.z = (_735 * _732) * _742;
                COLOR.w = 1.0f;
                TEXCOORD.y = _583 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _121;
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
            static float4 SV_Target;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                SV_Target.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                SV_Target.x = _44.x * COLOR.x;
                SV_Target.y = _44.y * COLOR.y;
                SV_Target.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = SV_Target;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
        Pass {
            Blend SrcAlpha One, SrcAlpha One
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
            ByteAddressBuffer stripDataBuffer : register(t2);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[3] : packoffset(c22);
                float4 unity_OrthoParams : packoffset(c25);
                float4 _Globals_Unmapped_at_416[40u] : packoffset(c26);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
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

            Texture2D<float4> bakedTexture : register(t3);
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

            float _73(float3 _71, float3 _72)
            {
                float _77 = _71.x * _72.x;
                return mad(_71.z, _72.z, mad(_71.y, _72.y, _77));
            }

            float _87(uint _86)
            {
                return float(_86);
            }

            float _95(float2 _93, float2 _94)
            {
                float _99 = _93.x * _94.x;
                return mad(_93.y, _94.y, _99);
            }

            void vert_main()
            {
                uint _106 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _115 = (_106 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _121 = asuint(_InstanceActiveIndex);
                uint _122 = _121 * 48u;
                if (_115 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_122 >> 2u, 4u) * 4 + 0) - 100u))
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
                uint _163 = _115 / 19u;
                uint _167 = asuint(_InstanceIndex);
                uint _175 = ((_163 + (_167 * 100u)) * 5u) + asuint(instancingConstants.z);
                uint _178 = _175 + 1u;
                uint _183 = (_163 * 4294967277u) + _115;
                if ((_183 + 1u) >= stripDataBuffer.Load<uint>(_178 * 4 + 0))
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
                uint _205 = ByteAddressMask((_122 + 16u) >> 2u, 4u);
                uint4 _218 = uint4(graphValuesBuffer.Load<uint>(_205 * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 2u) * 4 + 0), graphValuesBuffer.Load<uint>((_205 + 3u) * 4 + 0));
                uint _222 = _218.w;
                uint _229 = _183 + (_106 & 1u);
                uint _230 = stripDataBuffer.Load<uint>(_175 * 4 + 0) + _229;
                uint _233 = _163 * 20u;
                uint _234 = (_230 % 20u) + _233;
                uint _235 = _167 * 24576u;
                uint _260 = ByteAddressMask((((_235 + (_234 * 3u)) << 2u) + 73728u) >> 2u, 4u);
                float3 _271 = asfloat(uint3(attributeBuffer.Load<uint>(_260 * 4 + 0), attributeBuffer.Load<uint>((_260 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_260 + 2u) * 4 + 0)));
                COLOR.x = _271.x;
                COLOR.y = _271.y;
                COLOR.z = _271.z;
                uint _281 = ByteAddressMask((((_235 + (_234 << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float4 _294 = asfloat(uint4(attributeBuffer.Load<uint>(_281 * 4 + 0), attributeBuffer.Load<uint>((_281 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_281 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_281 + 3u) * 4 + 0)));
                float _295 = _294.x;
                float _296 = _294.y;
                float _297 = _294.z;
                uint _309 = ByteAddressMask((((_235 + ((_233 + ((_230 - 1u) % 20u)) << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float3 _319 = asfloat(uint3(attributeBuffer.Load<uint>(_309 * 4 + 0), attributeBuffer.Load<uint>((_309 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_309 + 2u) * 4 + 0)));
                float _324 = _297 - _319.z;
                float _325 = _295 - _319.x;
                float _326 = _296 - _319.y;
                float3 _327 = float3(_324, _325, _326);
                float _328 = _73(_327, _327);
                float _331 = rsqrt(_328);
                bool _335 = (_229 > 0u) && (_328 > 1.0000000133514319600180897396058e-10f);
                uint _336 = _229 + 1u;
                uint _347 = ByteAddressMask((((_235 + ((_233 + ((stripDataBuffer.Load<uint>(_175 * 4 + 0) + _336) % 20u)) << 2u)) << 2u) + 32768u) >> 2u, 4u);
                float3 _357 = asfloat(uint3(attributeBuffer.Load<uint>(_347 * 4 + 0), attributeBuffer.Load<uint>((_347 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_347 + 2u) * 4 + 0)));
                float _362 = _357.z - _297;
                float _363 = _357.x - _295;
                float _364 = _357.y - _296;
                float3 _365 = float3(_362, _363, _364);
                float _366 = _73(_365, _365);
                float _368 = rsqrt(_366);
                bool _372 = (_336 < stripDataBuffer.Load<uint>(_178 * 4 + 0)) && (_366 > 1.0000000133514319600180897396058e-10f);
                float _373 = _372 ? (_368 * _362) : 0.0f;
                float _374 = _372 ? (_368 * _363) : 0.0f;
                float _375 = _372 ? (_368 * _364) : 0.0f;
                float _377 = _335 ? ((_331 * _324) + _373) : _373;
                float _379 = _335 ? ((_331 * _325) + _374) : _374;
                float _381 = _335 ? ((_331 * _326) + _375) : _375;
                float3 _382 = float3(_377, _379, _381);
                float _386 = rsqrt(max(_73(_382, _382), 1.1754943508222875079687365372222e-38f));
                float _387 = _386 * _377;
                float _388 = _386 * _379;
                float _389 = _386 * _381;
                bool _395 = asfloat(asuint(unity_OrthoParams.w)) == 0.0f;
                float _400 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _419 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _433 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _453 = asfloat(asuint(unity_MatrixV[1u].z));
                float _461 = asfloat(asuint(unity_MatrixV[0u].z));
                float _469 = asfloat(asuint(unity_MatrixV[2u].z));
                float _493 = -mad(_469, unity_ObjectToWorld[2u].z, mad(_461, unity_ObjectToWorld[2u].x, _453 * unity_ObjectToWorld[2u].y));
                float _494 = -mad(_469, unity_ObjectToWorld[0u].z, mad(_461, unity_ObjectToWorld[0u].x, _453 * unity_ObjectToWorld[0u].y));
                float _495 = -mad(_469, unity_ObjectToWorld[1u].z, mad(_461, unity_ObjectToWorld[1u].x, _453 * unity_ObjectToWorld[1u].y));
                float3 _496 = float3(_493, _494, _495);
                float _498 = rsqrt(_73(_496, _496));
                float _502 = _395 ? (_297 - (mad(unity_WorldToObject[2u].z, _433, mad(unity_WorldToObject[0u].z, _419, _400 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z)) : (_498 * _493);
                float _503 = _395 ? (_295 - (mad(unity_WorldToObject[2u].x, _433, mad(unity_WorldToObject[0u].x, _419, _400 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x)) : (_498 * _494);
                float _504 = _395 ? (_296 - (mad(unity_WorldToObject[2u].y, _433, mad(unity_WorldToObject[0u].y, _419, _400 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y)) : (_498 * _495);
                float _511 = mad(_504, _387, -(_389 * _502));
                float _512 = mad(_502, _388, -(_387 * _503));
                float _513 = mad(_503, _389, -(_388 * _504));
                float3 _514 = float3(_511, _512, _513);
                float _515 = _73(_514, _514);
                bool _516 = _515 < 9.9999997473787516355514526367188e-06f;
                float _518 = rsqrt(_515);
                float _519 = _518 * _511;
                float _521 = _518 * _513;
                float _522 = _516 ? 1.0f : (_518 * _512);
                float _529 = -(_387 * _522);
                float _530 = -(_516 ? 0.0f : (_388 * _521));
                float2 _538 = spvUnpackHalf2x16(_218.x);
                float _542 = mad(_294.w / asfloat(attributeBuffer.Load<uint>(ByteAddressMask((((_234 + _235) << 2u) + 65536u) >> 2u, 4u) * 4 + 0)), _538.x, _538.y);
                float _553 = 0.0f;
                switch (_222 >> 2u)
                {
                    case 1u:
                    {
                        _553 = mad(frac(min(_542, 1.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 2u:
                    {
                        _553 = mad(frac(max(_542, 0.0f)), 0.9921875f, 0.00390625f);
                        break;
                    }
                    case 3u:
                    {
                        _553 = mad(clamp(_542, 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                        break;
                    }
                    default:
                    {
                        _553 = _542;
                        break;
                    }
                }
                uint _554 = _222 & 3u;
                float4 _561 = bakedTexture.SampleLevel(samplerbakedTexture, float2(_553, asfloat(_218.z)), 0.0f);
                float _575 = (_554 == 3u) ? _561.w : ((_554 == 2u) ? _561.z : ((_554 == 1u) ? _561.y : ((_554 == 0u) ? _561.x : 0.0f)));
                float _576 = asfloat(_218.y);
                TEXCOORD.x = _87(_229) / _87(stripDataBuffer.Load<uint>(_178 * 4 + 0) - 1u);
                float _583 = _87(_106 & 2u);
                float _591 = mad(_576, _575, ((_575 * _576) < 0.0f) ? (-9.9999997473787516355514526367188e-06f) : 9.9999997473787516355514526367188e-06f);
                float2 _598 = float2(mad(_583, 0.5f, -0.5f), 1.0f);
                float _599 = _95(float2(_516 ? 0.0f : (_591 * _519), _295), _598);
                float _601 = _95(float2(_591 * _522, _296), _598);
                float _603 = _95(float2(_516 ? 0.0f : (_591 * _521), _297), _598);
                float _633 = mad(unity_ObjectToWorld[2u].x, _603, mad(unity_ObjectToWorld[0u].x, _599, _601 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _634 = mad(unity_ObjectToWorld[2u].y, _603, mad(unity_ObjectToWorld[0u].y, _599, _601 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _635 = mad(unity_ObjectToWorld[2u].z, _603, mad(unity_ObjectToWorld[0u].z, _599, _601 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _635, mad((unity_MatrixVP[0u].x), _633, _634 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _635, mad((unity_MatrixVP[0u].y), _633, _634 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _635, mad((unity_MatrixVP[0u].z), _633, _634 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _635, mad((unity_MatrixVP[0u].w), _633, _634 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _704 = 1.0f / _591;
                float3 _711 = float3(-(_704 * (_516 ? _529 : mad(_389, _521, _529))), -(_704 * (_516 ? _530 : mad(_387, _519, _530))), -(_704 * mad(_388, _522, -(_516 ? 0.0f : (_389 * _519)))));
                float _718 = _73(_711, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _725 = _73(_711, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _732 = _73(_711, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _733 = float3(_718, _725, _732);
                float _735 = rsqrt(_73(_733, _733));
                float _742 = ((_591 * (_591 * _591)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_735 * _718) * _742;
                TEXCOORD_6.y = (_735 * _725) * _742;
                TEXCOORD_6.z = (_735 * _732) * _742;
                COLOR.w = 1.0f;
                TEXCOORD.y = _583 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _121;
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
            static float4 SV_Target;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                SV_Target.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                SV_Target.x = _44.x * COLOR.x;
                SV_Target.y = _44.y * COLOR.y;
                SV_Target.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = SV_Target;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
    }
}
