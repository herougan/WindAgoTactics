using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Draggable : MonoBehaviour
{
    // public bool SelfInit;

    // private Vector3 offset;
    // private bool draggable = false;
    // private DragDirection dir = DragDirection.y;
    // private Quaternion facing = ObjectUtil.top;

    // public Camera gameCamera;


    // // Velocity
    // private Vector3 velocity;

    // public Quaternion Facing { get => facing; set => facing = value; }

    // private void Awake()
    // {
    //     if (this.enabled) draggable = true;
    // }

    // public void Init(Camera camera, bool enabled)
    // {
    //     gameCamera = camera;
    //     draggable = enabled;
    // }

    // void OnMouseDown()
    // {
    //     // Store position offset between object and mouse
    //     if (draggable)
    //     {
    //         offset = gameObject.transform.position - GetMouseRelativePos();
    //         // Inform board state manager
    //     }
    // }

    // void OnMouseDrag()
    // {
    //     if (draggable) { 
    //         // Object follows mouse
    //         transform.position = GetMouseRelativePos() + offset;
    //     }
    // }

    // private Vector3 GetMouseRelativePos()
    // {
    //     // Get Mouse position (x, y)
    //     Vector3 mouse = Input.mousePosition;
    //     // Edit mouse z to gameobject (x, y, z)
    //     switch (dir)
    //     {
    //         case DragDirection.x:
    //             mouse.x = gameCamera.WorldToScreenPoint(gameObject.transform.position).x;
    //             break;
    //         case DragDirection.y:
    //             mouse.y = gameCamera.WorldToScreenPoint(gameObject.transform.position).y;
    //             break;
    //         case DragDirection.z:
    //             mouse.z = gameCamera.WorldToScreenPoint(gameObject.transform.position).z;
    //             break;
    //         case DragDirection.nx:
    //             mouse.x = - gameCamera.WorldToScreenPoint(gameObject.transform.position).x;
    //             break;
    //         case DragDirection.ny:
    //             mouse.y = - gameCamera.WorldToScreenPoint(gameObject.transform.position).y;
    //             break;
    //         case DragDirection.nz:
    //             mouse.z = - gameCamera.WorldToScreenPoint(gameObject.transform.position).z;
    //             break;
    //     }
    //     // Mouse position relative to object plane
    //     return Camera.main.ScreenToWorldPoint(mouse);
    // }

    // public void Switch(bool enable)
    // {
    //     draggable = enable;
    // }

    // // Update is called once per frame
    // void Update()
    // {

    // }

    // public enum DragDirection
    // {
    //     x,
    //     y,
    //     z,
    //     nx,
    //     ny,
    //     nz,
    // }
}

// Movement Style

// Tacit style card movement. Card wobbles when pulling it. []
// When let go, card gets "pulled" back into hand []
// To place, hover card around a valid slot and let go []
// If invalid, cards get pulled back. []
// Buffer time of ~0.2s where the mouse must not move much. Maybe. []

// Click Style

// Clicking the card puts it into some left "card" view. []
// Context-based actions after. eg. if need tributes, camera points to scene [] []
// awaiting tribute-related clicks []
// Potential tributes light up. []
// Left click away or on the card itself to cancel []

// Right click on any card
// to show it on the right side. []
// Effects appear as tooltips if right click is pressed [] -> elaboration needed
