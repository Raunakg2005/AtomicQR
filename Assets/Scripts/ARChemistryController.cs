using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[System.Serializable]
public class ChemistryElement
{
    public GameObject elementPrefab; 
    public string qrCodeId;          
}

public class ARChemistryController : MonoBehaviour
{
    [Header("AR Components")]
    public ARTrackedImageManager trackedImageManager;

    [Header("Chemistry Elements Data")]
    public ChemistryElement[] elements;

    [Header("Global Transform Settings")]
    [Tooltip("Global scale multiplier for all spawned elements")]
    public Vector3 globalScale = Vector3.one;
    
    [Tooltip("Global rotation offset for all spawned elements")]
    public Vector3 globalRotationOffset = Vector3.zero;
    
    [Tooltip("Global position offset for all spawned elements")]
    public Vector3 globalPositionOffset = Vector3.zero;

    [Header("Display Settings")]
    [Tooltip("Show only one element at a time")]
    public bool showOnlyOneElement = true;
    
    [Tooltip("Instantly hide elements when tracking is lost")]
    public bool hideOnTrackingLost = true;

    [Header("Debug Settings")]
    [Tooltip("Show debug window on device")]
    public bool showDebugWindow = true;
    
    [Tooltip("Debug window position (0-1 screen space)")]
    public Vector2 debugWindowPosition = new Vector2(0.02f, 0.02f);
    
    [Tooltip("Auto-hide debug window after this many seconds (0 = never)")]
    public float autoHideDebugAfter = 10f;

    private Dictionary<string, ChemistryElement> elementDatabase;
    private Dictionary<string, GameObject> preloadedElements;
    private Dictionary<string, Transform> trackingAnchors;
    private Quaternion globalRotation;
    private string currentlyVisibleElement = "";

    private bool debugWindowVisible = true;
    private float debugWindowTimer = 0f;
    private Vector2 scrollPosition = Vector2.zero;
    private bool showDetailedInfo = false;
    private bool showTransformControls = false;
    private string debugLog = "";
    private Queue<string> recentLogs = new Queue<string>();
    private const int maxLogEntries = 10;

    private bool isDraggingDebugWindow = false;
    private Vector2 dragStartPos;
    private Vector2 windowStartPos;

    void Start()
    {
        InitializeSystem();
        debugWindowTimer = autoHideDebugAfter;
    }

