
using UnityEngine;

public class RotateCubeFaces : MonoBehaviour
{
    public float interval = 1.0f; // seconds per face
    private float timer = 0f;
    private int faceIndex = 0;
    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            faceIndex = (faceIndex + 1) % 6; // cycle 0–5
            mat.SetInt("_FaceIndex", faceIndex);
        }
    }
}
