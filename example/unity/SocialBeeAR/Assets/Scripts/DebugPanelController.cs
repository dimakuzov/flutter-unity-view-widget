using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SocialBeeAR
{
    /// <summary>
    /// For printing debug message on screen.
    /// </summary>
    public class DebugPanelController : MonoBehaviour
    {
        
        private Queue<string> messageQueue = new Queue<string>();
        private Text debugConsoleText;
        private string finalMessage;
        private int counter = 0;
        
        [SerializeField] private  int rowNumber = 1; //by default the raw number is 1
        [SerializeField] private int maxRowCharactorNum = 110; //the max char each row can print 

        
        private void Awake()
        {
            this.debugConsoleText = gameObject.GetComponent<Text>();
        }


        public void PushMessage(string text)
        {
            List<string> strList = Utilities.CutOffStringToList(text, maxRowCharactorNum);
            for (int i = 0; i < strList.Count; i++)
            {
                DoPushMessage(strList[i]);
            }
        }


        private void DoPushMessage(string rowText)
        {
            if (rowText != null)
            {
                this.messageQueue.Enqueue("[" + ++counter + "] " + rowText);
                if (this.messageQueue.Count > this.rowNumber)
                {
                    this.messageQueue.Dequeue();
                }

                this.PrintMessage();
            }
        }
        

        private void PrintMessage()
        {
            this.finalMessage = "";
            int index = 0;
            foreach (string message in this.messageQueue)
            {
                this.finalMessage += (index == 0 ? "" : "\n") + message;
                index++;
            }
            this.debugConsoleText.text = this.finalMessage;
        }
        
        
        public void Clear()
        {
            this.messageQueue.Clear();
        }

    }

}






