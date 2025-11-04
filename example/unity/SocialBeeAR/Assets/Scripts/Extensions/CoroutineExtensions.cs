using System;
using System.Collections;
using UnityEngine;

namespace SocialBeeAR
{
    public static class CoroutineExtensions
    {
        public static Coroutine StartThrowingCoroutine(this MonoBehaviour monoBehaviour, IEnumerator enumerator, Action<Exception> onError)
        {
            return monoBehaviour.StartCoroutine(RunThrowingEnumerator(enumerator, onError));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        /// <remarks>
        /// Originally by a user named "Horace" and modified by Von for Social Bee.
        /// </remarks>
        public static IEnumerator RunThrowingEnumerator(IEnumerator enumerator, Action<Exception> onError)
        {
            var stack = new Stack();
            stack.Push(enumerator);

            while (stack.Count > 0)
            {
                var currentEnumerator = stack.Peek() as IEnumerator;
                object currentYieldedObject;
                try
                {
                    if (!currentEnumerator.MoveNext())
                    {
                        // Enumerator has finished
                        stack.Pop();
                        // Every enumerator is done.
                        continue;
                    }
                    currentYieldedObject = currentEnumerator.Current;
                }
                catch (Exception ex)
                {
                    // Our exception handling for Coroutines.
                    onError(ex);
                    yield break;
                }

                if (currentYieldedObject is IEnumerator current)
                    stack.Push(current);
                else
                    yield return currentYieldedObject;
            }
        }
    }

}
