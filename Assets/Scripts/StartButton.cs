using UnityEngine;

public class StartButton : MonoBehaviour
{
    public GameObject activationAreaPrefab;
    public Transform spawnPoint;
    private bool isActivated = false;

    public void OnButtonPoked()
    {
        if (isActivated) return;

        Instantiate(activationAreaPrefab, spawnPoint.position, Quaternion.identity);
        isActivated = true;
    }
}
