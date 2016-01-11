using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {

    [Range(1, 100)]
    public float speed;
    [Range(1, 100)]
    public float rotationSpeed;

    Transform target;
    Rigidbody myRigidbody;
    Vector3 velocity;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        myRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        velocity = (target.position - transform.position).normalized * speed;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), rotationSpeed);
    }

    void FixedUpdate()
    {
        myRigidbody.MovePosition(myRigidbody.position + velocity * Time.fixedDeltaTime);
    }
}
