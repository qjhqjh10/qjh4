# gen_shader_defaults.py — 把**原版 shader 的属性默认值**（`m_PropInfo.m_Props[].m_DefValue`）导成两张表
#
# 为什么要它（W64 · 兜底审计 **B10**）：
#   `WarpforgeEffectBinder.ApplyDerivedParticleDefaults` 给「原版材质里没序列化」的那几个
#   粒子派生属性补值，注释写着「取值对齐 URP 的默认状态」—— 那是**我们挑的**，不是原版的。
#   原版每个 shader 的属性默认值就写在 bundle 的 Shader 资产里
#   （`m_ParsedForm.m_PropInfo.m_Props[].m_DefValue[0..3]`），读得到就不该用替代值。
#   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` B10。
#
# 产物（两份）：
#   ① `数据/游戏数据/original_shader_props.tsv` —— **全量**：`shader \t prop \t type \t def0..def3`
#      （type: 0=Color/Vector · 1=Float/Range/Int · 2=… · 4=Texture —— 直接抄 Unity 的 `m_Type`）
#      这份是**人读的判据表**，运行时不用它。
#   ② `MyGame/Assets/WarpforgeVFX/Runtime/OriginalShaderDefaults.cs` —— **运行时子集**：
#      只收 `ApplyDerivedParticleDefaults` 会碰的那些属性名（见 `RUNTIME_PROPS`）。
#      ⚠️ 子集里**只出现「这个 shader 自己声明过」的属性** —— 查不到 = 原版没有这一格，
#         调用方**不许**拿它当「原版默认 0」（那会把「没有」当成「值为 0」）。
#
# 幂等：输出按 (shader, prop) 排序、数字用 `repr(float)` 的稳定十进制，**两次跑逐字节相同**。
#       `--check` 只重算并和盘上两份比对，**不写盘**；不一致打印差异条数并非 0 退出（A1648 同族口径）。
#
# 用法：
#   "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_shader_defaults.py"
#   "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_shader_defaults.py" --check
import io
import os
import sys

import UnityPy

CHECK = "--check" in sys.argv[1:]

AA = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64"
BUNDLES = [
    # 与 `工具/gen_shader_blend.py` 同一份包清单（那边的注释解释了为什么必须是这几个：
    # 只读一个包会得到「命中 0 个」的假结论）。
    os.path.join(AA, "shaders_assets_all.bundle"),
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_builtin.bundle",
    os.path.join(AA, "battleprefabs_vfxandmisc_assets_all.bundle"),
    os.path.join(AA, "battlesharedresources_assets_all.bundle"),
    # 🆕（W64 B11 同批）：TMP 的 4 个变体在字体包里。
    os.path.join(AA, "fonts_assets_all.bundle"),
]

OUT_TSV = r"D:\4\Unity\数据\游戏数据\original_shader_props.tsv"
OUT_CS = r"D:\4\Unity\MyGame\Assets\WarpforgeVFX\Runtime\OriginalShaderDefaults.cs"

# 运行时真正会查的属性名（= `WarpforgeEffectBinder.ApplyDerivedParticleDefaults` 那 10 个，
#  + 它点名要对齐的 URP 别名的**原版名** `_ColorAddSubDiff`
#  + 原版自己那个软粒子开关 `_SOFTPARTICLES`，用来给「原版有没有软粒子」留证）。
RUNTIME_PROPS = [
    "_SoftParticlesEnabled",
    "_SoftParticleFadeParams",
    "_SoftParticlesNearFadeDistance",
    "_SoftParticlesFarFadeDistance",
    "_CameraFadingEnabled",
    "_CameraFadeParams",
    "_CameraNearFadeDistance",
    "_CameraFarFadeDistance",
    "_ColorMode",
    "_BaseColorAddSubDiff",
    "_ColorAddSubDiff",
    "_SOFTPARTICLES",
]
RUNTIME_SET = set(RUNTIME_PROPS)


def fmt(v):
    """稳定十进制：`0` · `1` · `0.1` · `-0.55`（避免 0.10000000149 这种）。"""
    f = float(v)
    if f == int(f) and abs(f) < 1e15:
        return str(int(f))
    s = ("%.6f" % f).rstrip("0").rstrip(".")
    return s if s not in ("", "-0") else "0"


def collect():
    """返回 {shader_name: {prop_name: (type, (d0,d1,d2,d3))}}。后读到的包不覆盖先读到的同名 shader。"""
    out = {}
    missing = []
    for p in BUNDLES:
        if not os.path.exists(p):
            missing.append(p)
            continue
        env = UnityPy.load(p)
        for obj in env.objects:
            if obj.type.name != "Shader":
                continue
            d = obj.read()
            pf = getattr(d, "m_ParsedForm", None)
            if pf is None:
                continue
            name = getattr(pf, "m_Name", None) or getattr(d, "m_Name", "")
            if not name or name in out:
                continue
            props = {}
            for pr in (pf.m_PropInfo.m_Props or []):
                n = pr.m_Name
                if not n:
                    continue
                vals = tuple(getattr(pr, "m_DefValue_%d_" % i, 0.0) for i in range(4))
                props[n] = (int(pr.m_Type), vals)
            out[name] = props
    if missing:
        print("[gen_shader_defaults] ⚠️ 缺包（这些包里的 shader 不会进表）：")
        for m in missing:
            print("   ", m)
    return out


