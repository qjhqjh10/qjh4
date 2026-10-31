// Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad.shader
// 名称 = `Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Aeldari Portal · System (5)）
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/1e59d1729d67847d186ed2dfccb91988_2.shader`
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
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`d38980a103a14c64d9dfe14e705e2187`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad" {
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
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[56u] : packoffset(c22);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
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

            uint spvBitfieldInsert(uint Base, uint Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint2 spvBitfieldInsert(uint2 Base, uint2 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint3 spvBitfieldInsert(uint3 Base, uint3 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint4 spvBitfieldInsert(uint4 Base, uint4 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                bool _116 = _91 >= graphValuesBuffer.Load<uint>(ByteAddressMask((_97 << 6u) >> 2u, 4u) * 4 + 0);
                if (_116)
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
                uint _142 = asuint(_InstanceIndex) * 13312u;
                uint _145 = ((_91 << 2u) + _142) << 2u;
                bool _153 = attributeBuffer.Load<uint>(ByteAddressMask((_145 + 16396u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
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
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
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
                uint _189 = ByteAddressMask(spvBitfieldInsert(48u, _97, 6u, 26u) >> 2u, 4u);
                uint2 _195 = uint2(graphValuesBuffer.Load<uint>(_189 * 4 + 0), graphValuesBuffer.Load<uint>((_189 + 1u) * 4 + 0));
                uint _214 = ByteAddressMask((_145 + 16384u) >> 2u, 4u);
                uint3 _224 = uint3(attributeBuffer.Load<uint>(_214 * 4 + 0), attributeBuffer.Load<uint>((_214 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_214 + 2u) * 4 + 0));
                uint _231 = ByteAddressMask(_145 >> 2u, 4u);
                uint3 _240 = uint3(attributeBuffer.Load<uint>(_231 * 4 + 0), attributeBuffer.Load<uint>((_231 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_231 + 2u) * 4 + 0));
                uint _247 = ByteAddressMask((_145 + 36864u) >> 2u, 4u);
                uint4 _260 = uint4(attributeBuffer.Load<uint>(_247 * 4 + 0), attributeBuffer.Load<uint>((_247 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_247 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_247 + 3u) * 4 + 0));
                uint _264 = _260.w;
                float _275 = asfloat(_240.x);
                float _276 = asfloat(_240.y);
                float _277 = asfloat(_240.z);
                float3 _278 = float3(_275, _276, _277);
                float _280 = rsqrt(_60(_278, _278));
                float _281 = _280 * _275;
                float _282 = _280 * _276;
                float _283 = _280 * _277;
                float _288 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _308 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _321 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _334 = asfloat(_224.x);
                float _335 = asfloat(_224.y);
                float _336 = asfloat(_224.z);
                float _337 = _335 - (mad(unity_WorldToObject[2u].y, _321, mad(unity_WorldToObject[0u].y, _308, _288 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y);
                float _338 = _336 - (mad(unity_WorldToObject[2u].z, _321, mad(unity_WorldToObject[0u].z, _308, _288 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z);
                float _339 = _334 - (mad(unity_WorldToObject[2u].x, _321, mad(unity_WorldToObject[0u].x, _308, _288 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x);
                float _346 = mad(_282, _338, -(_283 * _337));
                float _347 = mad(_283, _339, -(_281 * _338));
                float _348 = mad(_281, _337, -(_282 * _339));
                float3 _349 = float3(_346, _347, _348);
                float _350 = _60(_349, _349);
                bool _351 = _350 < 9.9999997473787516355514526367188e-06f;
                float _353 = rsqrt(_350);
                float _355 = _353 * _347;
                float _356 = _353 * _348;
                float _357 = _351 ? 1.0f : (_353 * _346);
                float _364 = -(_351 ? 0.0f : (_282 * _356));
                float _365 = -(_283 * _357);
                float _376 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((((_91 + _142) << 2u) + 32768u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(((_142 + spvBitfieldInsert(3u, _91, 2u, 30u)) << 2u) >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _405 = _74(_82 & 1u);
                float _406 = _74(_82 & 2u);
                float _411 = asfloat(_264);
                float _412 = _411 * 0.20000000298023223876953125f;
                float3 _423 = float3(_405 - 0.5f, mad(_406, 0.5f, -0.5f), 1.0f);
                float _424 = _60(float3(_412 * _357, _411 * _281, _334), _423);
                float _426 = _60(float3(_351 ? 0.0f : (_412 * _355), _411 * _282, _335), _423);
                float _428 = _60(float3(_351 ? 0.0f : (_412 * _356), _411 * _283, _336), _423);
                float _458 = mad(unity_ObjectToWorld[2u].x, _428, mad(unity_ObjectToWorld[0u].x, _424, _426 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _459 = mad(unity_ObjectToWorld[2u].y, _428, mad(unity_ObjectToWorld[0u].y, _424, _426 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _460 = mad(unity_ObjectToWorld[2u].z, _428, mad(unity_ObjectToWorld[0u].z, _424, _426 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _460, mad((unity_MatrixVP[0u].x), _458, _459 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _460, mad((unity_MatrixVP[0u].y), _458, _459 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _460, mad((unity_MatrixVP[0u].z), _458, _459 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _460, mad((unity_MatrixVP[0u].w), _458, _459 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _529 = asfloat(_264);
                float _530 = 1.0f / _529;
                float3 _537 = float3(-(_530 * (_351 ? _364 : mad(_283, _355, _364))), -(_530 * (_351 ? _365 : mad(_281, _356, _365))), -(_530 * mad(_357, _282, -(_351 ? 0.0f : (_281 * _355)))));
                float _544 = _60(_537, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _551 = _60(_537, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _558 = _60(_537, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _559 = float3(_544, _551, _558);
                float _561 = rsqrt(_60(_559, _559));
                float _568 = ((_529 * (_529 * _412)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_561 * _544) * _568;
                TEXCOORD_6.y = (_561 * _551) * _568;
                TEXCOORD_6.z = (_561 * _558) * _568;
                COLOR.x = asfloat(_260.x);
                COLOR.y = asfloat(_260.y);
                COLOR.z = asfloat(_260.z);
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_195.y) > 0.5f) ? (mad(-frac(_376 * 128.0f), 0.0078125f, _376) + 0.00390625f) : _376, asfloat(_195.x)), 0.0f).w;
                TEXCOORD.x = _405;
                TEXCOORD.y = _406 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
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
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[21u] : packoffset(c0);
                float3 _WorldSpaceCameraPos : packoffset(c21);
                float _Globals_Unmapped_at_348 : packoffset(c21.w);
                float4 _Globals_Unmapped_at_352[56u] : packoffset(c22);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
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

            uint spvBitfieldInsert(uint Base, uint Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint2 spvBitfieldInsert(uint2 Base, uint2 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint3 spvBitfieldInsert(uint3 Base, uint3 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint4 spvBitfieldInsert(uint4 Base, uint4 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                bool _116 = _91 >= graphValuesBuffer.Load<uint>(ByteAddressMask((_97 << 6u) >> 2u, 4u) * 4 + 0);
                if (_116)
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
                uint _142 = asuint(_InstanceIndex) * 13312u;
                uint _145 = ((_91 << 2u) + _142) << 2u;
                bool _153 = attributeBuffer.Load<uint>(ByteAddressMask((_145 + 16396u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
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
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
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
                uint _189 = ByteAddressMask(spvBitfieldInsert(48u, _97, 6u, 26u) >> 2u, 4u);
                uint2 _195 = uint2(graphValuesBuffer.Load<uint>(_189 * 4 + 0), graphValuesBuffer.Load<uint>((_189 + 1u) * 4 + 0));
                uint _214 = ByteAddressMask((_145 + 16384u) >> 2u, 4u);
                uint3 _224 = uint3(attributeBuffer.Load<uint>(_214 * 4 + 0), attributeBuffer.Load<uint>((_214 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_214 + 2u) * 4 + 0));
                uint _231 = ByteAddressMask(_145 >> 2u, 4u);
                uint3 _240 = uint3(attributeBuffer.Load<uint>(_231 * 4 + 0), attributeBuffer.Load<uint>((_231 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_231 + 2u) * 4 + 0));
                uint _247 = ByteAddressMask((_145 + 36864u) >> 2u, 4u);
                uint4 _260 = uint4(attributeBuffer.Load<uint>(_247 * 4 + 0), attributeBuffer.Load<uint>((_247 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_247 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_247 + 3u) * 4 + 0));
                uint _264 = _260.w;
                float _275 = asfloat(_240.x);
                float _276 = asfloat(_240.y);
                float _277 = asfloat(_240.z);
                float3 _278 = float3(_275, _276, _277);
                float _280 = rsqrt(_60(_278, _278));
                float _281 = _280 * _275;
                float _282 = _280 * _276;
                float _283 = _280 * _277;
                float _288 = asfloat(asuint(_WorldSpaceCameraPos.y));
                float _308 = asfloat(asuint(_WorldSpaceCameraPos.x));
                float _321 = asfloat(asuint(_WorldSpaceCameraPos.z));
                float _334 = asfloat(_224.x);
                float _335 = asfloat(_224.y);
                float _336 = asfloat(_224.z);
                float _337 = _335 - (mad(unity_WorldToObject[2u].y, _321, mad(unity_WorldToObject[0u].y, _308, _288 * unity_WorldToObject[1u].y)) + unity_WorldToObject[3u].y);
                float _338 = _336 - (mad(unity_WorldToObject[2u].z, _321, mad(unity_WorldToObject[0u].z, _308, _288 * unity_WorldToObject[1u].z)) + unity_WorldToObject[3u].z);
                float _339 = _334 - (mad(unity_WorldToObject[2u].x, _321, mad(unity_WorldToObject[0u].x, _308, _288 * unity_WorldToObject[1u].x)) + unity_WorldToObject[3u].x);
                float _346 = mad(_282, _338, -(_283 * _337));
                float _347 = mad(_283, _339, -(_281 * _338));
                float _348 = mad(_281, _337, -(_282 * _339));
                float3 _349 = float3(_346, _347, _348);
                float _350 = _60(_349, _349);
                bool _351 = _350 < 9.9999997473787516355514526367188e-06f;
                float _353 = rsqrt(_350);
                float _355 = _353 * _347;
                float _356 = _353 * _348;
                float _357 = _351 ? 1.0f : (_353 * _346);
                float _364 = -(_351 ? 0.0f : (_282 * _356));
                float _365 = -(_283 * _357);
                float _376 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((((_91 + _142) << 2u) + 32768u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(((_142 + spvBitfieldInsert(3u, _91, 2u, 30u)) << 2u) >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _405 = _74(_82 & 1u);
                float _406 = _74(_82 & 2u);
                float _411 = asfloat(_264);
                float _412 = _411 * 0.20000000298023223876953125f;
                float3 _423 = float3(_405 - 0.5f, mad(_406, 0.5f, -0.5f), 1.0f);
                float _424 = _60(float3(_412 * _357, _411 * _281, _334), _423);
                float _426 = _60(float3(_351 ? 0.0f : (_412 * _355), _411 * _282, _335), _423);
                float _428 = _60(float3(_351 ? 0.0f : (_412 * _356), _411 * _283, _336), _423);
                float _458 = mad(unity_ObjectToWorld[2u].x, _428, mad(unity_ObjectToWorld[0u].x, _424, _426 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _459 = mad(unity_ObjectToWorld[2u].y, _428, mad(unity_ObjectToWorld[0u].y, _424, _426 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _460 = mad(unity_ObjectToWorld[2u].z, _428, mad(unity_ObjectToWorld[0u].z, _424, _426 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _460, mad((unity_MatrixVP[0u].x), _458, _459 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _460, mad((unity_MatrixVP[0u].y), _458, _459 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _460, mad((unity_MatrixVP[0u].z), _458, _459 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _460, mad((unity_MatrixVP[0u].w), _458, _459 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _529 = asfloat(_264);
                float _530 = 1.0f / _529;
                float3 _537 = float3(-(_530 * (_351 ? _364 : mad(_283, _355, _364))), -(_530 * (_351 ? _365 : mad(_281, _356, _365))), -(_530 * mad(_357, _282, -(_351 ? 0.0f : (_281 * _355)))));
                float _544 = _60(_537, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _551 = _60(_537, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _558 = _60(_537, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _559 = float3(_544, _551, _558);
                float _561 = rsqrt(_60(_559, _559));
                float _568 = ((_529 * (_529 * _412)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_561 * _544) * _568;
                TEXCOORD_6.y = (_561 * _551) * _568;
                TEXCOORD_6.z = (_561 * _558) * _568;
                COLOR.x = asfloat(_260.x);
                COLOR.y = asfloat(_260.y);
                COLOR.z = asfloat(_260.z);
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_195.y) > 0.5f) ? (mad(-frac(_376 * 128.0f), 0.0078125f, _376) + 0.00390625f) : _376, asfloat(_195.x)), 0.0f).w;
                TEXCOORD.x = _405;
                TEXCOORD.y = _406 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
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

            ENDHLSL
        }
    }
}
