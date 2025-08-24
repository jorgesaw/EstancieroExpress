using System.Collections;
using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float snapDistance = 0.02f;

    [HideInInspector] public int boardIndex = 0;
    [HideInInspector] public bool isMoving = false;

    // Ahora avanza en SENTIDO HORARIO (restando índice)
    public IEnumerator MoveSteps(Vector3[] path, int steps)
    {
        isMoving = true;
        while (steps-- > 0)
        {
            // antes: boardIndex = (boardIndex + 1) % path.Length;
            // antes (para compensar tiles antihorario): 
            // boardIndex = (boardIndex - 1 + path.Length) % path.Length;

            // ahora (tiles ya horario → avance horario sumando):
            boardIndex = (boardIndex + 1) % path.Length;

            yield return MoveTo(path[boardIndex]);
        }
        isMoving = false;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > snapDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    public void TeleportTo(Vector3 position, int index)
    {
        transform.position = position;
        boardIndex = index;
    }
}
