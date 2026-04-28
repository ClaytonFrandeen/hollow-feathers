using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // start game with mouse click
    public void StartGame()
    {
        SceneManager.LoadScene("Clayton-Scene");
    }
}