    void InitializeSystem()
    {
        elementDatabase = new Dictionary<string, ChemistryElement>();
        preloadedElements = new Dictionary<string, GameObject>();
        trackingAnchors = new Dictionary<string, Transform>();

        globalRotation = Quaternion.Euler(globalRotationOffset);

        foreach (var element in elements)
        {
            if (element != null && element.elementPrefab != null && !string.IsNullOrEmpty(element.qrCodeId))
            {
                elementDatabase[element.qrCodeId] = element;
                PreloadElement(element);
                AddDebugLog($"Preloaded: {element.qrCodeId}");
            }
            else
            {
                AddDebugLog("Invalid element config detected");
            }
        }

        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            AddDebugLog($"AR System initialized. {preloadedElements.Count} elements preloaded");
        }
        else
        {
            AddDebugLog("ERROR: ARTrackedImageManager not assigned!");
        }
    }

    void PreloadElement(ChemistryElement element)
    {
        try
        {
            GameObject anchor = new GameObject($"Anchor_{element.qrCodeId}");
            anchor.transform.SetParent(transform);
            trackingAnchors[element.qrCodeId] = anchor.transform;

            GameObject obj = Instantiate(element.elementPrefab, anchor.transform);
            obj.transform.localPosition = globalPositionOffset;
            obj.transform.localRotation = globalRotation;
            obj.transform.localScale = globalScale;
            
            obj.SetActive(false);
            
            preloadedElements[element.qrCodeId] = obj;
        }
        catch (System.Exception e)
        {
            AddDebugLog($"Failed to preload {element.qrCodeId}: {e.Message}");
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            HandleImageAdded(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            HandleImageUpdated(trackedImage);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            HandleImageRemoved(trackedImage);
        }
    }

    void HandleImageAdded(ARTrackedImage trackedImage)
    {
        string imageId = trackedImage.referenceImage.name;
        
        if (!preloadedElements.ContainsKey(imageId))
        {
            AddDebugLog($"No preloaded element for: {imageId}");
            return;
        }

        if (showOnlyOneElement && !string.IsNullOrEmpty(currentlyVisibleElement))
        {
            HideElement(currentlyVisibleElement);
        }

        ShowElement(imageId, trackedImage.transform);
        AddDebugLog($"Showing: {imageId}");
    }

    void HandleImageUpdated(ARTrackedImage trackedImage)
    {
        string imageId = trackedImage.referenceImage.name;

        if (!preloadedElements.ContainsKey(imageId))
            return;

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            if (!preloadedElements[imageId].activeInHierarchy)
            {
                ShowElement(imageId, trackedImage.transform);
            }
            else
            {
                UpdateElementTransform(imageId, trackedImage.transform);
            }
        }
        else if (hideOnTrackingLost)
        {
            HideElement(imageId);
            AddDebugLog($"Lost tracking: {imageId}");
        }
    }

    void HandleImageRemoved(ARTrackedImage trackedImage)
    {
        string imageId = trackedImage.referenceImage.name;
        HideElement(imageId);
        AddDebugLog($"Hidden (removed): {imageId}");
    }

    void ShowElement(string imageId, Transform trackedTransform)
    {
        if (!preloadedElements.ContainsKey(imageId))
            return;

        GameObject element = preloadedElements[imageId];
        
        UpdateElementTransform(imageId, trackedTransform);
        
        element.SetActive(true);
        
        currentlyVisibleElement = imageId;
    }

    void HideElement(string imageId)
    {
        if (!preloadedElements.ContainsKey(imageId))
            return;

        GameObject element = preloadedElements[imageId];
        element.SetActive(false);
        
        if (currentlyVisibleElement == imageId)
        {
            currentlyVisibleElement = "";
        }
    }

    void UpdateElementTransform(string imageId, Transform trackedTransform)
    {
        if (!trackingAnchors.ContainsKey(imageId))
            return;

        Transform anchor = trackingAnchors[imageId];
        
        anchor.position = trackedTransform.position;
        anchor.rotation = trackedTransform.rotation;
    }

    void HideAllElements()
    {
        foreach (var kvp in preloadedElements)
        {
            kvp.Value.SetActive(false);
        }
        currentlyVisibleElement = "";
    }

    void AddDebugLog(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        recentLogs.Enqueue(logEntry);
        if (recentLogs.Count > maxLogEntries)
        {
            recentLogs.Dequeue();
        }
        
        Debug.Log(logEntry);
        
        if (autoHideDebugAfter > 0)
        {
            debugWindowTimer = autoHideDebugAfter;
            debugWindowVisible = true;
        }
    }

    void Update()
    {
        if (autoHideDebugAfter > 0 && debugWindowVisible)
        {
            debugWindowTimer -= Time.deltaTime;
            if (debugWindowTimer <= 0)
            {
                debugWindowVisible = false;
            }
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began && touch.tapCount >= 3)
            {
                debugWindowVisible = !debugWindowVisible;
                if (debugWindowVisible)
                {
                    debugWindowTimer = autoHideDebugAfter;
                }
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugWindow || !debugWindowVisible)
            return;

        float scaleFactor = Screen.width / 800f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scaleFactor, scaleFactor, 1));

        float windowWidth = 350f;
        float windowHeight = showDetailedInfo ? 400f : 250f;
        
        float posX = debugWindowPosition.x * (Screen.width / scaleFactor - windowWidth);
        float posY = debugWindowPosition.y * (Screen.height / scaleFactor - windowHeight);
        
        Rect windowRect = new Rect(posX, posY, windowWidth, windowHeight);

        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(windowRect, "");
        GUI.color = Color.white;

        GUILayout.BeginArea(windowRect);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("AR Chemistry Debug", GUI.skin.box);
        if (GUILayout.Button("X", GUILayout.Width(30)))
        {
            debugWindowVisible = false;
        }
        GUILayout.EndHorizontal();

        if (autoHideDebugAfter > 0)
        {
            GUILayout.Label($"Auto-hide in: {debugWindowTimer:F1}s");
        }

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label($"Preloaded Elements: {preloadedElements.Count}");
        GUILayout.Label($"Currently Visible: {(string.IsNullOrEmpty(currentlyVisibleElement) ? "None" : currentlyVisibleElement)}");
        
        int visibleCount = 0;
        foreach (var kvp in preloadedElements)
        {
            if (kvp.Value.activeInHierarchy)
                visibleCount++;
        }
        GUILayout.Label($"Total Visible: {visibleCount}");
        
        if (trackedImageManager != null)
        {
            GUILayout.Label($"Tracked Images: {trackedImageManager.trackables.count}");
        }
        GUILayout.EndVertical();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hide All"))
        {
            HideAllElements();
            AddDebugLog("Manual: Hide all elements");
        }
        if (GUILayout.Button("Details"))
        {
            showDetailedInfo = !showDetailedInfo;
        }
        if (GUILayout.Button("Transforms"))
        {
            showTransformControls = !showTransformControls;
        }
        GUILayout.EndHorizontal();

        if (showDetailedInfo)
        {
            GUILayout.Label("Recent Logs:", GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            
            foreach (string log in recentLogs)
            {
                GUILayout.Label(log, GUI.skin.textArea);
            }
            
            GUILayout.EndScrollView();

            GUILayout.Label("Element Status:", GUI.skin.box);
            foreach (var kvp in preloadedElements)
            {
                string status = kvp.Value.activeInHierarchy ? "VISIBLE" : "hidden";
                GUILayout.Label($"{kvp.Key}: {status}");
            }
        }

        if (showTransformControls)
        {
            GUILayout.Label("Global Transform Controls:", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale:", GUILayout.Width(50));
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                SetGlobalScale(globalScale * 0.8f);
                AddDebugLog($"Scale: {globalScale}");
            }
            GUILayout.Label(globalScale.x.ToString("F2"));
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                SetGlobalScale(globalScale * 1.2f);
                AddDebugLog($"Scale: {globalScale}");
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset Transform"))
            {
                SetGlobalScale(Vector3.one);
                SetGlobalRotation(Vector3.zero);
                SetGlobalPosition(Vector3.zero);
                AddDebugLog("Transform reset");
            }
        }

        GUILayout.EndArea();

        GUI.color = new Color(1, 1, 1, 0.7f);
        GUI.Label(new Rect(10, Screen.height / scaleFactor - 60, 300, 50), 
                  "Triple-tap screen to toggle debug window\nSwipe down from top to show again");
        GUI.color = Color.white;
    }

    public void SetGlobalScale(Vector3 newScale)
    {
        globalScale = newScale;
        UpdateAllElementTransforms();
    }

    public void SetGlobalRotation(Vector3 newRotationOffset)
    {
        globalRotationOffset = newRotationOffset;
        globalRotation = Quaternion.Euler(globalRotationOffset);
        UpdateAllElementTransforms();
    }

    public void SetGlobalPosition(Vector3 newPositionOffset)
    {
        globalPositionOffset = newPositionOffset;
        UpdateAllElementTransforms();
    }

    private void UpdateAllElementTransforms()
    {
        foreach (var kvp in preloadedElements)
        {
            Transform elementTransform = kvp.Value.transform;
            elementTransform.localPosition = globalPositionOffset;
            elementTransform.localRotation = globalRotation;
            elementTransform.localScale = globalScale;
        }
    }

    public int GetPreloadedElementCount()
    {
        return preloadedElements.Count;
    }

    public bool IsElementVisible(string imageId)
    {
        return preloadedElements.ContainsKey(imageId) && 
               preloadedElements[imageId].activeInHierarchy;
    }

    public void ForceShowElement(string imageId)
    {
        if (preloadedElements.ContainsKey(imageId))
        {
            if (showOnlyOneElement)
                HideAllElements();
            
            preloadedElements[imageId].SetActive(true);
            currentlyVisibleElement = imageId;
            AddDebugLog($"Force show: {imageId}");
        }
    }

    public void ForceHideElement(string imageId)
    {
        HideElement(imageId);
        AddDebugLog($"Force hide: {imageId}");
    }

    public string GetCurrentlyVisibleElement()
    {
        return currentlyVisibleElement;
    }

    public void ToggleDebugWindow()
    {
        debugWindowVisible = !debugWindowVisible;
        if (debugWindowVisible)
        {
            debugWindowTimer = autoHideDebugAfter;
        }
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnDestroy()
    {
        OnDisable();
    }
}
