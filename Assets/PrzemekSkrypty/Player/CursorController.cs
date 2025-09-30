using UnityEngine;

public class CursorController : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;                  
    }

    private void Update()
    {
        // Wymuszaj widocznoœæ kursora w ka¿dej klatce, aby nadpisaæ inne skrypty
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }
}