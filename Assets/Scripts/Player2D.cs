using UnityEngine;
using System.Collections;

public class Player2D : MonoBehaviour
{
    #region variables
    [Range(1, 100)]
    public float speed;

    Rigidbody2D myRigidbody;
    Vector2 velocity;
    #endregion

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * speed;
    }

    void FixedUpdate()
    {
        myRigidbody.MovePosition(myRigidbody.position + velocity * Time.fixedDeltaTime);
    }
}
