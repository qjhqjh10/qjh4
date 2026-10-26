// InitializableBehaviour.cs — 可被场景初始化机调度的件（原版 `InitializableBehaviour` 的等价基类）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `Shell/SceneInitializer.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`；根因与先例见 `Shell/WindowTabBase.cs` 文件头（A1506）。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里）。同名一族（`IBaseInitializable` /
//    `IInitializable` / `IInitializableAsync` / `IInitializationComplete` /
//    `IBeforeInitializationHandler` / `InitializationPriority` / `InitializationStatus`）
//    与初始化机 `SceneInitializer` 仍在 `SceneInitializer.cs`（同一 `CardPresentation` 命名空间）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `InitializableBehaviour` 的等价基类：`Start()` 里自己向场景登记。</summary>
    public abstract class InitializableBehaviour : MonoBehaviour, IInitializable, IInitializationComplete, IBeforeInitializationHandler
    {
        [SerializeField] InitializationPriority priority = InitializationPriority.Data;
        [SerializeField] InitializationStatus status = InitializationStatus.Pending;

        public InitializationPriority Priority { get { return priority; } }
        public InitializationStatus Status { get { return status; } set { status = value; } }

        public abstract void Initialize();

        void Start() { SceneInitializer.QueueForInitialization(this); }

        public virtual void OnInitializationComplete() { }
        public virtual void OnBeforeInitialization() { }
    }
}
