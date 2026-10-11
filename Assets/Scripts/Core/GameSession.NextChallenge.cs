using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        // Shared reward navigation lives in GameSession.Expedition. Keep the
        // desktop chapter aliases without defining a second copy of that API.
        public bool CanChallengeNextChapterTier {get{return ChapterActive&&CanChallengeNextTier;}}
        public bool ChallengeNextChapterTier(){return ChapterActive&&ChallengeNextTier();}
    }
}
