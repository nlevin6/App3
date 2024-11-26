using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Reference to the pause menu canvas
    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Change key as needed
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); 
        Time.timeScale = 1f;
        AudioListener.volume = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;
       
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true); 
        Time.timeScale = 0f; 
        AudioListener.volume = 0f;

        Cursor.lockState = CursorLockMode.None; // Unlocks the cursor
        Cursor.visible = true;
        isPaused = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit(); 
    }
}
