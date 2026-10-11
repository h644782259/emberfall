using System;

namespace Emberfall
{
    public enum SaveFlowKind { None, ManualSave, LoadSave }
    public enum SaveLoadChoice { SaveAndLoad, DiscardAndLoad }

    /// <summary>Confirmation state bound to stable source/target IDs. Cancelling has
    /// no persistence callback; failed operations remain visible and retryable.</summary>
    public sealed class SafeSaveFlow
    {
        public SaveFlowKind Kind { get; private set; }
        public bool Open { get { return Kind != SaveFlowKind.None; } }
        public bool Busy { get; private set; }
        public bool SavedCurrent { get; private set; }
        public bool NeedsFreshConfirmation { get; private set; }
        public string SourceId { get; private set; }
        public string TargetId { get; private set; }
        public string Error { get; private set; }
        public void RequestManual(string sourceId) { Request(SaveFlowKind.ManualSave, sourceId, sourceId); }
        public void RequestLoad(string sourceId, string targetId) { Request(SaveFlowKind.LoadSave, sourceId, targetId); }
        private void Request(SaveFlowKind kind, string sourceId, string targetId)
        {
            if (Open || Busy || string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId)) return;
            Kind = kind; SourceId = sourceId; TargetId = targetId; SavedCurrent = false; NeedsFreshConfirmation = false; Error = null;
        }
        public void Cancel()
        {
            if (Busy) return;
            Kind = SaveFlowKind.None; SavedCurrent = false; NeedsFreshConfirmation = false; Error = null;
        }
        private bool Matches(string source, string target)
        {
            if (NeedsFreshConfirmation) return false;
            if (SourceId == source && TargetId == target) return true;
            NeedsFreshConfirmation = true;
            Error = "当前角色或读取目标已改变，请取消后重新确认。";
            return false;
        }
        public bool ConfirmManual(string currentId, Func<bool> save)
        {
            if (!Open || Busy || Kind != SaveFlowKind.ManualSave || !Matches(currentId, currentId)) return false;
            Busy = true;
            try
            {
                if (save == null || !save()) return false;
                Kind = SaveFlowKind.None; Error = null; return true;
            }
            finally { Busy = false; }
        }
        public bool ConfirmLoad(string currentId, string selectedTargetId, SaveLoadChoice choice, Func<bool> save, Func<bool, bool> load)
        {
            if (!Open || Busy || Kind != SaveFlowKind.LoadSave || !Matches(currentId, selectedTargetId) ||
                !Enum.IsDefined(typeof(SaveLoadChoice), choice)) return false;
            Busy = true;
            try
            {
                if (choice == SaveLoadChoice.SaveAndLoad && !SavedCurrent)
                {
                    if (save == null || !save()) return false;
                    SavedCurrent = true;
                }
                if (load == null || !load(choice == SaveLoadChoice.DiscardAndLoad)) return false;
                Kind = SaveFlowKind.None; SavedCurrent = false; Error = null; return true;
            }
            finally { Busy = false; }
        }
    }

    /// <summary>Load validation is isolated from live events/profile. A corrupt or
    /// missing target cannot clear the current player, loot or autosave destination.</summary>
    public static class SaveSlotTransition
    {
        public static bool TryStage(ProgressionService current, string targetId, out ProgressionService candidate, out string error)
        {
            candidate = null; error = null;
            if (current == null) { error = "当前存档服务不可用。"; return false; }
            if(current.IsPracticeOnly){error="试招角色不能切换持久存档。";return false;}
            var staged = new ProgressionService(current.SaveDirectory);
            if (!staged.LoadSlot(targetId)) { error = staged.LastError; return false; }
            current.CarryPendingChestContextTo(staged);
            candidate = staged;
            return true;
        }
    }
}
