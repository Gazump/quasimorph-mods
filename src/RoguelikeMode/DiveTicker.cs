using UnityEngine;

namespace RoguelikeMode
{
    public class DiveTicker : MonoBehaviour
    {
        private LadderStatus _last = LadderStatus.Idle;

        private void Update()
        {
            if (LadderClient.BoardStatus == _last)
            {
                return;
            }
            _last = LadderClient.BoardStatus;
            DiveScreen.NotifyLadderChanged();
        }
    }
}
