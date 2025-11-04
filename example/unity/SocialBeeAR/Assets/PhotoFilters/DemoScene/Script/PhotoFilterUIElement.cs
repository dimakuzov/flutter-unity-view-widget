using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoFilters
{
    public class PhotoFilterUIElement : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Text lable;
        [SerializeField] Image background;

        private PhotoFilterMainUI mainUI;
        public Material material { get; private set; }

        public void Initiliaze(string name, Material material, PhotoFilterMainUI mainUI)
        {
            this.material = material;
            image.material = material;
            lable.text = name;
            this.mainUI = mainUI;
        }

        public void SetActiveBackground(bool isActive)
        {
            background.gameObject.SetActive(isActive);
        }

        public void Press()
        {
            mainUI.SelectElement(this);
        }
    }
}
