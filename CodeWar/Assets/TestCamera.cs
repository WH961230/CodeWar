using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCamera : MonoBehaviour
{
    private Camera camera;
    // Start is called before the first frame update
    void Start()
    {
     camera = GetComponent<Camera>();   
    }

    private float x;
    private float y;
    private int dirx = 1;
    private int diry = 1;
    // Update is called once per frame
    void Update() {
        if (x < 1 && dirx == 1) {
            x += Time.deltaTime;
        } else if (x > 1 && dirx == 1) {
            dirx = -1;
        } else if (x > 0 && dirx == -1) {
            x -= Time.deltaTime;
        } else if (x < 0 && dirx == -1) {
            dirx = 1;
        }

        if (y < 1 && diry == 1) {
            y += Time.deltaTime;
        } else if (y > 1 && diry == 1) {
            diry = -1;
        } else if (y > 0 && diry == -1) {
            y -= Time.deltaTime;
        } else if (y < 0 && diry == -1) {
            diry = 1;
        }

        camera.rect = new Rect(x, y, 1, 1);
    }
}
