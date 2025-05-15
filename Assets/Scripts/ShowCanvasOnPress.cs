using UnityEngine;

public class ShowCanvasOnPress : MonoBehaviour
{
    public GameObject canvasToShow;

    public void ShowCanvas()
    {
        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
        }
    }
}
