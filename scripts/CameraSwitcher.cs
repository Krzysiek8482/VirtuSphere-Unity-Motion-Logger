using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera fpsCamera;       
    public Camera topDownCamera;   
    public bool useTopDown = false; 

    public Canvas canvasFPS;      
    public Canvas canvasTopDown;  


    void Start() { Apply(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            useTopDown = !useTopDown;
            Apply();
        }
    }

    public void Apply()
    {   
        if (fpsCamera)     fpsCamera.enabled = !useTopDown;
        if (topDownCamera) topDownCamera.enabled = useTopDown;

        var fpsAL  = fpsCamera     ? fpsCamera.GetComponent<AudioListener>()     : null;
        var topAL  = topDownCamera ? topDownCamera.GetComponent<AudioListener>() : null;
        if (fpsAL) fpsAL.enabled   = !useTopDown;
        if (topAL) topAL.enabled   = useTopDown;

        if (canvasFPS)     canvasFPS.gameObject.SetActive(!useTopDown);
        if (canvasTopDown) canvasTopDown.gameObject.SetActive(useTopDown);
    }
}
