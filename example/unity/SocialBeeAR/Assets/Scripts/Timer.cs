using System;
using UnityEngine;

namespace SocialBeeAR
{
    public class Timer : BaseSingletonClass<Timer>
    {
        public DateTime? CountFrom;

        // Update is called once per frame
        void Update()
        {                    
            if (SBContextManager.Instance.context != null && SBContextManager.Instance.context.stats != null)
                PointsBarManager.Instance.SetTimeConsumed(SBContextManager.Instance.context.stats.GetFormattedFinalTotalTime());

            OnTimerTriggered();
        }

        public Action OnTimerTriggered;
    }

}
