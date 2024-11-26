using TMPro; // Import TextMeshPro namespace
using UnityEngine;

public class RoundOverDisplay : MonoBehaviour
{
    public TextMeshProUGUI roundOverText; // Reference to the TextMeshPro object
    

    void Start()
    {
        // Get the number of rounds survived from the SpawnManager
        int roundsSurvived = RoundData.RoundsSurvived;

        // Set the TextMeshPro text to display the message
        roundOverText.text = $"You survived {roundsSurvived} rounds";
    }
}
