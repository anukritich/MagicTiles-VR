using UnityEngine;

public class Tile : MonoBehaviour
{
    public TileManager tileManager;

    private bool playerIsOnTile = false;
    private float cooldownTime = 0.5f;
    private float lastTriggerTime;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tileManager.playerTag) && !playerIsOnTile)
        {
            playerIsOnTile = true;

            // Avoid double triggering
            if (Time.time - lastTriggerTime < cooldownTime)
                return;

            lastTriggerTime = Time.time;

            // Notify the tile manager
            tileManager.HandlePlayerStep(this.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tileManager.playerTag))
        {
            playerIsOnTile = false;
        }
    }
}