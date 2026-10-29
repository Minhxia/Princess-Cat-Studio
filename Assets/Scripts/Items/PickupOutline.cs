using System.Collections.Generic;
using UnityEditor.SpeedTree.Importer;
using UnityEngine;
using UnityEngine.Rendering;    // para opciones de renderizado

[DisallowMultipleComponent]

// Mesh       ->    contiene la geometría: vértices, triángulos, normales, etc.
// MeshFilter ->    referencia la malla que utiliza el GameObject
// MeshRenderer ->	se encarga de dibujarla con sus materiales

// Clase que prepara los elementos necesarios para dibujar el contorno de los objetos recogibles
// y que lo muestra o lo oculta en función de la situación
public class PickupOutline : MonoBehaviour
{
    #region Variables
    [Header("Outline")]
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private bool _smoothNormals = true;

    // si está vacío, se usan todo los MeshRenderer del objeto y sus hijos
    [SerializeField] private MeshRenderer[] _targetRenderers;

    private readonly List<MeshRenderer> _sourceRenderers = new List<MeshRenderer>(); // originales del objeto
    private readonly List<MeshRenderer> _outlineRenderers = new List<MeshRenderer>(); // auxiliares del contorno
    private readonly List<Mesh> _createdMeshes = new List<Mesh>(); // mallas nuevas que se crean para suavizar las normales
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (_outlineMaterial == null)
        {
            Debug.LogWarning("[PickupOutline] Asigna el material del contorno en el Inspector.", this);
            return;
        }

        MeshRenderer[] renderers = _targetRenderers != null && _targetRenderers.Length > 0
            ? _targetRenderers : GetComponentsInChildren<MeshRenderer>(true);
        // con (true) se incluyen también los hijos inactivos!

        // se obtiene la malla de cada parte
        foreach (MeshRenderer sourceRenderer in renderers)
        {
            if (sourceRenderer == null || _sourceRenderers.Contains(sourceRenderer))
                continue;

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            Mesh outlineMesh = sourceFilter.sharedMesh; // referencia a la malla existente

            // si se quieren las normales suavizadas
            if (_smoothNormals && outlineMesh.isReadable)
            {
                outlineMesh = CreateSmoothedMesh(outlineMesh);
                _createdMeshes.Add(outlineMesh);
            }
            else if (_smoothNormals)
            {
                Debug.LogWarning("[PickupOutline] La malla: " + outlineMesh.name + ",  no es legible. " +
                    "El contorno usará las normales originales. Si aparecen cortes en la esquinas, activa " +
                    " Read/Write en las opciones de importación del modelo.");
            }

            // se crea la copia del objeto como hijo
            GameObject outlineObject = new GameObject("PickupOutlineVisual");
            outlineObject.layer = sourceRenderer.gameObject.layer;
            outlineObject.transform.SetParent(sourceRenderer.transform, false);

            // se le añaden los componentes visuales
            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = outlineMesh;
            // y el renderizador
            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.enabled = false; // para que el contorno empiece apagado
            // se desactivan sus sombras e iluminación, solo es un contorno
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // se preparan sus materiales
            Material[] materials = new Material[outlineMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = _outlineMaterial;
            }
            outlineRenderer.sharedMaterials = materials;

            // por último, se amplía la caja que Unity usa para comprobar la visibilidad, ya
            // que la copia se ensancha
            Bounds bounds = sourceRenderer.localBounds;
            Vector3 scale = sourceRenderer.transform.lossyScale;    // escala global aprox.

            float width = _outlineMaterial.HasProperty("_OutlineWidth")
                ? _outlineMaterial.GetFloat("_OutlineWidth")
                : 0.003f;
            // se amplía la caja
            bounds.Expand(new Vector3(
                2f * width / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                2f * width / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                2f * width / Mathf.Max(Mathf.Abs(scale.z), 0.0001f)
            ));
            outlineRenderer.localBounds = bounds;

            // se guardan las referencias
            _sourceRenderers.Add(sourceRenderer);
            _outlineRenderers.Add(outlineRenderer);
        }
    }

    // Apaga el efecto al desactivarse el componente
    private void OnDisable()
    {
        SetHighlighted(false);
        
    }

    // Limpieza de todo lo que ha creado al destruirse
    private void OnDestroy()
    {
        foreach (MeshRenderer outlineRenderer in _outlineRenderers)
        {
            if (outlineRenderer != null)
                Destroy(outlineRenderer.gameObject);
        }

        foreach (Mesh mesh in _createdMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
    #endregion

    #region Other Methods
    // Muestra u oculta el contorno del objeto (la copia)
    public void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < _outlineRenderers.Count; i++)
        {
            if (_outlineRenderers[i] != null)
            {
                _outlineRenderers[i].enabled = highlighted && _sourceRenderers[i] != null
                    && _sourceRenderers[i].enabled;
            }
        }
    }

    // Crea las normales suavizadas de la copia del objeto
    private Mesh CreateSmoothedMesh(Mesh sourceMesh)
    {
        // se duplica la malla, y se trabaja en esa copia
        Mesh mesh = Instantiate(sourceMesh);
        mesh.name = sourceMesh.name + "_PickupOutline";

        // se obtienen los datos de la copia: vértices y normales
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        if (normals.Length != vertices.Length)
        {
            mesh.RecalculateNormals();
            normals = mesh.normals;
        }

        // como puede darse el caso de que varios vértices compartan posición y tener normales
        // diferentes, se agrupan en un diccionario
        // es decir, se unen las direcciones de los vértices que comparten posición
        Dictionary<Vector3, Vector3> normalSums = new Dictionary<Vector3, Vector3>();
        // primero se acumulan las normales
        for (int i = 0; i < vertices.Length; i++)
        {
            normalSums.TryGetValue(vertices[i], out Vector3 sum);
            normalSums[vertices[i]] = sum + normals[i];
        }
        // luego, se asigna una dirección común
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 normal = normalSums[vertices[i]];
            // en caso de las normales (por su dirección) se hayan cancelado entre sí
            if (normal.sqrMagnitude > 0.000001f)
            {
                normals[i] = normal.normalized;
            }
        }

        mesh.normals = normals;
        return mesh;
    }
    #endregion
}
