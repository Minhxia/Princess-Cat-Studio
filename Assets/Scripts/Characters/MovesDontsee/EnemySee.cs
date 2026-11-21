using UnityEngine;

public class EnemySee : MonoBehaviour
{
    GameObject Player;

    public float speed = 3f;

    public bool CanMove;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (CanMove)
        {
            Move();
        }
        else
        {
            Stop();
        }

        transform.position = Vector3.MoveTowards(transform.position, Player.transform.position, speed * Time.deltaTime);
    }

    void Move()
    {
        speed = 3f;
    }

    void Stop()
    {
        speed = 0f;
    }
}
