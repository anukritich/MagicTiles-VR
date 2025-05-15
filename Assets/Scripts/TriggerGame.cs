using UnityEngine;

public class StartAreaTrigger : MonoBehaviour
{
    public TileManager tileManager;
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            tileManager.GenerateNewPattern();
        }
    }
}