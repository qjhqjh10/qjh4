// Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad.shader
// 名称 = `Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Gargoyle Swarm · 输出材质（G4 新拷））
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad.shader`
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
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`a54152a8ad9d9714fa8a5cc861406b54`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad" {
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
            #pragma multi_compile_local _ INSTANCING_ON

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            #if defined(INSTANCING_ON)
            // Stage: Vertex  Blob: 3  ParamBlob: 1  Language: hlsl
            // Keywords: INSTANCING_ON
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            
            struct unity_Builtins0Array
            {
                column_major float4x4 unity_Builtins0Array_unity_ObjectToWorldArray;
                column_major float4x4 unity_Builtins0Array_unity_WorldToObjectArray;
            };
            
            struct PerInstanceArray
            {
                float PerInstanceArray_InstanceIndex;
                float PerInstanceArray_InstanceActiveIndex;
            };
            
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };
            
            cbuffer type_UnityInstancing_PerDraw0 : register(b1)
            {
                unity_Builtins0Array unity_Builtins0Array[2] : packoffset(c0);
                float4 UnityInstancing_PerDraw0_Unmapped[4080u] : packoffset(c16);
            };
            
            cbuffer type_outputParamsConst : register(b2)
            {
                float4 instancingConstants : packoffset(c0);
            };
            
            cbuffer type_UnityInstancing_PerInstance : register(b3)
            {
                PerInstanceArray PerInstanceArray[2] : packoffset(c0);
                float4 UnityInstancing_PerInstance_Unmapped[4094u] : packoffset(c2);
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
            
            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }
            
            float _72(float3 _70, float3 _71)
            {
                float _76 = _70.x * _71.x;
                return mad(_70.z, _71.z, mad(_70.y, _71.y, _76));
            }
            
            float _86(uint _85)
            {
                return float(_85);
            }
            
            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _113 = min((_103 / 50000u), (asuint(instancingConstants.x) - 1u));
                uint _116 = (_113 * 4294917296u) + _103;
                uint _121 = _113 + asuint(instancingConstants.w);
                uint _127 = (_121 * 4u) + 1u;
                uint _130 = asuint(PerInstanceArray[_121].PerInstanceArray_InstanceActiveIndex);
                uint _131 = _130 * 192u;
                uint _151 = _121 * 4u;
                uint _154 = asuint(PerInstanceArray[_121].PerInstanceArray_InstanceIndex);
                bool _160 = _116 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_131 >> 2u, 4u) * 4 + 0) - deadList.Load<uint>(_154 * 4 + 0));
                if (_160)
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
                if (_116 >= indirectBuffer.Load<uint>(_130 * 4 + 0))
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
                uint _205 = ((_130 * 50000u) + asuint(instancingConstants.z)) + _116;
                uint _210 = _154 * 550528u;
                uint _213 = ((indirectBuffer.Load<uint>(_205 * 4 + 0) << 2u) + _210) << 2u;
                if (attributeBuffer.Load<uint>(ByteAddressMask((_213 + 400396u) >> 2u, 4u) * 4 + 0) == 0u)
                {
                    gl_Position.x = _160 ? asfloat(0xffffffffu /* nan */) : 0.0f;
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
                uint _243 = ByteAddressMask((_131 + 144u) >> 2u, 4u);
                uint3 _253 = uint3(graphValuesBuffer.Load<uint>(_243 * 4 + 0), graphValuesBuffer.Load<uint>((_243 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_243 + 2u) * 4 + 0));
                uint _260 = ByteAddressMask((_131 + 160u) >> 2u, 4u);
                uint3 _269 = uint3(graphValuesBuffer.Load<uint>(_260 * 4 + 0), graphValuesBuffer.Load<uint>((_260 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_260 + 2u) * 4 + 0));
                uint _275 = (_210 + indirectBuffer.Load<uint>(_205 * 4 + 0)) << 2u;
                uint _289 = ByteAddressMask((_275 + 200192u) >> 2u, 4u);
                uint _296 = ByteAddressMask((_213 + 400384u) >> 2u, 4u);
                uint3 _305 = uint3(attributeBuffer.Load<uint>(_296 * 4 + 0), attributeBuffer.Load<uint>((_296 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_296 + 2u) * 4 + 0));
                uint _320 = _121 << 3u;
                float _330 = (unity_MatrixV[1u].x);
                float _331 = (unity_MatrixV[1u].y);
                float _332 = (unity_MatrixV[1u].z);
                uint _337 = (_320 * 4u) + 1u;
                float _350 = (unity_MatrixV[0u].x);
                float _351 = (unity_MatrixV[0u].y);
                float _352 = (unity_MatrixV[0u].z);
                uint _356 = _320 * 4u;
                float _369 = (unity_MatrixV[2u].x);
                float _370 = (unity_MatrixV[2u].y);
                float _371 = (unity_MatrixV[2u].z);
                uint _377 = (_320 * 4u) + 2u;
                float _380 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                float _381 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                uint _388 = (((16u * _320) + 16u) + 4u) >> 2u;
                uint _398 = (((16u * _320) + 16u) + 0u) >> 2u;
                uint _408 = (((16u * _320) + 16u) + 8u) >> 2u;
                float _411 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                float _412 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                uint _420 = (((16u * _320) + 32u) + 4u) >> 2u;
                uint _430 = (((16u * _320) + 32u) + 0u) >> 2u;
                uint _440 = (((16u * _320) + 32u) + 8u) >> 2u;
                float _443 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float _445 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float3 _446 = float3(_380, _411, _443);
                float _448 = rsqrt(_72(_446, _446));
                float3 _452 = float3(_381, _412, _445);
                float _454 = rsqrt(_72(_452, _452));
                float _458 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                float _459 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                float _460 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float3 _461 = float3(_458, _459, _460);
                float _463 = rsqrt(_72(_461, _461));
                float _472 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_275 + 1201152u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_275 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float4 _492 = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_253.y) > 0.5f) ? (mad(-frac(_472 * 128.0f), 0.0078125f, _472) + 0.00390625f) : _472, asfloat(_253.x)), 0.0f);
                float _499 = asfloat(_253.z);
                COLOR.x = asfloat(_269.x) * (_499 * _492.x);
                COLOR.y = asfloat(_269.y) * (_499 * _492.y);
                COLOR.z = asfloat(_269.z) * (_499 * _492.z);
                float _514 = _86(_94 & 1u);
                float _515 = _86(_94 & 2u);
                float _519 = asfloat(attributeBuffer.Load<uint>(_289 * 4 + 0));
                float3 _528 = float3(_514 - 0.5f, mad(_515, 0.5f, -0.5f), 1.0f);
                float _529 = _72(float3(_519 * (_448 * _380), _519 * (_454 * _381), asfloat(_305.x)), _528);
                float _532 = _72(float3(_519 * (_448 * _411), _519 * (_454 * _412), asfloat(_305.y)), _528);
                float _535 = _72(float3(_519 * (_448 * _443), _519 * (_454 * _445), asfloat(_305.z)), _528);
                float _566 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].x;
                float _567 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].y;
                float _568 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _568, mad((unity_MatrixVP[0u].x), _566, _567 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _568, mad((unity_MatrixVP[0u].y), _566, _567 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _568, mad((unity_MatrixVP[0u].z), _566, _567 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _568, mad((unity_MatrixVP[0u].w), _566, _567 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _637 = asfloat(attributeBuffer.Load<uint>(_289 * 4 + 0));
                float _638 = 1.0f / _637;
                float3 _645 = float3(-(_638 * (_463 * _458)), -(_638 * (_463 * _459)), -(_638 * (_463 * _460)));
                float _653 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].z));
                float _662 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].z));
                float _671 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].z));
                float3 _672 = float3(_653, _662, _671);
                float _674 = rsqrt(_72(_672, _672));
                float _681 = ((_637 * (_637 * _637)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_674 * _653) * _681;
                TEXCOORD_6.y = (_674 * _662) * _681;
                TEXCOORD_6.z = (_674 * _671) * _681;
                float _692 = trunc(floor(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_275 + 1401344u) >> 2u, 4u) * 4 + 0)))) * 0.5f;
                float _695 = frac(abs(_692));
                float _697 = (_692 >= 0.0f) ? _695 : (-_695);
                TEXCOORD.x = (_514 + (_697 + _697)) * 0.5f;
                TEXCOORD.y = (mad(_515, 0.5f, -floor(_692)) + 1.0f) * 0.5f;
                COLOR.w = _492.w;
                INDEX.y = _130;
                INDEX.x = _121;
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
            #else
            // Catch-all.
            // Stage: Vertex  Blob: 2  ParamBlob: 0  Language: hlsl
            // Keywords: <none> (default / catch-all variant)
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
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
            
            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }
            
            float _72(float3 _70, float3 _71)
            {
                float _76 = _70.x * _71.x;
                return mad(_70.z, _71.z, mad(_70.y, _71.y, _76));
            }
            
            float _86(uint _85)
            {
                return float(_85);
            }
            
            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _109 = asuint(_InstanceActiveIndex);
                uint _110 = _109 * 192u;
                uint _129 = asuint(_InstanceIndex);
                bool _135 = _103 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_110 >> 2u, 4u) * 4 + 0) - deadList.Load<uint>(_129 * 4 + 0));
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
                if (_103 >= indirectBuffer.Load<uint>(_109 * 4 + 0))
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
                uint _182 = _103 + ((_109 * 50000u) + asuint(instancingConstants.z));
                uint _187 = _129 * 550528u;
                uint _190 = ((indirectBuffer.Load<uint>(_182 * 4 + 0) << 2u) + _187) << 2u;
                if (attributeBuffer.Load<uint>(ByteAddressMask((_190 + 400396u) >> 2u, 4u) * 4 + 0) == 0u)
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
                uint _220 = ByteAddressMask((_110 + 144u) >> 2u, 4u);
                uint3 _230 = uint3(graphValuesBuffer.Load<uint>(_220 * 4 + 0), graphValuesBuffer.Load<uint>((_220 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_220 + 2u) * 4 + 0));
                uint _237 = ByteAddressMask((_110 + 160u) >> 2u, 4u);
                uint3 _246 = uint3(graphValuesBuffer.Load<uint>(_237 * 4 + 0), graphValuesBuffer.Load<uint>((_237 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_237 + 2u) * 4 + 0));
                uint _252 = (_187 + indirectBuffer.Load<uint>(_182 * 4 + 0)) << 2u;
                uint _266 = ByteAddressMask((_252 + 200192u) >> 2u, 4u);
                uint _273 = ByteAddressMask((_190 + 400384u) >> 2u, 4u);
                uint3 _282 = uint3(attributeBuffer.Load<uint>(_273 * 4 + 0), attributeBuffer.Load<uint>((_273 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_273 + 2u) * 4 + 0));
                float _306 = (unity_MatrixV[1u].x);
                float _307 = (unity_MatrixV[1u].y);
                float _308 = (unity_MatrixV[1u].z);
                float _321 = (unity_MatrixV[0u].x);
                float _322 = (unity_MatrixV[0u].y);
                float _323 = (unity_MatrixV[0u].z);
                float _336 = (unity_MatrixV[2u].x);
                float _337 = (unity_MatrixV[2u].y);
                float _338 = (unity_MatrixV[2u].z);
                float _341 = mad(_336, unity_ObjectToWorld[0u].z, mad(_321, unity_ObjectToWorld[0u].x, _306 * unity_ObjectToWorld[0u].y));
                float _342 = mad(_337, unity_ObjectToWorld[0u].z, mad(_322, unity_ObjectToWorld[0u].x, _307 * unity_ObjectToWorld[0u].y));
                float _358 = mad(_336, unity_ObjectToWorld[1u].z, mad(_321, unity_ObjectToWorld[1u].x, _306 * unity_ObjectToWorld[1u].y));
                float _359 = mad(_337, unity_ObjectToWorld[1u].z, mad(_322, unity_ObjectToWorld[1u].x, _307 * unity_ObjectToWorld[1u].y));
                float _376 = mad(_336, unity_ObjectToWorld[2u].z, mad(_321, unity_ObjectToWorld[2u].x, _306 * unity_ObjectToWorld[2u].y));
                float _378 = mad(_337, unity_ObjectToWorld[2u].z, mad(_322, unity_ObjectToWorld[2u].x, _307 * unity_ObjectToWorld[2u].y));
                float3 _379 = float3(_341, _358, _376);
                float _381 = rsqrt(_72(_379, _379));
                float3 _385 = float3(_342, _359, _378);
                float _387 = rsqrt(_72(_385, _385));
                float _391 = -mad(_338, unity_ObjectToWorld[0u].z, mad(_323, unity_ObjectToWorld[0u].x, _308 * unity_ObjectToWorld[0u].y));
                float _392 = -mad(_338, unity_ObjectToWorld[1u].z, mad(_323, unity_ObjectToWorld[1u].x, _308 * unity_ObjectToWorld[1u].y));
                float _393 = -mad(_338, unity_ObjectToWorld[2u].z, mad(_323, unity_ObjectToWorld[2u].x, _308 * unity_ObjectToWorld[2u].y));
                float3 _394 = float3(_391, _392, _393);
                float _396 = rsqrt(_72(_394, _394));
                float _405 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_252 + 1201152u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_252 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float4 _425 = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_230.y) > 0.5f) ? (mad(-frac(_405 * 128.0f), 0.0078125f, _405) + 0.00390625f) : _405, asfloat(_230.x)), 0.0f);
                float _432 = asfloat(_230.z);
                COLOR.x = asfloat(_246.x) * (_432 * _425.x);
                COLOR.y = asfloat(_246.y) * (_432 * _425.y);
                COLOR.z = asfloat(_246.z) * (_432 * _425.z);
                float _447 = _86(_94 & 1u);
                float _448 = _86(_94 & 2u);
                float _452 = asfloat(attributeBuffer.Load<uint>(_266 * 4 + 0));
                float3 _461 = float3(_447 - 0.5f, mad(_448, 0.5f, -0.5f), 1.0f);
                float _462 = _72(float3(_452 * (_381 * _341), _452 * (_387 * _342), asfloat(_282.x)), _461);
                float _465 = _72(float3(_452 * (_381 * _358), _452 * (_387 * _359), asfloat(_282.y)), _461);
                float _468 = _72(float3(_452 * (_381 * _376), _452 * (_387 * _378), asfloat(_282.z)), _461);
                float _498 = mad(unity_ObjectToWorld[2u].x, _468, mad(unity_ObjectToWorld[0u].x, _462, _465 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _499 = mad(unity_ObjectToWorld[2u].y, _468, mad(unity_ObjectToWorld[0u].y, _462, _465 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _500 = mad(unity_ObjectToWorld[2u].z, _468, mad(unity_ObjectToWorld[0u].z, _462, _465 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _500, mad((unity_MatrixVP[0u].x), _498, _499 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _500, mad((unity_MatrixVP[0u].y), _498, _499 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _500, mad((unity_MatrixVP[0u].z), _498, _499 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _500, mad((unity_MatrixVP[0u].w), _498, _499 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _569 = asfloat(attributeBuffer.Load<uint>(_266 * 4 + 0));
                float _570 = 1.0f / _569;
                float3 _577 = float3(-(_570 * (_396 * _391)), -(_570 * (_396 * _392)), -(_570 * (_396 * _393)));
                float _584 = _72(_577, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _591 = _72(_577, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _598 = _72(_577, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _599 = float3(_584, _591, _598);
                float _601 = rsqrt(_72(_599, _599));
                float _608 = ((_569 * (_569 * _569)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_601 * _584) * _608;
                TEXCOORD_6.y = (_601 * _591) * _608;
                TEXCOORD_6.z = (_601 * _598) * _608;
                float _619 = trunc(floor(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_252 + 1401344u) >> 2u, 4u) * 4 + 0)))) * 0.5f;
                float _622 = frac(abs(_619));
                float _624 = (_619 >= 0.0f) ? _622 : (-_622);
                TEXCOORD.x = (_447 + (_624 + _624)) * 0.5f;
                TEXCOORD.y = (mad(_448, 0.5f, -floor(_619)) + 1.0f) * 0.5f;
                COLOR.w = _425.w;
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
            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            #if defined(INSTANCING_ON)
            // Stage: Fragment  Blob: 6  ParamBlob: 4  Language: hlsl
            // Keywords: INSTANCING_ON
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
                nointerpolation float4 COLOR : TEXCOORD8;
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
            #else
            // Catch-all.
            // Stage: Fragment  Blob: 5  ParamBlob: 4  Language: hlsl
            // Keywords: <none> (default / catch-all variant)
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
                nointerpolation float4 COLOR : TEXCOORD8;
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
            #pragma multi_compile_local _ INSTANCING_ON

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            #if defined(INSTANCING_ON)
            // Stage: Vertex  Blob: 3  ParamBlob: 1  Language: hlsl
            // Keywords: INSTANCING_ON
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            
            struct unity_Builtins0Array
            {
                column_major float4x4 unity_Builtins0Array_unity_ObjectToWorldArray;
                column_major float4x4 unity_Builtins0Array_unity_WorldToObjectArray;
            };
            
            struct PerInstanceArray
            {
                float PerInstanceArray_InstanceIndex;
                float PerInstanceArray_InstanceActiveIndex;
            };
            
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };
            
            cbuffer type_UnityInstancing_PerDraw0 : register(b1)
            {
                unity_Builtins0Array unity_Builtins0Array[2] : packoffset(c0);
                float4 UnityInstancing_PerDraw0_Unmapped[4080u] : packoffset(c16);
            };
            
            cbuffer type_outputParamsConst : register(b2)
            {
                float4 instancingConstants : packoffset(c0);
            };
            
            cbuffer type_UnityInstancing_PerInstance : register(b3)
            {
                PerInstanceArray PerInstanceArray[2] : packoffset(c0);
                float4 UnityInstancing_PerInstance_Unmapped[4094u] : packoffset(c2);
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
            
            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }
            
            float _72(float3 _70, float3 _71)
            {
                float _76 = _70.x * _71.x;
                return mad(_70.z, _71.z, mad(_70.y, _71.y, _76));
            }
            
            float _86(uint _85)
            {
                return float(_85);
            }
            
            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _113 = min((_103 / 50000u), (asuint(instancingConstants.x) - 1u));
                uint _116 = (_113 * 4294917296u) + _103;
                uint _121 = _113 + asuint(instancingConstants.w);
                uint _127 = (_121 * 4u) + 1u;
                uint _130 = asuint(PerInstanceArray[_121].PerInstanceArray_InstanceActiveIndex);
                uint _131 = _130 * 192u;
                uint _151 = _121 * 4u;
                uint _154 = asuint(PerInstanceArray[_121].PerInstanceArray_InstanceIndex);
                bool _160 = _116 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_131 >> 2u, 4u) * 4 + 0) - deadList.Load<uint>(_154 * 4 + 0));
                if (_160)
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
                if (_116 >= indirectBuffer.Load<uint>(_130 * 4 + 0))
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
                uint _205 = ((_130 * 50000u) + asuint(instancingConstants.z)) + _116;
                uint _210 = _154 * 550528u;
                uint _213 = ((indirectBuffer.Load<uint>(_205 * 4 + 0) << 2u) + _210) << 2u;
                if (attributeBuffer.Load<uint>(ByteAddressMask((_213 + 400396u) >> 2u, 4u) * 4 + 0) == 0u)
                {
                    gl_Position.x = _160 ? asfloat(0xffffffffu /* nan */) : 0.0f;
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
                uint _243 = ByteAddressMask((_131 + 144u) >> 2u, 4u);
                uint3 _253 = uint3(graphValuesBuffer.Load<uint>(_243 * 4 + 0), graphValuesBuffer.Load<uint>((_243 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_243 + 2u) * 4 + 0));
                uint _260 = ByteAddressMask((_131 + 160u) >> 2u, 4u);
                uint3 _269 = uint3(graphValuesBuffer.Load<uint>(_260 * 4 + 0), graphValuesBuffer.Load<uint>((_260 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_260 + 2u) * 4 + 0));
                uint _275 = (_210 + indirectBuffer.Load<uint>(_205 * 4 + 0)) << 2u;
                uint _289 = ByteAddressMask((_275 + 200192u) >> 2u, 4u);
                uint _296 = ByteAddressMask((_213 + 400384u) >> 2u, 4u);
                uint3 _305 = uint3(attributeBuffer.Load<uint>(_296 * 4 + 0), attributeBuffer.Load<uint>((_296 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_296 + 2u) * 4 + 0));
                uint _320 = _121 << 3u;
                float _330 = (unity_MatrixV[1u].x);
                float _331 = (unity_MatrixV[1u].y);
                float _332 = (unity_MatrixV[1u].z);
                uint _337 = (_320 * 4u) + 1u;
                float _350 = (unity_MatrixV[0u].x);
                float _351 = (unity_MatrixV[0u].y);
                float _352 = (unity_MatrixV[0u].z);
                uint _356 = _320 * 4u;
                float _369 = (unity_MatrixV[2u].x);
                float _370 = (unity_MatrixV[2u].y);
                float _371 = (unity_MatrixV[2u].z);
                uint _377 = (_320 * 4u) + 2u;
                float _380 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                float _381 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                uint _388 = (((16u * _320) + 16u) + 4u) >> 2u;
                uint _398 = (((16u * _320) + 16u) + 0u) >> 2u;
                uint _408 = (((16u * _320) + 16u) + 8u) >> 2u;
                float _411 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                float _412 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                uint _420 = (((16u * _320) + 32u) + 4u) >> 2u;
                uint _430 = (((16u * _320) + 32u) + 0u) >> 2u;
                uint _440 = (((16u * _320) + 32u) + 8u) >> 2u;
                float _443 = mad(_369, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_350, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _330 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float _445 = mad(_370, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_351, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _331 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float3 _446 = float3(_380, _411, _443);
                float _448 = rsqrt(_72(_446, _446));
                float3 _452 = float3(_381, _412, _445);
                float _454 = rsqrt(_72(_452, _452));
                float _458 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y));
                float _459 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y));
                float _460 = -mad(_371, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, mad(_352, unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _332 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y));
                float3 _461 = float3(_458, _459, _460);
                float _463 = rsqrt(_72(_461, _461));
                float _472 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_275 + 1201152u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_275 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float4 _492 = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_253.y) > 0.5f) ? (mad(-frac(_472 * 128.0f), 0.0078125f, _472) + 0.00390625f) : _472, asfloat(_253.x)), 0.0f);
                float _499 = asfloat(_253.z);
                COLOR.x = asfloat(_269.x) * (_499 * _492.x);
                COLOR.y = asfloat(_269.y) * (_499 * _492.y);
                COLOR.z = asfloat(_269.z) * (_499 * _492.z);
                float _514 = _86(_94 & 1u);
                float _515 = _86(_94 & 2u);
                float _519 = asfloat(attributeBuffer.Load<uint>(_289 * 4 + 0));
                float3 _528 = float3(_514 - 0.5f, mad(_515, 0.5f, -0.5f), 1.0f);
                float _529 = _72(float3(_519 * (_448 * _380), _519 * (_454 * _381), asfloat(_305.x)), _528);
                float _532 = _72(float3(_519 * (_448 * _411), _519 * (_454 * _412), asfloat(_305.y)), _528);
                float _535 = _72(float3(_519 * (_448 * _443), _519 * (_454 * _445), asfloat(_305.z)), _528);
                float _566 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].x, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].x, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].x)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].x;
                float _567 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].y, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].y, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].y)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].y;
                float _568 = mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[2u].z, _535, mad(unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[0u].z, _529, _532 * unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[1u].z)) + unity_Builtins0Array[_121].unity_Builtins0Array_unity_ObjectToWorldArray[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _568, mad((unity_MatrixVP[0u].x), _566, _567 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _568, mad((unity_MatrixVP[0u].y), _566, _567 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _568, mad((unity_MatrixVP[0u].z), _566, _567 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _568, mad((unity_MatrixVP[0u].w), _566, _567 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _637 = asfloat(attributeBuffer.Load<uint>(_289 * 4 + 0));
                float _638 = 1.0f / _637;
                float3 _645 = float3(-(_638 * (_463 * _458)), -(_638 * (_463 * _459)), -(_638 * (_463 * _460)));
                float _653 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[0u].z));
                float _662 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[1u].z));
                float _671 = _72(_645, float3(unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].x, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].y, unity_Builtins0Array[_121].unity_Builtins0Array_unity_WorldToObjectArray[2u].z));
                float3 _672 = float3(_653, _662, _671);
                float _674 = rsqrt(_72(_672, _672));
                float _681 = ((_637 * (_637 * _637)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_674 * _653) * _681;
                TEXCOORD_6.y = (_674 * _662) * _681;
                TEXCOORD_6.z = (_674 * _671) * _681;
                float _692 = trunc(floor(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_275 + 1401344u) >> 2u, 4u) * 4 + 0)))) * 0.5f;
                float _695 = frac(abs(_692));
                float _697 = (_692 >= 0.0f) ? _695 : (-_695);
                TEXCOORD.x = (_514 + (_697 + _697)) * 0.5f;
                TEXCOORD.y = (mad(_515, 0.5f, -floor(_692)) + 1.0f) * 0.5f;
                COLOR.w = _492.w;
                INDEX.y = _130;
                INDEX.x = _121;
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
            #else
            // Catch-all.
            // Stage: Vertex  Blob: 2  ParamBlob: 0  Language: hlsl
            // Keywords: <none> (default / catch-all variant)
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            ByteAddressBuffer indirectBuffer : register(t2);
            ByteAddressBuffer deadList : register(t3);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
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
            
            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }
            
            float _72(float3 _70, float3 _71)
            {
                float _76 = _70.x * _71.x;
                return mad(_70.z, _71.z, mad(_70.y, _71.y, _76));
            }
            
            float _86(uint _85)
            {
                return float(_85);
            }
            
            void vert_main()
            {
                uint _94 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _103 = (_94 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _109 = asuint(_InstanceActiveIndex);
                uint _110 = _109 * 192u;
                uint _129 = asuint(_InstanceIndex);
                bool _135 = _103 >= (graphValuesBuffer.Load<uint>(ByteAddressMask(_110 >> 2u, 4u) * 4 + 0) - deadList.Load<uint>(_129 * 4 + 0));
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
                if (_103 >= indirectBuffer.Load<uint>(_109 * 4 + 0))
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
                uint _182 = _103 + ((_109 * 50000u) + asuint(instancingConstants.z));
                uint _187 = _129 * 550528u;
                uint _190 = ((indirectBuffer.Load<uint>(_182 * 4 + 0) << 2u) + _187) << 2u;
                if (attributeBuffer.Load<uint>(ByteAddressMask((_190 + 400396u) >> 2u, 4u) * 4 + 0) == 0u)
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
                uint _220 = ByteAddressMask((_110 + 144u) >> 2u, 4u);
                uint3 _230 = uint3(graphValuesBuffer.Load<uint>(_220 * 4 + 0), graphValuesBuffer.Load<uint>((_220 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_220 + 2u) * 4 + 0));
                uint _237 = ByteAddressMask((_110 + 160u) >> 2u, 4u);
                uint3 _246 = uint3(graphValuesBuffer.Load<uint>(_237 * 4 + 0), graphValuesBuffer.Load<uint>((_237 + 1u) * 4 + 0), graphValuesBuffer.Load<uint>((_237 + 2u) * 4 + 0));
                uint _252 = (_187 + indirectBuffer.Load<uint>(_182 * 4 + 0)) << 2u;
                uint _266 = ByteAddressMask((_252 + 200192u) >> 2u, 4u);
                uint _273 = ByteAddressMask((_190 + 400384u) >> 2u, 4u);
                uint3 _282 = uint3(attributeBuffer.Load<uint>(_273 * 4 + 0), attributeBuffer.Load<uint>((_273 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_273 + 2u) * 4 + 0));
                float _306 = (unity_MatrixV[1u].x);
                float _307 = (unity_MatrixV[1u].y);
                float _308 = (unity_MatrixV[1u].z);
                float _321 = (unity_MatrixV[0u].x);
                float _322 = (unity_MatrixV[0u].y);
                float _323 = (unity_MatrixV[0u].z);
                float _336 = (unity_MatrixV[2u].x);
                float _337 = (unity_MatrixV[2u].y);
                float _338 = (unity_MatrixV[2u].z);
                float _341 = mad(_336, unity_ObjectToWorld[0u].z, mad(_321, unity_ObjectToWorld[0u].x, _306 * unity_ObjectToWorld[0u].y));
                float _342 = mad(_337, unity_ObjectToWorld[0u].z, mad(_322, unity_ObjectToWorld[0u].x, _307 * unity_ObjectToWorld[0u].y));
                float _358 = mad(_336, unity_ObjectToWorld[1u].z, mad(_321, unity_ObjectToWorld[1u].x, _306 * unity_ObjectToWorld[1u].y));
                float _359 = mad(_337, unity_ObjectToWorld[1u].z, mad(_322, unity_ObjectToWorld[1u].x, _307 * unity_ObjectToWorld[1u].y));
                float _376 = mad(_336, unity_ObjectToWorld[2u].z, mad(_321, unity_ObjectToWorld[2u].x, _306 * unity_ObjectToWorld[2u].y));
                float _378 = mad(_337, unity_ObjectToWorld[2u].z, mad(_322, unity_ObjectToWorld[2u].x, _307 * unity_ObjectToWorld[2u].y));
                float3 _379 = float3(_341, _358, _376);
                float _381 = rsqrt(_72(_379, _379));
                float3 _385 = float3(_342, _359, _378);
                float _387 = rsqrt(_72(_385, _385));
                float _391 = -mad(_338, unity_ObjectToWorld[0u].z, mad(_323, unity_ObjectToWorld[0u].x, _308 * unity_ObjectToWorld[0u].y));
                float _392 = -mad(_338, unity_ObjectToWorld[1u].z, mad(_323, unity_ObjectToWorld[1u].x, _308 * unity_ObjectToWorld[1u].y));
                float _393 = -mad(_338, unity_ObjectToWorld[2u].z, mad(_323, unity_ObjectToWorld[2u].x, _308 * unity_ObjectToWorld[2u].y));
                float3 _394 = float3(_391, _392, _393);
                float _396 = rsqrt(_72(_394, _394));
                float _405 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_252 + 1201152u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_252 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float4 _425 = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_230.y) > 0.5f) ? (mad(-frac(_405 * 128.0f), 0.0078125f, _405) + 0.00390625f) : _405, asfloat(_230.x)), 0.0f);
                float _432 = asfloat(_230.z);
                COLOR.x = asfloat(_246.x) * (_432 * _425.x);
                COLOR.y = asfloat(_246.y) * (_432 * _425.y);
                COLOR.z = asfloat(_246.z) * (_432 * _425.z);
                float _447 = _86(_94 & 1u);
                float _448 = _86(_94 & 2u);
                float _452 = asfloat(attributeBuffer.Load<uint>(_266 * 4 + 0));
                float3 _461 = float3(_447 - 0.5f, mad(_448, 0.5f, -0.5f), 1.0f);
                float _462 = _72(float3(_452 * (_381 * _341), _452 * (_387 * _342), asfloat(_282.x)), _461);
                float _465 = _72(float3(_452 * (_381 * _358), _452 * (_387 * _359), asfloat(_282.y)), _461);
                float _468 = _72(float3(_452 * (_381 * _376), _452 * (_387 * _378), asfloat(_282.z)), _461);
                float _498 = mad(unity_ObjectToWorld[2u].x, _468, mad(unity_ObjectToWorld[0u].x, _462, _465 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _499 = mad(unity_ObjectToWorld[2u].y, _468, mad(unity_ObjectToWorld[0u].y, _462, _465 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _500 = mad(unity_ObjectToWorld[2u].z, _468, mad(unity_ObjectToWorld[0u].z, _462, _465 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _500, mad((unity_MatrixVP[0u].x), _498, _499 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _500, mad((unity_MatrixVP[0u].y), _498, _499 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _500, mad((unity_MatrixVP[0u].z), _498, _499 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _500, mad((unity_MatrixVP[0u].w), _498, _499 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _569 = asfloat(attributeBuffer.Load<uint>(_266 * 4 + 0));
                float _570 = 1.0f / _569;
                float3 _577 = float3(-(_570 * (_396 * _391)), -(_570 * (_396 * _392)), -(_570 * (_396 * _393)));
                float _584 = _72(_577, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _591 = _72(_577, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _598 = _72(_577, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _599 = float3(_584, _591, _598);
                float _601 = rsqrt(_72(_599, _599));
                float _608 = ((_569 * (_569 * _569)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_601 * _584) * _608;
                TEXCOORD_6.y = (_601 * _591) * _608;
                TEXCOORD_6.z = (_601 * _598) * _608;
                float _619 = trunc(floor(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_252 + 1401344u) >> 2u, 4u) * 4 + 0)))) * 0.5f;
                float _622 = frac(abs(_619));
                float _624 = (_619 >= 0.0f) ? _622 : (-_622);
                TEXCOORD.x = (_447 + (_624 + _624)) * 0.5f;
                TEXCOORD.y = (mad(_448, 0.5f, -floor(_619)) + 1.0f) * 0.5f;
                COLOR.w = _425.w;
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
            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            #if defined(INSTANCING_ON)
            // Stage: Fragment  Blob: 6  ParamBlob: 4  Language: hlsl
            // Keywords: INSTANCING_ON
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
                nointerpolation float4 COLOR : TEXCOORD8;
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
            #else
            // Catch-all.
            // Stage: Fragment  Blob: 5  ParamBlob: 4  Language: hlsl
            // Keywords: <none> (default / catch-all variant)
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
                nointerpolation float4 COLOR : TEXCOORD8;
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
            #endif

            ENDHLSL
        }
    }
}
