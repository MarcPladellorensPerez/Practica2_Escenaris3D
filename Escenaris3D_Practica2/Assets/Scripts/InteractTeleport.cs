using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class InteractTeleport : MonoBehaviour
{
    [Header("Referencias de Escena (Arrastra aquí)")]
    public Transform destinoTeletransporte; 
    public GameObject uiPromptCanvas; 

    private bool jugadorCerca = false;
    private GameObject jugador;
    private CharacterController cc;

    void Start()
    {
        // Nos aseguramos de que el collider actúe como un área de detección
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
                cc = charController;
                jugadorCerca = true;
                
                if (uiPromptCanvas != null) uiPromptCanvas.SetActive(true);
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
        if (jugadorCerca)
        {
            // Detectar si se pulsa la tecla E
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EjecutarTeletransporte();
            }
        }
    }

    void EjecutarTeletransporte()
    {
        if (jugador == null || destinoTeletransporte == null) return;

        // Ocultar el texto al teletransportarse
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);

        // Desactivar el CharacterController momentáneamente para poder forzar la posición
        if (cc != null) cc.enabled = false;

        // Mover al jugador a la posición del destino
        jugador.transform.position = destinoTeletransporte.position;
        
        // Opcional: También copia la rotación del destino por si quieres que mire hacia un lado concreto
        jugador.transform.rotation = destinoTeletransporte.rotation;

        if (cc != null) cc.enabled = true;
    }
}