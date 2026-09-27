// EndPanelProbe.cs — 结算面板的**看图探针**：把 0/1/2/3 个骷髅各渲一张，人眼核对「点亮的是哪几个」。
//
// 用法：… -executeMethod EndPanelProbe.Run
//
// 为什么要有它（2026-09-26）：用户说「点亮骷髅头的时候不是从左到右依次点亮」。
//   代码读起来是 `i < ShownSkulls`、`skull_0` 在最左（`BattleScene` 也补了三条断言），
//   但**断言只管行为、不管「看起来对不对」**（`CLAUDE.md` §三 + §10·6 那条教训：
//   卡面那三条错**同时通过了 588 条断言和四张截图**）。这种「像不像」的账只能并排看。
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EndPanelProbe
{
    const string P = "[EndProbe] ";
    const string ShotDir = "d:/4/_tmp_view/endprobe";

    public static void Run()
    {
        Directory.CreateDirectory(ShotDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);

        var host = new GameObject("Probe Root").transform;
        var panel = EndPanel.Create(host);

        // 敌方督军**降到过的最低生命** → 骷髅数（`DeckRules.SkullsFor`：≥20→1 · ≥10→2 · ≥0→3）
        var cases = new[]
        {
            new { MinHp = 30, Want = 0, Name = "00_零个.png" },
            new { MinHp = 15, Want = 1, Name = "01_一个.png" },
            new { MinHp = 5,  Want = 2, Name = "02_两个.png" },
            new { MinHp = 0,  Want = 3, Name = "03_三个.png" },
        };

        int bad = 0;
        foreach (var c in cases)
        {
            panel.Show(2, 0, c.MinHp, 7);        // winner=2 ⇒ 我方（index 0）胜
            var sk = panel.SkullQuads;
            string line = $"{P} 敌方最低生命 {c.MinHp,2} ⇒ 应亮 {c.Want} 个，实亮 {panel.ShownSkulls} 个｜";
            for (int i = 0; i < sk.Length; i++)
                line += $" skull{i}(x={sk[i].transform.position.x * 108f + 960f:F0}, act={sk[i].gameObject.activeSelf})";
            Debug.Log(line);
            if (panel.ShownSkulls != c.Want) { bad++; Debug.LogError(P + "  ✗ 数量不对"); }
            // 🔴 2026-09-27 加：**点亮的必须是「最左那 N 个」，其余 `activeSelf=false`（根本不显示）**
            //    —— 原版 `ShowRewards.c:58-68` 就是 `SetActive(true)` 前 N 个；**不是半透明**。
            for (int i = 0; i < sk.Length; i++)
                if (sk[i].gameObject.activeSelf != (i < c.Want))
                { bad++; Debug.LogError(P + $"  ✗ skull{i} 的显隐不对（应 {(i < c.Want ? "亮" : "灭")}）"); }
            // 🔴 以及 **0 个时整行（连底板）都藏**（`ShowRewards.c:55` `skullHolderObj.SetActive(0 < skullsObtained)`）
            bool rowOn = panel.SkullRow != null && panel.SkullRow.gameObject.activeSelf;
            if (rowOn != (c.Want > 0))
            { bad++; Debug.LogError(P + $"  ✗ 骷髅整行显隐不对（{c.Want} 个时应 {(c.Want > 0 ? "显示" : "整行藏")}）"); }

            Shot(c.Name);
        }

        // 再看一眼「投降」那种（不会有人掉血）—— 应该是 0 个
        panel.Show(2, 0, int.MaxValue, 1, 0);
        Debug.Log(P + $" 投降那一局 ⇒ {panel.ShownSkulls} 个骷髅");
        Shot("04_投降.png");

        Debug.Log($"{P} ===== 探针跑完（数量对不上的：{bad}）=====");
        if (Application.isBatchMode) EditorApplication.Exit(bad == 0 ? 0 : 1);
    }

    static void Shot(string file)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + "  截图 " + Path.Combine(ShotDir, file));
    }
}
