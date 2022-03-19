using System.Collections;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{


    private Vector3 origPos, targetPos;
    private BoxCollider2D boxCollider;
    public string thisSquare;
    [SerializeField] private LayerMask colliderlayerMask;
    [SerializeField] float timeToMove = 0.5f;
    [SerializeField] AudioClip success;
    [SerializeField] AudioClip hitnumber;
    [SerializeField] AudioClip hitlocked;
    [SerializeField] AudioClip hitnormal;
    [SerializeField] AudioClip death;
    [SerializeField] AudioClip coin;
    [SerializeField] [Range(0f, 1f)] float nextSceneTimer = 1f;
    [SerializeField] float playerJumpHeight = 2.2f;
    public int WinStatus = 0;
    public bool isMoving;
    public bool firstMove = false;
    private bool LeftCollision;
    private bool RightCollision;
    private bool playerAlive = true;
    private bool isMovingArc;
    public bool hitNumber = false;
    AudioSource audioSource;
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        Movement();          // Square Movement
        CastCollisionRays(); // Checks player collision with another square in the x axis
        GameFlow();

    }

    //Input
    private void Movement()
    {
        if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) && !isMoving && playerAlive)
        {
            //firstMove = true;
            if (!LeftCollision)
            {
                StartCoroutine(MovePlayer(Vector3.left * 2, 1));
            }
            else
            {
                StartCoroutine(MovePlayer((Vector3.left + Vector3.up) * 2, playerJumpHeight));
            }

        }

        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) && !isMoving && playerAlive)
        {
           // firstMove = true;
            if (!RightCollision)
            {
                StartCoroutine(MovePlayer(Vector3.right * 2, 1));
            }
            else
            {
                StartCoroutine(MovePlayer((Vector3.right + Vector3.up) * 2, playerJumpHeight));
            }
        }

    }
    //Movement
    private IEnumerator MovePlayer(Vector3 direction, float height)
    {
        firstMove = true;// moves for the first time
        isMoving = true; // this checks if player is affected by gravity, will turn false if player touch the ground
        isMovingArc = true;// this checks if player is on his parabolic movement
        hitNumber = false;
        float elapsedTime = 0;
        origPos = transform.position;
        targetPos = origPos + direction;// his fixed move location of 1 step at a time 

        while (elapsedTime < timeToMove)
        {
            //transform.position = Vector3.Lerp(origPos, targetPos, (elapsedTime / timeToMove));   // this makes the player move in a straight line
            transform.position = Parabola(origPos, targetPos, height, (elapsedTime / timeToMove)); // this makes the player move in an parabolic line 
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        isMovingArc = false; // player finished his parabolic movement
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject) 
        {
            isMoving = false;
        }

        if (collision.gameObject.tag == "Liquid")
        {
            audioSource.Stop();
            audioSource.PlayOneShot(death);
            playerAlive = false;
            Invoke("ResetCurrentLevel", 1f);
        }
        else if (collision.gameObject.tag == "Friendly" && firstMove && !isMoving)
        {
            audioSource.PlayOneShot(hitnormal);
            
        }
        else if (collision.gameObject.tag == "NumberSquare" && firstMove)
        {
            hitNumber = true;
            thisSquare = collision.gameObject.name;

            audioSource.Stop();
            audioSource.PlayOneShot(hitnumber);
            
        }
        else if (collision.gameObject.tag == "Locked" && firstMove)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(hitlocked);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "GoldCoin")
        {
            audioSource.Stop();
            audioSource.PlayOneShot(coin);
            Destroy(collision.gameObject);
        }
    }
    public static Vector2 Parabola(Vector2 start, Vector2 end, float height, float t)
    {
        Func<float, float> f = x => -4 * height * x * x + 4 * height * x; // parabolic ecuation

        var mid = Vector2.Lerp(start, end, t);                            // liniar interpolation, used to draw the line from startpos to endpos in time t

        return new Vector2(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t));  // return the moving behaviour to use in MovePlayer  
    }
    //Raycast
    private void CastCollisionRays()
    {
        float extraLenght = .1f;
        
        RaycastHit2D hitleft = Physics2D.Raycast(boxCollider.bounds.center, Vector2.left, (boxCollider.bounds.extents.x + extraLenght) * 3, colliderlayerMask);
        // draws a Ray to check for collisions left side of the player
        RaycastHit2D hitright = Physics2D.Raycast(boxCollider.bounds.center, Vector2.right, (boxCollider.bounds.extents.x + extraLenght) * 3, colliderlayerMask);
        // draws a Ray to check for collisions right side of the player
        RaycastHit2D hitdown = Physics2D.Raycast(boxCollider.bounds.center, Vector2.down, (boxCollider.bounds.extents.y + .1f), colliderlayerMask);
        //Color rayColorLeft;
        Color rayColor;
        if (hitdown.collider != null && !isMovingArc)       // player can only move if he is touching the ground
        {

            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
        


        ColorRays(extraLenght, ref hitleft, ref hitright, ref hitdown, out rayColor);
        CheckHorizontalRayCollisions(hitleft, hitright);
    }
    private void CheckHorizontalRayCollisions(RaycastHit2D hitleft, RaycastHit2D hitright)
    {
        if (hitleft.collider != null)
        {
            LeftCollision = true;
        }
        else
        {
            LeftCollision = false;
        }
        if (hitright.collider != null)
        {
            RightCollision = true;
        }
        else
        {
            RightCollision = false;
        }
    }
    // Scene related
    private void GameFlow()
    {
        if (WinStatus == 0)  // total sum of blocks in a certain level, if it's 0 pass to next level
        {
            GoToNextLevel();
        }

        else if (!playerAlive && WinStatus != 0) // if player falls or steps in liquid
        {
            Invoke("ResetCurrentLevel", nextSceneTimer);
        }
        if (Debug.isDebugBuild)
        {
            RespondToDebugKey();                // only in dev builds you can press L for instantly going to next level 
        }
        if (Input.GetKeyDown(KeyCode.R))        // restart level by pressing R
        {
            ResetCurrentLevel();
        }
    }

    private void GoToNextLevel()
    {
        playerAlive = false;        //disable movement
        Invoke("PlayerTransition", 0.5f);
        Invoke("LoadNextScene", nextSceneTimer);
    }
    private void ResetCurrentLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        if (nextSceneIndex == SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 1;
        }
        SceneManager.LoadScene(nextSceneIndex);
    }
    private void LoadLastScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex - 1;
        if (nextSceneIndex == 0)
        {
            nextSceneIndex = 1;
        }
        SceneManager.LoadScene(nextSceneIndex);
    }
    private void PlayerTransition()
    {
        gameObject.SetActive(false);
    }

    //Developer only
    private void ColorRays(float extraLenght, ref RaycastHit2D hitleft, ref RaycastHit2D hitright, ref RaycastHit2D hitdown, out Color rayColor)
    {
        if (hitdown.collider != null)
        {
            rayColor = Color.green;
        }
        else
        {
            rayColor = Color.red;
        }
        Debug.DrawRay(transform.position, Vector2.down * (boxCollider.bounds.extents.y +.1f), rayColor);
        if (hitleft.collider != null)
        {
            rayColor = Color.green;
        }
        else
        {
            rayColor = Color.red;
        }

        if (hitright.collider != null)
        {
            rayColor = Color.green;
        }
        else
        {
            rayColor = Color.red;
        }

        Debug.DrawRay(transform.position, Vector2.left * (boxCollider.bounds.extents.x + extraLenght) * 2, rayColor);
        Debug.DrawRay(transform.position, Vector2.right * (boxCollider.bounds.extents.x + extraLenght) * 2, rayColor);
    }       // this draws the Horizontal rays in debugger mode 
    private void RespondToDebugKey()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadNextScene();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            LoadLastScene();
        }

    }
}
