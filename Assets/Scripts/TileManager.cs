using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class TileManager : MonoBehaviour
{
    public Transform specialParticleSpawnPoint;

    [Header("Tiles")]
    public List<GameObject> tiles;


    [Header("Tile Visuals")]
    public List<GameObject> tileVisuals; // Matches 1-to-1 with `tiles`
    public float dropHeight = 5f;
    public float dropSpeed = 5f;


    [Header("Particle Effects")]
    public ParticleSystem successParticlePrefab;
    public ParticleSystem failParticlePrefab;
    public ParticleSystem highlightParticlePrefab;
    public float particleYOffset = 0.2f;
    private List<ParticleSystem> activeParticles = new List<ParticleSystem>();
    public ParticleSystem specialsuccessParticlePrefab;


    [Header("Game Settings")]
    public float highlightTime = 1f;
    public float waitTimeBetweenTiles = 0.5f;
    public float waitTimeBeforePlayback = 1f;
    public int startingPatternLength = 3;
    public int maxPatternLength = 10;
    public bool allowDuplicatesInPattern = false;
    public string playerTag = "Player";

    [Header("Grab Interactable")]
    public GameObject grabInteractableObject;         // Reference to the interactable prefab or object
    public Transform grabSpawnPoint;                  // Where it should appear

    [Header("Difficulty Settings")]
    public float decreaseHighlightTimePerLevel = 0.1f;
    public float minHighlightTime = 0.3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip[] tileSounds;  // Optional: unique sound for each tile
    public AudioClip specialSuccessSound;

    [Header("Game Events")]
    public UnityEvent OnPatternStart;
    public UnityEvent OnPatternComplete;
    public UnityEvent OnPlayerSuccess;
    public UnityEvent OnPlayerFail;

    // Private variables
    private List<GameObject> currentPattern = new List<GameObject>();
    private int playerInputIndex = 0;
    private bool inputEnabled = false;
    private bool inputFailed = false;
    private bool patternStarted = false;
    private int currentLevel = 1;
    private float stepCooldown = 0.5f;
    private float lastStepTime = -1f;
    private ParticleSystem currentFailParticle;
    private int currentScore = 0;

    // Properties
    public int CurrentScore => currentScore;
    public int CurrentLevel => currentLevel;
    public int CurrentPatternLength => startingPatternLength + (currentLevel - 1);

    private void Start()
    {
        Debug.Log("TileManager Initialized.");

        // Verify all tiles have colliders
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Collider>() == null)
            {
                Debug.LogError($"Tile {tile.name} has no collider!");
            }
        }
    }

    public void StartGame()
    {
        ClearActiveParticles();
        currentLevel = 1;
        currentScore = 0;
        GenerateNewPattern();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (!patternStarted || inputFailed)
            {
                Debug.Log("Player entered start area. Starting new pattern...");
                GenerateNewPattern();
            }
            else if (!inputEnabled && !inputFailed && playerInputIndex >= currentPattern.Count)
            {
                Debug.Log("Player completed last pattern and re-entered start area. Proceeding to next level...");
                GenerateNewPattern();
            }
        }
    }

    public void GenerateNewPattern()
    {
        ClearActiveParticles();
        patternStarted = true;
        inputFailed = false;

        // Clean up any existing particles
        if (currentFailParticle != null)
        {
            Destroy(currentFailParticle.gameObject);
            currentFailParticle = null;
        }

        int patternLength = Mathf.Min(CurrentPatternLength, maxPatternLength);
        StartPattern(patternLength);

        // Invoke the event
        OnPatternStart?.Invoke();
    }

    public void StartPattern(int length)
    {
        currentPattern.Clear();
        Debug.Log($"Generating new pattern of length {length}");

        // First hide all visual tiles
        foreach (GameObject vis in tileVisuals)
        {
            vis.SetActive(false);
        }

        if (allowDuplicatesInPattern)
        {
            for (int i = 0; i < length; i++)
            {
                int randomIndex = Random.Range(0, tiles.Count);
                GameObject anchorTile = tiles[randomIndex];
                currentPattern.Add(anchorTile);
                Debug.Log($"Selected tile {anchorTile.name} at index {randomIndex}");

                EnableAndDropVisual(anchorTile);
            }
        }
        else
        {
            int actualLength = Mathf.Min(length, tiles.Count);
            List<GameObject> shuffledTiles = new List<GameObject>(tiles);
            ShuffleList(shuffledTiles);

            for (int i = 0; i < actualLength; i++)
            {
                GameObject anchorTile = shuffledTiles[i];
                currentPattern.Add(anchorTile);
                Debug.Log($"Selected tile {anchorTile.name} at index {tiles.IndexOf(anchorTile)}");

                EnableAndDropVisual(anchorTile);
            }

            if (length > tiles.Count)
            {
                Debug.LogWarning("Requested pattern length exceeds number of unique tiles! Truncating.");
            }
        }

        StartCoroutine(WaitAndPlayPattern());
    }

    private void EnableAndDropVisual(GameObject anchorTile)
    {
        int index = tiles.IndexOf(anchorTile);
        if (index < 0 || index >= tileVisuals.Count) return;

        GameObject visual = tileVisuals[index];
        visual.SetActive(true);
        visual.transform.position = anchorTile.transform.position + Vector3.up * dropHeight;

        // Play the tile sound when the tile starts dropping
        if (audioSource != null && tileSounds.Length > 0)
        {
            // Play a sound based on the tile index
            int tileIndex = tiles.IndexOf(anchorTile);
            if (tileIndex >= 0 && tileIndex < tileSounds.Length)
            {
                audioSource.PlayOneShot(tileSounds[tileIndex]);
            }
        }

        StartCoroutine(DropTileVisual(visual, anchorTile.transform.position));
    }

    private IEnumerator WaitAndPlayPattern()
    {
        // Wait for all visual tiles to finish dropping
        yield return new WaitForSeconds(1f); // <-- Adjust this delay as needed

        // Now play the pattern sequence (e.g., light up anchor tiles, etc.)
        StartCoroutine(PlayPattern());
    }

    private IEnumerator DropTileVisual(GameObject visual, Vector3 targetPosition)
    {
        while (Vector3.Distance(visual.transform.position, targetPosition) > 0.05f)
        {
            visual.transform.position = Vector3.MoveTowards(visual.transform.position, targetPosition, dropSpeed * Time.deltaTime);
            yield return null;
        }
        visual.transform.position = targetPosition;
    }

    private void SetGrabInteractableActive(bool isActive)
    {
        if (grabInteractableObject != null)
        {
            grabInteractableObject.SetActive(isActive);
        }
        else
        {
            Debug.LogWarning("Grab interactable object is not assigned!");
        }
    }

    private void MoveGrabInteractableToSpawn()
    {
        if (grabInteractableObject != null && grabSpawnPoint != null)
        {
            grabInteractableObject.transform.position = grabSpawnPoint.position;
            grabInteractableObject.transform.rotation = grabSpawnPoint.rotation;
            grabInteractableObject.SetActive(true);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    IEnumerator PlayPattern()
    {
        SetGrabInteractableActive(false);
        Debug.Log("Playing the pattern...");

        // Wait before showing the pattern
        yield return new WaitForSeconds(waitTimeBeforePlayback);

        // Calculate highlight time based on current level
        float currentHighlightTime = Mathf.Max(
            highlightTime - ((currentLevel - 1) * decreaseHighlightTimePerLevel),
            minHighlightTime
        );

        foreach (GameObject tile in currentPattern)
        {
            HighlightTile(tile);
            yield return new WaitForSeconds(currentHighlightTime);
            UnhighlightTile(tile);
            yield return new WaitForSeconds(waitTimeBetweenTiles);
        }

        EnablePlayerInput();

        // Invoke pattern complete event
        OnPatternComplete?.Invoke();
    }

    private void HighlightTile(GameObject tile)
    {
        if (highlightParticlePrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 spawnPosition = tile.transform.position + new Vector3(0f, particleYOffset, 0f);

            ParticleSystem ps = Instantiate(highlightParticlePrefab, spawnPosition, rotation);
            ps.Play();
            activeParticles.Add(ps);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        // Play tile sound if available
        if (audioSource && tileSounds.Length > 0)
        {
            int tileIndex = tiles.IndexOf(tile);
            if (tileIndex >= 0 && tileIndex < tileSounds.Length)
            {
                audioSource.PlayOneShot(tileSounds[tileIndex]);
            }
        }

        Debug.Log($"Highlighted tile: {tile.name}");
    }

    private void UnhighlightTile(GameObject tile)
    {
        Debug.Log($"Finished highlight on tile: {tile.name}");
    }

    private void EnablePlayerInput()
    {
        ClearActiveParticles();
        SetGrabInteractableActive(true);
        MoveGrabInteractableToSpawn();
        Debug.Log("Pattern finished! Player's turn now!");
        inputEnabled = true;
        playerInputIndex = 0;
        inputFailed = false;
        lastStepTime = -1f; // Reset step timer
    }

    public void HandlePlayerStep(GameObject steppedTile)
    {
        if (!inputEnabled || inputFailed)
            return;

        // Prevent double triggers
        if (Time.time - lastStepTime < stepCooldown)
            return;

        lastStepTime = Time.time;

        GameObject expectedTile = currentPattern[playerInputIndex];

        if (steppedTile == expectedTile)
        {
            Debug.Log($"Correct tile {steppedTile.name} at index {playerInputIndex}");

            // Check if it's the last tile in the pattern
            bool isLastTile = (playerInputIndex == currentPattern.Count - 1);

            if (isLastTile)
            {
                // Play special effect for last tile
                PlaySpecialSuccessParticle(steppedTile);

                // Play special success sound
                if (audioSource != null && specialSuccessSound != null)
                {
                    audioSource.PlayOneShot(specialSuccessSound);
                }
            }
            else
            {
                // Regular success effect for other tiles
                PlaySuccessParticle(steppedTile);

                // Play normal tile sound
                if (audioSource != null && tileSounds.Length > 0)
                {
                    int tileIndex = tiles.IndexOf(steppedTile);
                    if (tileIndex >= 0 && tileIndex < tileSounds.Length)
                    {
                        audioSource.PlayOneShot(tileSounds[tileIndex]);
                    }
                }
            }

            playerInputIndex++;

            if (playerInputIndex >= currentPattern.Count)
            {
                Debug.Log("Player completed the pattern!");
                inputEnabled = false;

                // Increase score and level
                currentScore += currentPattern.Count;
                currentLevel++;

                // Invoke success event
                OnPlayerSuccess?.Invoke();

                Debug.Log("Waiting for player to enter the restart area to begin the next level...");
            }
        }
        else
        {
            Debug.Log($"Wrong tile! Expected {expectedTile.name}, but stepped on {steppedTile.name}.");
            PlayFailParticle(steppedTile);

            // Play fail sound
            if (audioSource != null && failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }

            inputFailed = true;
            inputEnabled = false;

            // Invoke fail event
            OnPlayerFail?.Invoke();
        }
    }

    private void PlaySpecialSuccessParticle(GameObject tile)
    {
        if (specialsuccessParticlePrefab != null)
        {
            Vector3 spawnPos = specialParticleSpawnPoint != null
                ? specialParticleSpawnPoint.position
                : tile.transform.position + new Vector3(0f, particleYOffset, 0f);

            ParticleSystem ps = Instantiate(specialsuccessParticlePrefab, spawnPos, Quaternion.identity);
            ps.Play();
            activeParticles.Add(ps);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }



    private void PlaySuccessParticle(GameObject tile)
    {
        if (successParticlePrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 spawnPos = tile.transform.position + new Vector3(0f, particleYOffset, 0f);
            ParticleSystem ps = Instantiate(successParticlePrefab, spawnPos, rotation);
            ps.Play();
            activeParticles.Add(ps);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);

            // Play success sound
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }
        }
    }

    private void PlayFailParticle(GameObject tile)
    {
        if (failParticlePrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 spawnPos = tile.transform.position + new Vector3(0f, particleYOffset, 0f);
            currentFailParticle = Instantiate(failParticlePrefab, spawnPos, rotation);
            currentFailParticle.Play();
            activeParticles.Add(currentFailParticle);
            Destroy(currentFailParticle.gameObject, currentFailParticle.main.duration + currentFailParticle.main.startLifetime.constantMax);

            // Play fail sound
            if (audioSource != null && failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }
    }

    // Handle time expiration
    public void HandleTimeExpired()
    {
        if (inputEnabled && !inputFailed)
        {
            ClearActiveParticles();
            Debug.Log("Time limit exceeded!");
            inputEnabled = false;
            inputFailed = true;

            // Optionally play failure effects on the last correct tile or a random tile
            if (playerInputIndex > 0 && playerInputIndex < currentPattern.Count)
            {
                PlayFailParticle(currentPattern[playerInputIndex]);
            }

            // Play fail sound
            if (audioSource != null && failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }
    }

    private void ClearActiveParticles()
    {
        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps != null)
            {
                Destroy(ps.gameObject);
            }
        }
        activeParticles.Clear();
    }
}