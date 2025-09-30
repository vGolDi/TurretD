using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene"); // nazwa Twojej sceny z rozgrywk¹
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
