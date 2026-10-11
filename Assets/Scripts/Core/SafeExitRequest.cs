using System;

namespace Emberfall
{
    /// <summary>One confirmation owns one preservation transaction. A rejected leave
    /// keeps the dialog and successful preparation available for a safe retry.</summary>
    public sealed class SafeExitRequest
    {
        public bool Open { get; private set; }
        public bool ToTitle { get; private set; }
        public bool Busy { get; private set; }
        public bool Prepared { get; private set; }

        public void Request(bool toTitle)
        {
            if (Open || Busy) return;
            ToTitle = toTitle;
            Prepared = false;
            Open = true;
        }

        public void Cancel()
        {
            if (Busy) return;
            Open = false;
            Prepared = false;
        }

        public bool Confirm(Func<bool> preserve, Func<bool, bool> leave)
        {
            if (!Open || Busy) return false;
            if (preserve == null) throw new ArgumentNullException("preserve");
            if (leave == null) throw new ArgumentNullException("leave");
            Busy = true;
            try
            {
                if (!Prepared)
                {
                    if (!preserve()) return false;
                    Prepared = true;
                }
                if (!leave(ToTitle)) return false;
                Open = false;
                Prepared = false;
                return true;
            }
            finally { Busy = false; }
        }
    }
}
