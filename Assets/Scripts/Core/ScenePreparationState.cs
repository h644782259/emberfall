using System;

namespace Emberfall
{
    // Progress represents completed preparation work, never elapsed time.
    public sealed class ScenePreparationState
    {
        public bool Active { get; private set; }
        public float Progress { get; private set; }
        public string Stage { get; private set; } = "";
        public string Error { get; private set; }
        public void Begin(string stage)
        { Active = true; Progress = 0; Stage = stage; Error = null; }
        public void Report(float progress, string stage)
        {
            if (!Active || Error != null) return;
            Progress = Math.Max(Progress, Math.Min(.99f, Math.Max(0, progress)));
            Stage = stage;
        }
        public void Complete()
        { if (Error != null) return; Progress = 1; Stage = "准备完成"; Active = false; }
        public void Fail(string error)
        { Error = error; Stage = "场景准备未完成"; Active = true; }
        public void Cancel()
        { Active = false; Progress = 0; Stage = ""; Error = null; }
    }
}
