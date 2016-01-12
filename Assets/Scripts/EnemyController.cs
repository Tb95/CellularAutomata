using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour {

    [Range(0, 100)]
    public float speed;
    [Range(0, 100)]
    public float rotationSpeed;
    [Range(0, 5)]
    public float pathfindingPrecision;
    [Range(0, 5)]
    public float pathfollowingPrecision;
    [Range(0, 5)]
    public float attackRange;

    Transform target;
    Rigidbody myRigidbody;
    Vector3 velocity;
    Pathfinding pathfinding;
    List<Vector3> path;
    Vector3 lastPathfindedPosition;
    Vector3 nextStep;
    Animator animator;
    enum State
    {
        Moving,
        Attacking,
        Idle
    }
    State currentState;
    private State CurrentState
    {
        get { return currentState; }
        set
        {
            if (value != currentState)
            {
                switch (value)
                {
                    case State.Idle:
                        animator.SetInteger("State", 0);
                        velocity = Vector3.zero;
                        break;
                    case State.Moving:
                        animator.SetInteger("State", 1);
                        break;
                    case State.Attacking:
                        animator.SetInteger("State", 2);
                        velocity = Vector3.zero;
                        break;
                }
                currentState = value;
            }
        }
    }

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        myRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        CurrentState = State.Idle;

        pathfinding = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>().pathfinding;
        Pathfind();
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Idle:
                if (ApproximatedDistance(transform.position, target.position) < attackRange * attackRange)
                    CurrentState = State.Attacking;
                else if (ApproximatedDistance(lastPathfindedPosition, target.position) > pathfindingPrecision * pathfindingPrecision)
                {
                    Pathfind();
                    CurrentState = State.Moving;
                }
                break;

            case State.Moving:
                if (ApproximatedDistance(transform.position, target.position) < attackRange * attackRange)
                    CurrentState = State.Attacking;
                else
                {
                    if (ApproximatedDistance(lastPathfindedPosition, target.position) > pathfindingPrecision * pathfindingPrecision)
                        Pathfind();

                    if (ApproximatedDistance(nextStep, transform.position) < pathfollowingPrecision * pathfollowingPrecision)
                    {
                        if (path != null && path.Count > 0)
                        {
                            nextStep = path[0];
                            path.RemoveAt(0);
                        }
                        else
                            CurrentState = State.Idle;
                    }
                }
                
                velocity = nextStep - transform.position;
                velocity.y = 0;
                velocity = velocity.normalized * speed;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), rotationSpeed * Time.deltaTime);
                break;

            case State.Attacking:
                float approxDistance = ApproximatedDistance(transform.position, target.position);

                if ( approxDistance > attackRange * attackRange)
                {
                    if (approxDistance > 5 * attackRange * attackRange)
                    {
                        Pathfind();
                        CurrentState = State.Moving;
                    }
                    else
                    {
                        nextStep = target.position;
                        velocity = nextStep - transform.position;
                        velocity.y = 0;
                        velocity = velocity.normalized * speed;
                        CurrentState = State.Moving;
                    }
                }

                Vector3 lookAt = target.position - transform.position;
                lookAt.y = 0;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookAt), rotationSpeed * Time.deltaTime);
                break;
        }

        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1]);
            }
            Debug.DrawLine(nextStep + new Vector3(2.5f, 0, 2.5f), nextStep + new Vector3(-2.5f, 0, -2.5f), Color.red);
            Debug.DrawLine(nextStep + new Vector3(2.5f, 0, -2.5f), nextStep + new Vector3(-2.5f, 0, 2.5f), Color.red);
        }
    }

    void FixedUpdate()
    {
        if (CurrentState == State.Moving)
        {
            myRigidbody.MovePosition(myRigidbody.position + velocity * Time.fixedDeltaTime);
        }
    }

    void Pathfind()
    {
        lastPathfindedPosition = target.position;

        if (pathfinding == null)
        {
            pathfinding = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>().pathfinding;
            return;
        }

        path = pathfinding.GetPath(transform.position, lastPathfindedPosition);
        if (path != null && path.Count > 1)
        {
            CurrentState = State.Moving;

            path.RemoveAt(path.Count - 1);
            path.Add(lastPathfindedPosition);

            path.RemoveAt(0);
            nextStep = path[0];
            path.RemoveAt(0);            
        }
        else
            Debug.Log(path == null);
    }

    float ApproximatedDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.z - b.z, 2);
    }
}