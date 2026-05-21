using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;
using StarterAssets;
using Unity.Mathematics;

[RequireComponent(typeof(BoxCollider))]
public class LianaGrind : MonoBehaviour
{
    [Header("Referencias de Escena (Arrastra aquí)")]
    public SplineContainer spline;    
    public GameObject uiPromptCanvas; 

    [Header("Ajustes de Movimiento")]
    public float velocidadGrind = 15f; 

    private bool jugadorCerca = false;
    private bool enLiana = false;
    
    private float progresoGrind = 0f;
    private int direccionDeslizamiento = 1; 
    
    private GameObject jugador;
    private ThirdPersonController tpc;
    private CharacterController cc;

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController charController = other.GetComponentInParent<CharacterController>();
            
            if (charController != null)
            {
                jugador = charController.gameObject; 
                jugadorCerca = true;
                
                if (!enLiana && uiPromptCanvas != null) uiPromptCanvas.SetActive(true); 
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false); 
        }
    }

    void Update()
    {
        if (jugadorCerca && !enLiana)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EmpezarDeslizamiento();
            }
        }

        if (enLiana)
        {
            ActualizarGrindJugador();
        }
    }

    void EmpezarDeslizamiento()
    {
        if (jugador == null || spline == null) return;

        tpc = jugador.GetComponent<ThirdPersonController>();
        cc = jugador.GetComponent<CharacterController>();

        if (tpc == null || cc == null) return;
        if (spline.Splines == null || spline.Splines.Count == 0 || spline.CalculateLength() <= 0.1f) return;

        enLiana = true;

        float3 posMundoInicio = spline.EvaluatePosition(0f);
        float3 posMundoFin = spline.EvaluatePosition(1f);

        float distAInicio = Vector3.Distance(jugador.transform.position, posMundoInicio);
        float distAFin = Vector3.Distance(jugador.transform.position, posMundoFin);

        if (distAInicio < distAFin)
        {
            progresoGrind = 0f;
            direccionDeslizamiento = 1;
        }
        else
        {
            progresoGrind = 1f;
            direccionDeslizamiento = -1;
        }

        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false); 
        
        tpc.enabled = false;
        cc.enabled = false;
    }

    void ActualizarGrindJugador()
    {
        float longitudTotal = spline.CalculateLength();
        progresoGrind += (velocidadGrind / longitudTotal) * Time.deltaTime * direccionDeslizamiento;

        if (progresoGrind >= 1f || progresoGrind <= 0f)
        {
            TerminarDeslizamiento();
        }
        else
        {
            float3 posicionMundo = spline.EvaluatePosition(progresoGrind);
            float3 direccionMundo = spline.EvaluateTangent(progresoGrind);

            if (direccionDeslizamiento == -1) direccionMundo = -direccionMundo;

            jugador.transform.position = posicionMundo;
            
            if (math.length(direccionMundo) > 0.01f)
            {
                jugador.transform.rotation = Quaternion.LookRotation(math.normalize(direccionMundo));
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TerminarDeslizamiento();
            }
        }
    }

    void TerminarDeslizamiento()
    {
        enLiana = false;
        
        if (cc != null) cc.enabled = true;
        if (tpc != null) tpc.enabled = true;
        
        if (uiPromptCanvas != null && jugadorCerca) uiPromptCanvas.SetActive(true);
    }
}