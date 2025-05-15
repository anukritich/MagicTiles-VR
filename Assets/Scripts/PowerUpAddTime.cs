using UnityEngine;

public class PowerUpAddTime : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    public GameUIManager gameManager;

    private void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        gameManager = FindObjectOfType<GameUIManager>();  // Get the GameManager instance
    }

    private void OnTriggerEnter(Collider other)
    {
        if (grabInteractable != null && grabInteractable.isSelected && other.CompareTag("MainCamera"))
        {
            Debug.Log("Power-Up triggered with Player!");

            if (gameManager != null)
            {
                gameManager.AddTime(15f); // Add 15 seconds to timer
                Debug.Log("15 seconds added to timer!");
            }
            else
            {
                Debug.LogWarning("GameManager not found!");
            }

            gameObject.SetActive(false); // Disable the power-up after use
        }
    }
}
