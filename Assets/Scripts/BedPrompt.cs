using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BedPrompt : MonoBehaviour
{
    [SerializeField] private GameObject prompttextUi;   // UI to display when player is in range
    [SerializeField] private string sceneToLoad;    // name of next scene

    private bool playerInRange;

    private void Start()
    {
        prompttextUi.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            prompttextUi.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            prompttextUi.SetActive(false);
        }
    }
}