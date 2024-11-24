using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Add this to use SceneManager

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    public void PlayGame()
    {
        SceneManager.LoadScene("OutdoorsScene"); // Use double quotes here
    }
    public void QuitGame()
    {
        Application.Quit(); // Correct method casing
    }

    public void LoadInstruction()
    {
        SceneManager.LoadScene("InstructionScene"); // Use double quotes here
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene"); // Use double quotes here
    }
}
