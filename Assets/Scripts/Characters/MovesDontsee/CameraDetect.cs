using UnityEngine;

public class CameraDetect : MonoBehaviour
{
    public Camera camera;

    MeshRenderer renderer;

    public Plane[] cameraFrustum;

    Collider collider;

    public EnemySee enemy;

    void Start()
    {
        camera = Camera.main;
        renderer = GetComponent<MeshRenderer>();
        collider = enemy.GetComponent<Collider>();
        //enemy = GetComponent<EnemySee>();
    }

    // Update is called once per frame
    void Update()
    {
        var bounds = collider.bounds;
        cameraFrustum = GeometryUtility.CalculateFrustumPlanes(camera);
        if (GeometryUtility.TestPlanesAABB(cameraFrustum, bounds))
        {
            Debug.Log("No Mira");
            enemy.CanMove = false;
        }
        else
        {
            Debug.Log("Mira");
            enemy.CanMove = true;
        }
    }
}
