using UnityEngine;
using System.Collections;

/// <summary>
/// Manages invincibility frames and visual feedback (flashing).
/// </summary>
public class EnemyInvincibility : MonoBehaviour
{
    // [SerializeField] private float defaultDuration = 0.5f; // Set by Health usually
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.8f); // White flash

    private SpriteRenderer spriteRenderer;
    private Material defaultMaterial;
    private MaterialPropertyBlock propertyBlock;
    
    private float invincibilityTimer;
    private bool isFlashing;

    public bool IsInvincible => invincibilityTimer > 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultMaterial = spriteRenderer.material;
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;

            if (invincibilityTimer <= 0f)
            {
                StopInvincibility();
            }
        }
    }

    public void StartInvincibility(float duration)
    {
        invincibilityTimer = duration;
        if (!isFlashing)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    public void StopInvincibility()
    {
        invincibilityTimer = 0f;
        isFlashing = false;
        
        // Reset visuals
        if (spriteRenderer != null)
        {
             // Reset color properly
             if (propertyBlock != null)
             {
                 spriteRenderer.GetPropertyBlock(propertyBlock);
                 propertyBlock.SetColor("_Color", Color.white); // Assuming default is white tint
                 // OR propertyBlock.Clear(); 
                 spriteRenderer.SetPropertyBlock(propertyBlock);
             }
             else
             {
                 spriteRenderer.color = Color.white;
             }
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        bool toggle = false;

        while (invincibilityTimer > 0f)
        {
            toggle = !toggle;
            
            if (spriteRenderer != null)
            {
                if (propertyBlock != null)
                {
                    spriteRenderer.GetPropertyBlock(propertyBlock);
                    // Standard sprites use _Color for tint
                    propertyBlock.SetColor("_Color", toggle ? flashColor : Color.white);
                    spriteRenderer.SetPropertyBlock(propertyBlock);
                }
                else
                {
                     // Fallback if PropertyBlock fails (though it shouldn't)
                     spriteRenderer.color = toggle ? flashColor : Color.white;
                }
            }

            yield return new WaitForSeconds(flashInterval);
        }
        
        StopInvincibility();
    }
}
