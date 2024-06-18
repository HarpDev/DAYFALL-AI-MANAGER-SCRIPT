using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;


/**************************
 * 
 * Harps' SmartAI Controller
 * 
 * by HarpCo.
 * 
 **************************/


public enum States
{
    Chasing,
    Stalking,
    Hiding, 
    Staring,
    Wandering,
} 

public class smartAI : MonoBehaviour
{
    public IEnumerator Chasing()
    {
        Vector3 posPlayer = Player.transform.position;
        ai.SetDestination(posPlayer);
        ai.speed = chaseSpeed;

        yield return new WaitForSecondsRealtime(0.1f);
    }
    public IEnumerator Hiding()
    {

        yield return new WaitForSecondsRealtime(0.1f);
    }
    public IEnumerator Staring()
    {

        yield return new WaitForSecondsRealtime(0.1f);
    }
    public IEnumerator Stalking()
    {
        Vector3 posPlayer = Player.transform.position;
        ai.SetDestination(posPlayer);

        if(ai.remainingDistance < minDistanceFromPlayerWhenStalking)
        {
            ai.speed = stalkFarSpeed;
        }
        else
        {
            ai.speed = stalkSpeed;
        }
       
        yield return new WaitForSecondsRealtime(0.1f);
    }
    public IEnumerator Wandering()
    {
        while (true)
        {
            // Pick a new random point
            PickRandomPoint();

            // Wait until the agent has reached the destination
            while (!ai.pathPending && ai.remainingDistance > 0.5f)
            {
                // Add noise to the movement
                AddNoiseToMovement();

                // Random chance to redirect to a new point
                if (Random.value < redirectChance)
                {
                    PickRandomPoint();
                }
               

                yield return null;
            }

            // Wait for a specified time before picking a new point
            yield return new WaitForSeconds(waitTime);
        }
        yield return new WaitForSecondsRealtime(0.1f);
    }


    private States _currentState;
    public States currentState
    {
        get { return _currentState; }
        set
        {

            StopAllCoroutines();
            _currentState = value;
            switch (_currentState)
            {
                case States.Chasing:
                    StartCoroutine(this.Chasing());
                    break;
                case States.Hiding:
                    StartCoroutine(this.Hiding());
                    break;
                case States.Stalking:
                    StartCoroutine(this.Stalking());
                    break;
                case States.Staring:
                    StartCoroutine(this.Staring());
                    break;
                case States.Wandering:
                    StartCoroutine(this.Wandering());
                    break;
                default:
                    break;
            }

        }

    }


    [Header("AI Stuff")]
    public NavMeshAgent ai;
    public bool _isAiActive;

    [Header("Speed Values")]
    float wanderSpeed = 3f;
    float chaseSpeed = 8f;
    float hideSpeed = 6f;
    float stalkSpeed = 1f;
    float stalkFarSpeed = 7f;


    public float aiDistance;
    public bool _isBlocked;
    public bool _isViewed;
    public bool _isBeingShinedByLight;
    public Transform SmartAIBody;
    
    [Header("Player Values")]
    public Camera PlayerCam;//PlayerCamera. Reference for any action that uses what the player can see.
    public GameObject Player; //The object that acts as the player. what smartAI will chase when the correct state is called
    public FlashLight flashlight;//References the player flashlight script
    public float minDistanceFromPlayerWhenStalking = 2f;


    [Header("AI Wandering Eunum Stuff")]
    public float radius = 10.0f; // The radius within which to pick a random point
    private Vector3 targetPoint;
    public float waitTime = 1.0f; // Time to wait before picking a new point
    public float redirectChance = 0.1f; // Chance to redirect to a new point mid-travel (0.0 to 1.0)
    public float noiseIntensity = 0.5f; // Intensity of the movement noise


    // Start is called before the first frame update
    void Start()
    {
        _isAiActive = true;
        
        beginWander();
        
        
        
    }


    public void beginWander()
    {
        currentState = States.Wandering;
       
    }

    // Update is called once per frame
    void Update()
    {
        
        aiDistance = Vector3.Distance(Player.transform.position, this.transform.position);
        if (PlayerCam == null)
        {
            Debug.LogError("Camera component not found on the player object!");
            return;
        }
        else
        {
            IsVisibleToPlayerCheck();
        }
        
       
    }
    private void IsVisibleToPlayerCheck()
    {

        //Runs a ray from the monster to the player, with the goal of checking to see if there is anything in the way of the monster having direct line of sight to the player.
        //pretty much, it checks both the player and the monster to see if both match for the monster to properly stop.

        bool isPathBlocked(Vector3 start, Vector3 end)
        {
            //Cast a ray from monster to player
            RaycastHit hit;
            if (Physics.Linecast(start, end, out hit))
            {
                if (hit.collider.isTrigger == false)
                {
                    return true; //path blocked
                   
                   
                }
               
            }
            return false;
        }

        if (isPathBlocked(SmartAIBody.position, Player.transform.position))
        {
            //pathblocked
            _isBlocked = true;
            
            Debug.DrawLine(SmartAIBody.position, PlayerCam.transform.position, Color.cyan, Time.fixedDeltaTime);
        }
        else
        {
            //pathclear
            _isBlocked = false;
            if (!_isViewed)
            {
                Debug.DrawLine(SmartAIBody.position, PlayerCam.transform.position, Color.red, Time.fixedDeltaTime);
            }
            else
            {
                Debug.DrawLine(SmartAIBody.position, PlayerCam.transform.position, Color.green, Time.fixedDeltaTime);
            }
        }



        //Calculate frustum planes
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(PlayerCam);

        //Get the AI distance from player
       aiDistance = Vector3.Distance(SmartAIBody.position, Player.transform.position);

        //If the AI is in the playerCam view,
        if (_isBlocked == false)
        {
            if (GeometryUtility.TestPlanesAABB(planes, this.gameObject.GetComponent<Renderer>().bounds))
            {
                _isViewed = true;
                
                if (flashlight.isOn && _isViewed == true)
                {
                    _isBeingShinedByLight = true;
                }
                else
                {
                    _isBeingShinedByLight = false;
                }
               
            }
           
        }

       
        //If the AI isn't in the player's Camera's view,

        if (!GeometryUtility.TestPlanesAABB(planes, this.gameObject.GetComponent<Renderer>().bounds))
        {
            _isViewed = false;

             
          
        }
        else
        {
            return;
        }




    }

    //Random point for wandering state
    void PickRandomPoint()
    {
        // Generate a random point within a circle
        Vector2 randomPoint = Random.insideUnitCircle * radius;
        targetPoint = new Vector3(transform.position.x + randomPoint.x, transform.position.y, transform.position.z + randomPoint.y);

        // Check if the point is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            targetPoint = hit.position;
            ai.SetDestination(targetPoint);
        }
    }

    //Unpredictability to movement.
    void AddNoiseToMovement()
    {
        // Add small random noise to the agent's destination
        Vector3 noise = new Vector3(
            Random.Range(-noiseIntensity, noiseIntensity),
            0,
            Random.Range(-noiseIntensity, noiseIntensity)
        );

        ai.SetDestination(ai.destination + noise);
    }

    //gizmo Draw for testing
    void OnDrawGizmos()
    {
        // Draw the radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw the target point
        if (ai != null && ai.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPoint, 0.5f);
        }
    }

    


}

