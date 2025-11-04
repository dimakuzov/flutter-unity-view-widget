using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoFilters
{
    public class PhotoFilterMainUI : MonoBehaviour
    {
        [SerializeField] GameObject photoFilterElement;
        [SerializeField] Transform elementsParent;
        [SerializeField] Material[] materials;

        private List<PhotoFilterUIElement> elements = new List<PhotoFilterUIElement>();
        private bool isComprasionEnabled;

        private void Awake()
        {
            float offset = 0;
            float elementWidth = 0;
            foreach (var material in materials)
            {
                PhotoFilterUIElement element = Instantiate(photoFilterElement, elementsParent, false).GetComponent<PhotoFilterUIElement>();
                element.transform.localPosition += new Vector3(offset, 0, 0);
                offset += element.GetComponent<RectTransform>().sizeDelta.x;
                elementWidth = element.GetComponent<RectTransform>().sizeDelta.x;
                element.Initiliaze(material.name, material, this);
                elements.Add(element);
            }

            offset -= GetComponent<CanvasScaler>().referenceResolution.x;
            elementsParent.GetComponent<UIContentClamper>().Initialize(elementWidth, offset);
            isComprasionEnabled = false;
        }

        public void SelectElement(PhotoFilterUIElement selectedElement)
        {
            foreach (var element in elements)
            {
                element.SetActiveBackground(false);
            }
            selectedElement.SetActiveBackground(true);

            PostEffectController.Instance.SetMaterial(selectedElement.material);
        }

        public void DeselectAll()
        {
            PostEffectController.Instance.ShowOriginal(true);
            foreach (var element in elements)
            {
                element.SetActiveBackground(false);
            }
        }

        public void ShowComprasion(Text text)
        {
            isComprasionEnabled = !isComprasionEnabled;
            text.text = isComprasionEnabled ? "Hide\nComprasion" : "Show\nComprasion".ToString();
            PostEffectController.Instance.ShowComprasion(isComprasionEnabled);
        }

        public void TakeScreenshot()
        {
            PostEffectController.Instance.TakeScreenshot();
        }
    }
}
