using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGridController : MonoBehaviour
{
    public float speed = 5f;
    public Transform movePoint;
    public Animator anim;
    public bool check = false;
    public LayerMask stopMovement;
    public int numMovimiento = 0;
    private BattleSystem battleSystem;
    private MainCharacter player;

    void Start()
    {
        movePoint.parent = null;
        player = GetComponent<MainCharacter>();
        FindBattleSystem();
    }

    void Update()
    {
        if (battleSystem == null)
        {
            FindBattleSystem();
        }
        Debug.Log(battleSystem.state);
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, movePoint.position) <= 0f) // chequea que ya ha llegado a la casilla antes de volver a mover
        {
            if (battleSystem.state == BattleState.WON || battleSystem.reset == true)
            {
                numMovimiento = 0; // si has ganado te podras mover libremente
            }
            if (numMovimiento < player.GetSpeed()) //jugador limitado ha su movimiento
            {
                if (battleSystem.canMove == true) // es tu turno?
                {
                    if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
                    {
                        if (!Physics2D.OverlapCircle(movePoint.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f), .2f, stopMovement)) // chequea si no hay colision
                        {
                            movePoint.position += new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                            numMovimiento++; // suma 1 al movimiento
                        }
                    }
                    else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
                    {
                        if (!Physics2D.OverlapCircle(movePoint.position + new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f), .2f, stopMovement))
                        {
                            movePoint.position += new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                            numMovimiento++; // suma 1 al movimiento
                        }
                    }
                    if (Input.GetAxisRaw("Horizontal") < 0 && !check)// Para cuando el player gira a la izquierda, para que flipee el objeto
                    {
                        transform.Rotate(new Vector3(0, 180, 0));
                        check = true;
                    }
                    anim.SetFloat("Horizontal", Input.GetAxisRaw("Horizontal"));
                    anim.SetFloat("Vertical", Input.GetAxisRaw("Vertical"));
                    if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                    {
                        if(check == true) // cuando este quieto, si esta flipeado que vuelva al original
                        {
                            transform.Rotate(new Vector3(0, 180, 0));
                            check = false;
                        }
                        anim.SetBool("Moving", false);
                    }
                    else
                    {
                        anim.SetBool("Moving", true);
                    }
                }
            }
            else
            {
                if (check == true) // cuando este quieto, si esta flipeado que vuelva al original
                {
                    transform.Rotate(new Vector3(0, 180, 0));
                    check = false;
                }
                anim.SetBool("Moving", false);
            }
        }
    }

    private void FindBattleSystem()
    {
        battleSystem = GameObject.FindObjectOfType<BattleSystem>();
    }
    public void SetMovePointToSpawn(Vector3 spawnPosition)
    {
        movePoint.position = spawnPosition;
    }
}
