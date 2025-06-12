using UnityEngine;

public class ElementBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    public string defaultAnimationName = "Plane|Action"; // Change this to your preferred animation
    public bool playOnStart = true;
    
    [Header("Interaction Settings")]
    public float scaleMultiplier = 1.2f;
    public float scaleSpeed = 0.1f;
    
    private ChemistryElement elementData;
    private Animator animator;
    private Vector3 originalScale;
    
    public void Initialize(ChemistryElement element)
    {
        elementData = element;
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
        
        // Play the default animation when initialized
        if (animator != null && playOnStart)
        {
            PlayAnimation(defaultAnimationName);
        }
    }
    
    private void Start()
    {
        // Backup initialization if Initialize wasn't called
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            originalScale = transform.localScale;
            
            if (animator != null && playOnStart)
            {
                PlayAnimation(defaultAnimationName);
            }
        }
    }
    
    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            // Check if the animation exists
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            try
            {
                animator.Play(animationName);
                Debug.Log($"Playing animation: {animationName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not play animation '{animationName}': {e.Message}");
                
                // Try alternative animation names
                TryAlternativeAnimations();
            }
        }
    }
    
    private void TryAlternativeAnimations()
    {
        string[] alternativeNames = {
            "Plane|Action",
            "Plane|PlaneAction", 
            "Action",
            "PlaneAction",
            "Take 001"
        };
        
        foreach (string altName in alternativeNames)
        {
            try
            {
                animator.Play(altName);
                Debug.Log($"Successfully playing alternative animation: {altName}");
                return;
            }
            catch
            {
                continue;
            }
        }
        
        Debug.LogWarning("No suitable animation found. Model will remain static.");
    }
    
    // Handle touch/click interaction
    private void OnMouseDown()
    {
        if (elementData != null)
        {
            StartCoroutine(ScaleEffect());
            
            // Optional: Play a different animation on interaction
            if (animator != null)
            {
                // You can switch to a different animation clip on interaction
                PlayAnimation("Plane|Action.001"); // Use one of the longer animations
            }
        }
    }
    
    private System.Collections.IEnumerator ScaleEffect()
    {
        Vector3 targetScale = originalScale * scaleMultiplier;
        
        // Scale up
        float timer = 0;
        while (timer < scaleSpeed)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / scaleSpeed);
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < scaleSpeed)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / scaleSpeed);
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    // Public method to change animations from other scripts
    public void ChangeAnimation(string newAnimationName)
    {
        PlayAnimation(newAnimationName);
    }
    
    // Method to pause/resume animation
    public void PauseAnimation()
    {
        if (animator != null)
            animator.speed = 0;
    }
    
    public void ResumeAnimation()
    {
        if (animator != null)
            animator.speed = 1;
    }
}