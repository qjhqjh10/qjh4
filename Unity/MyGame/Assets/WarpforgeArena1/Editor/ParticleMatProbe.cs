// ParticleMatProbe.cs — 判「`_EMISSION` 到底有没有开起来」。
//
// 起因（2026-09-24）：原版 `SmokeySteam01` 的 `m_ValidKeywords` 含 `_EMISSION`，
// 我们按同一份清单重建后，落盘的 `PS_SmokeySteam01.mat` 里**只有** `_FADING_ON/_FLIPBOOKBLENDING_ON/
// _SOFTPARTICLES_ON/_SURFACE_TYPE_TRANSPARENT` —— `_EMISSION` 不见了，画面也一点没变。
// 而 `ParticlesUnlit.shader` 里它是 `#pragma shader_feature_local_fragment _EMISSION`
// （**按 stage 作用域**的本地关键字）⇒ 怀疑 `Material.EnableKeyword(string)` 对它无效，
// 要走 `Material.SetKeyword(new LocalKeyword(shader, name), true)`。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod ParticleMatProbe.Run -logFile "d:/4/_tmp_view/pmp.log"
//   筛输出：grep "^PMP " d:/4/_tmp_view/pmp.log
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ParticleMatProbe
{
    const string P = "PMP ";

    public static void Run()
    {
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Debug.Log(P + "shader = " + (sh != null ? sh.name : "(null)"));
        if (sh == null) return;

        // ① 新建一份，两种 API 各试一次
        var m = new Material(sh) { name = "PMP_probe" };
        Debug.Log(P + "① 新建时 _EMISSION = " + m.IsKeywordEnabled("_EMISSION"));

        m.EnableKeyword("_EMISSION");
        Debug.Log(P + "② EnableKeyword(字符串) 之后 = " + m.IsKeywordEnabled("_EMISSION")
                    + " · enabledKeywords=" + Dump(m));

        var lk = new LocalKeyword(sh, "_EMISSION");
        Debug.Log(P + "③ LocalKeyword.isValid = " + lk.isValid + " · type = " + lk.type);
        m.SetKeyword(lk, true);
        Debug.Log(P + "    SetKeyword(true) 之后 = " + m.IsKeywordEnabled("_EMISSION")
                    + " · enabledKeywords=" + Dump(m));

        Object.DestroyImmediate(m);

        // ⑤ **开 → 存盘 → 重读** 往返（判「是没开上」还是「存盘丢了」）
        var m2 = new Material(sh) { name = "PMP_roundtrip" };
        m2.SetKeyword(new LocalKeyword(sh, "_EMISSION"), true);
        m2.SetKeyword(new LocalKeyword(sh, "_SOFTPARTICLES_ON"), true);
        m2.EnableKeyword("_FADING_ON");
        Debug.Log(P + "⑤ 存盘前  = " + Dump(m2));
        const string tmp = "Assets/WarpforgeArena1/arenas/battlearena1/Materials/PMP_roundtrip.mat";
        AssetDatabase.DeleteAsset(tmp);
        AssetDatabase.CreateAsset(m2, tmp);
        AssetDatabase.SaveAssets();
        var back = AssetDatabase.LoadAssetAtPath<Material>(tmp);
        Debug.Log(P + "   重读    = " + (back == null ? "(载入失败)" : Dump(back)));
        if (back != null)
            Debug.Log(P + "   重读 _EMISSION = " + back.IsKeywordEnabled("_EMISSION")
                        + " · _SOFTPARTICLES_ON = " + back.IsKeywordEnabled("_SOFTPARTICLES_ON")
                        + " · _FADING_ON = " + back.IsKeywordEnabled("_FADING_ON"));
        AssetDatabase.DeleteAsset(tmp);

        // ④ 读实际落盘的那份
        const string path = "Assets/WarpforgeArena1/arenas/battlearena1/Materials/PS_SmokeySteam01.mat";
        var real = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (real == null) { Debug.LogWarning(P + "④ 读不到 " + path); return; }
        Debug.Log(P + "④ 落盘那份：shader=" + real.shader.name
                    + " · IsKeywordEnabled(_EMISSION)=" + real.IsKeywordEnabled("_EMISSION")
                    + " · enabledKeywords=" + Dump(real));
        var rlk = new LocalKeyword(real.shader, "_EMISSION");
        Debug.Log(P + "   它的 LocalKeyword.isValid = " + rlk.isValid);
        Debug.Log(P + "   _EmissionColor = " + real.GetVector("_EmissionColor")
                    + " @emission 属性存在=" + real.HasProperty("_EmissionColor"));
    }

    static string Dump(Material m)
    {
        var sb = new StringBuilder("[");
        foreach (var k in m.enabledKeywords) sb.Append(k.name).Append(k.isValid ? "" : "(无效)").Append(",");
        foreach (var k in m.shaderKeywords) sb.Append(k).Append(",");
        return sb.Append("]").ToString();
    }
}
