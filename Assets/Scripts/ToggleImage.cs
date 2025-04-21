using UnityEngine;
using UnityEngine.UI;

public class ToggleImage : MonoBehaviour
{
    public Image imageToToggle; // Reference to the Image component

    private bool isImageVisible = true; // Start with the image visible

    void Start()
    {
        // Set the image to be visible initially based on isImageVisible
        if (imageToToggle != null)
        {
            imageToToggle.gameObject.SetActive(isImageVisible);
        }
    }

    void Update()
    {
        // Check for the key press or button press
        if (Input.GetButtonDown("ToggleImageButton"))
        {
            ToggleImageVisibility();
        }
    }

    void ToggleImageVisibility()
    {
        if (imageToToggle != null)
        {
            isImageVisible = !isImageVisible; // Toggle the state
            imageToToggle.gameObject.SetActive(isImageVisible); // Set the visibility based on the state
        }
    }
}
