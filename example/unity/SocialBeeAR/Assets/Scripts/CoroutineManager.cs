using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace SocialBeeAR
{
    
    public class CoroutineManager : MonoBehaviour
    {
        
        //Coroutine queue
        private Queue<IEnumerator> mCoroutineQueue = new Queue<IEnumerator>();
        
        //The current work
        private IEnumerator mCurrentWork = null;
     
        private static CoroutineManager instance;
        public static CoroutineManager Instance
        {
            get { return instance; }
        }
     
        void Awake()
        {
            instance = this;
        }
     
        public void EnqueueWork(IEnumerator work)
        {
            mCoroutineQueue.Enqueue(work);
        }
     
        void Update()
        {
            while (mCoroutineQueue.Count > 30)
                DoWork();
     
            //Execute 3 jobs if queue > 20
            if (mCoroutineQueue.Count > 20)
                DoWork();
     
            //Execute 2 jobs if queue > 10
            if (mCoroutineQueue.Count > 10)
                DoWork();
     
            DoWork();
        }
     
        void DoWork()
        {
            if (mCurrentWork == null && mCoroutineQueue.Count == 0)
                return;
     
            if (mCurrentWork == null)
            {
                mCurrentWork = mCoroutineQueue.Dequeue();
            }
     
            //check if this job is completed
            bool isJobCompleted = !mCurrentWork.MoveNext();
            if (isJobCompleted)
            {
                mCurrentWork = null;
            }
            
            //if there is another coroutine inside the coroutine
            else if (mCurrentWork.Current is IEnumerator)
            {
                mCurrentWork = (mCurrentWork.Current as IEnumerator);
            }
        }
        
    }
    
}

