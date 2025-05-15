using UnityEngine;


public class PowerUpTrigger : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Start()
    {
        gameObject.SetActive(true);
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if this object is being held and the trigger is the XR Origin collider
        if (grabInteractable.isSelected && other.CompareTag("MainCamera"))
        {
            Debug.Log("Power-up activated!");
            // Disable or destroy the power-up object
            gameObject.SetActive(false);
            // or: Destroy(gameObject);
        }
    }
}
