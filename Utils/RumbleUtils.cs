using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
using RumbleModdingAPI.RMAPI;
using UnityEngine;

namespace OuterWildsRumble.Components.Utils;

public class RumbleUtils
{
    public class ButtonWithLabel
    {
        public GameObject button;
        public GameObject label;

        public ButtonWithLabel(Vector3 localPosition, string labelText, string objectName, Transform parent = null)
        {
            // Create button at origin with identity rotation
            button = Create.NewButton(
                Vector3.zero, 
                Quaternion.identity
            );
    
            button.name = objectName;
            if (parent != null)
            {
                button.transform.SetParent(parent, false);
            }

            // Set local transforms
            button.transform.localPosition = localPosition;
            button.transform.localRotation = Quaternion.Euler(90, 180, 0);
    
            // Create label as child of button
            label = Create.NewText(labelText, 0.5f, Color.white, Vector3.zero, Quaternion.identity);
            label.name = objectName + " label";
            label.transform.SetParent(button.transform, false);
            label.transform.localPosition = new Vector3(0, 0.1f, 0f); // Position below button   0f, 0f, 0.12f
            label.transform.localRotation = Quaternion.Euler(90, 180, 0);
            
            TextMeshPro tmp = label.GetComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.enableWordWrapping = false;       //no line breaks
            tmp.overflowMode = TextOverflowModes.Overflow; //allows text to extend
        }

        public ButtonWithLabel(Vector3 localPosition, string labelText, string objectName, GameObject parent)
            : this(localPosition, labelText, objectName, parent.transform)
        {
        }

        public void ChangeLabelText(string newLabel)
        {
            label.GetComponent<TextMeshPro>().text = newLabel;
        }

        public void AddOnPressed(Action action)
        {
            button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(action);
        }

        public void Destroy()
        {
            GameObject.Destroy(button);
            GameObject.Destroy(label);
        }
    }
}