def build_tsv(data):
    lines = ["shader\tprop\ttype\tdef0\tdef1\tdef2\tdef3"]
    for shader in sorted(data):
        props = data[shader]
        for prop in sorted(props):
            t, v = props[prop]
            lines.append("\t".join([shader, prop, str(t)] + [fmt(x) for x in v]))
    return "\n".join(lines) + "\n"


def build_cs(data):
    rows = []
    for shader in sorted(data):
        props = data[shader]
        for prop in sorted(props):
            if prop not in RUNTIME_SET:
                continue
            _, v = props[prop]
            rows.append((shader, prop, v))
    # 每行一条，人可 grep；数量很小（现读 ~60 条），不做压缩编码。
    body = []
    for shader, prop, v in rows:
        body.append('            new Row { shader = "%s", prop = "%s", x = %s, y = %s, z = %s, w = %s },'
                    % (shader.replace("\\", "\\\\").replace('"', '\\"'), prop,
                       fmt(v[0]), fmt(v[1]), fmt(v[2]), fmt(v[3])))
    return """// OriginalShaderDefaults.cs — **由 `工具/gen_shader_defaults.py` 生成，别手改**
//
// 它是什么：**原版 shader 自己声明的属性默认值**（bundle 里 `m_ParsedForm.m_PropInfo.m_Props[].m_DefValue`）。
//   只收 `WarpforgeEffectBinder.ApplyDerivedParticleDefaults` 会碰的那一组属性名（生成器里的 `RUNTIME_PROPS`）；
//   全量表在 `数据/游戏数据/original_shader_props.tsv`。
//
// 🔴 **为什么要有它（W64 · 兜底审计 B10）**：那几个「粒子派生属性」原来的值是**我们挑的**
//   （注释写着「对齐 URP 的默认状态」）—— 原版真值就在 Shader 资产里，读得到就不该用替代值。
//   ⚠️ **查不到 ≠ 默认 0**：表里没有 `(shader, prop)` = **原版这个 shader 根本没声明这一格**。
//      调用方必须把这种情况和「声明了、默认是 0」分开处理（见 `ApplyDerivedParticleDefaults` 的注释）。
//
// 现读：%d 条 `(shader, prop)` · 覆盖 %d 个 shader。
using UnityEngine;

namespace WarpforgeVFX
{
    public static class OriginalShaderDefaults
    {
        struct Row
        {
            public string shader;
            public string prop;
            public float x, y, z, w;
        }

        static readonly Row[] Rows =
        {
%s
        };

        static System.Collections.Generic.Dictionary<string, Row> _byKey;

        static System.Collections.Generic.Dictionary<string, Row> ByKey
        {
            get
            {
                if (_byKey == null)
                {
                    _byKey = new System.Collections.Generic.Dictionary<string, Row>(Rows.Length);
                    foreach (var r in Rows) _byKey[r.shader + "\\n" + r.prop] = r;
                }
                return _byKey;
            }
        }

        /// <summary>原版这个 shader **自己声明过**这个属性吗？声明过就给出它的默认值。</summary>
        public static bool TryGet(string shaderName, string prop, out Vector4 value)
        {
            value = Vector4.zero;
            if (string.IsNullOrEmpty(shaderName) || string.IsNullOrEmpty(prop)) return false;
            Row r;
            if (!ByKey.TryGetValue(shaderName + "\\n" + prop, out r)) return false;
            value = new Vector4(r.x, r.y, r.z, r.w);
            return true;
        }

        /// <summary>同上，取 `.x`（标量属性的默认值就在 x 里）。</summary>
        public static bool TryGetFloat(string shaderName, string prop, out float value)
        {
            Vector4 v;
            if (TryGet(shaderName, prop, out v)) { value = v.x; return true; }
            value = 0f;
            return false;
        }

        /// <summary>表里有多少条（自检用）。</summary>
        public static int Count { get { return Rows.Length; } }

        /// <summary>表里覆盖多少个 shader（自检用）。</summary>
        public static int ShaderCount
        {
            get
            {
                var s = new System.Collections.Generic.HashSet<string>();
                foreach (var r in Rows) s.Add(r.shader);
                return s.Count;
            }
        }
    }
}
""" % (len(rows), len({r[0] for r in rows}), "\n".join(body))


def write_or_check(path, text):
    raw = text.encode("utf-8")
    old = None
    if os.path.exists(path):
        old = io.open(path, "rb").read()
    if CHECK:
        if old == raw:
            print("[gen_shader_defaults] OK  %s" % path)
            return True
        print("[gen_shader_defaults] DIFF %s（盘上 %s B / 新算 %s B）"
              % (path, "-1" if old is None else len(old), len(raw)))
        return False
    if old != raw:
        d = os.path.dirname(path)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        io.open(path, "wb").write(raw)
        print("[gen_shader_defaults] 写 %s（%s B）" % (path, len(raw)))
    else:
        print("[gen_shader_defaults] 未变 %s（%s B）" % (path, len(raw)))
    return True


def main():
    data = collect()
    print("[gen_shader_defaults] 读到 %d 个 shader · 属性 %d 条"
          % (len(data), sum(len(v) for v in data.values())))
    ok = True
    ok &= write_or_check(OUT_TSV, build_tsv(data))
    ok &= write_or_check(OUT_CS, build_cs(data))
    if CHECK and not ok:
        sys.exit(1)


if __name__ == "__main__":
    main()
