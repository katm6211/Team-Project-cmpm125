using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFade : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "CabinScene";
    [SerializeField] private float timeBeforeFade = 20f;
    [SerializeField] private float fadeDuration = 1.5f;

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);

        yield return new WaitForSeconds(timeBeforeFade);

        // Capture what the player currently sees
        yield return new WaitForEndOfFrame();
        Texture2D snapshot = ScreenCapture.CaptureScreenshotAsTexture();

        // Build a persistent full-screen overlay showing that snapshot
        var canvasGO = new GameObject("CrossfadeCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var group = canvasGO.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        var imgGO = new GameObject("Snapshot");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var raw = imgGO.AddComponent<RawImage>();
        raw.texture = snapshot;
        raw.raycastTarget = false;
        var rt = raw.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Load the new scene underneath the snapshot
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        while (!op.isDone) yield return null;
        yield return null; // let the new scene render one frame

        // Fade the snapshot out to reveal the new scene
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        Destroy(snapshot);
        Destroy(canvasGO);
        Destroy(gameObject);
    }
}
