using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("UI")]
    public Slider healthBar;
    public GameObject crosshair;
    public GameObject gameOverPanel;

    private float currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (crosshair != null)
        {
            crosshair.SetActive(true);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("Player died.");

        // Show Game Over screen
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Hide health bar
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        // Hide crosshair
        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }

        // Disable movement
        FirstPersonController movement =
            GetComponent<FirstPersonController>();

        if (movement != null)
        {
            movement.enabled = false;
        }

        // Disable mouse look
        MouseLook mouseLook =
            GetComponentInChildren<MouseLook>();

        if (mouseLook != null)
        {
            mouseLook.enabled = false;
        }

        // Disable shooting
        GunShoot gunShoot =
            GetComponentInChildren<GunShoot>();

        if (gunShoot != null)
        {
            gunShoot.enabled = false;
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}