﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;

public class PlayerController : MonoBehaviour
{

    public bool jump = false;
    public bool slide = false;
    public bool run = false;
    public bool left = false;
    public bool right = false;
    private bool isGrounded = true;
    public bool dead = false;
    public bool boost = false;
    public bool gameStarted = false;
    public bool gamePaused = false;

    public Animator animator;
    private Rigidbody rigidBody;
    public GameObject trigger;
    public LayerMask Ground;
    public Transform groundChecker;
    public PlayerMetadata playerMetadata;
    public GameObject fire;
    Vector3 targetXPosition;

    public GameObject boostAbilityCanvas;
    public Image boostAbilityCounter;

    public float jumpHeight = 1f;
    public float leftRightForce = 2f;
    public float groundDistance = 0.2f;
    public float speed = 8f;
    public float speedIncrement = 0.2f;
    public float animationSpeedIncrement = 0.007f;
    public float currentScore = 0;
    public float currentCoins = 0;
    public float recordScore = 0;
    public float speedAndScoreMultiplier = 1;
    public float boostMultiplier = 5;
    public float defaultMultiplier = 1;
    public float boostDuration = 5;
    public float coinValue = 1f;
    public float horizontalSpeed = 5.5f;
    public float leftRightMovement = 1f;

    [SerializeField] Text specialCoinText;

    public List<PlayerMetadata> ranking = new List<PlayerMetadata>();

    bool AnimatorIsPlaying(Animator animator, int layerIndex, string stateName)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    float AnimatorProgressPercentage(Animator animator, int layerIndex, string stateName)
    {
        return (animator.GetCurrentAnimatorStateInfo(0).normalizedTime / animator.GetCurrentAnimatorStateInfo(0).length) * 100;
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        groundChecker = GetComponent<Transform>();
        currentCoins = Mathf.Round(PlayerPrefs.GetFloat(GamePrefs.Keys.COINS_AMNT, 0));
        recordScore = Mathf.Round(PlayerPrefs.GetFloat(GamePrefs.Keys.CURRENT_MAP_RECORD_SCORE, 0));
        playerMetadata = new PlayerMetadata(currentCoins, currentScore);

        Messenger.AddListener(GameEvent.BEGIN_GAME, beginGame);
        Messenger.AddListener(GameEvent.UNPAUSE, UnPauseGame);
        Messenger.AddListener(GameEvent.PAUSE, PauseGame);

        targetXPosition = transform.position;
    }

    void PauseGame() {
        gamePaused = true;
    }

    void UnPauseGame()
    {
        gamePaused = false;
        if (gameStarted)
            Messenger.Broadcast(GameEvent.PLAY_AND_SCHEDULE_RUNNING_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
    }


    void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.BEGIN_GAME,beginGame);
        Messenger.RemoveListener(GameEvent.UNPAUSE, UnPauseGame);
        Messenger.RemoveListener(GameEvent.PAUSE, PauseGame);
    }

    void beginGame() {
        animator.SetBool("isRun", true);
        Messenger.Broadcast(GameEvent.PLAY_AND_SCHEDULE_RUNNING_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
        gameStarted = true;
    }

    void updatePlayerMetadata() {
        playerMetadata.update(Mathf.Round(currentCoins), Mathf.Round(currentScore));
        Messenger<PlayerMetadata>.Broadcast(GameEvent.UPDATE_METADATA, playerMetadata,MessengerMode.DONT_REQUIRE_LISTENER);
    }

    void playAudio() {
        // if jump or slide animation is in progress, stop running audio
        if (( AnimatorIsPlaying(animator,0,"BaseLayer.Jump") || AnimatorIsPlaying(animator, 0, "BaseLayer.Running Slide")) && AnimatorProgressPercentage(animator,0, "BaseLayer.Jump")<80)
        {
            Messenger.Broadcast(GameEvent.PAUSE_RUNNING_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        else
        {
            Messenger.Broadcast(GameEvent.UNPAUSE_RUNNING_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
        }

    }

    void checkDead()
    {
        if (dead == true)
        {
            Messenger.Broadcast(GameEvent.PLAY_COLLISION_OBJECT, MessengerMode.DONT_REQUIRE_LISTENER);
            // wait
            Messenger.Broadcast(GameEvent.STOP_RUNNING_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
            Messenger<PlayerMetadata>.Broadcast(GameEvent.STORE_RANKING, playerMetadata, MessengerMode.DONT_REQUIRE_LISTENER);
            speed = 0;
            animator.Play("Hit To The Legs");
            gamePaused = true;
            Messenger.Broadcast(GameEvent.ACTIVATE_FRONT_CAMERA, MessengerMode.DONT_REQUIRE_LISTENER);
            StartCoroutine(LoadGameOverWithDelay());
        }
    }

    IEnumerator LoadGameOverWithDelay()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("GameOverMenu");
    }

    void checkBoost()
    {
        if (boost == true)
        {
            boostAbilityCounter.fillAmount -= 1f / boostDuration * Time.deltaTime;
        }
    }

    void processInput()
    {

        isGrounded = Physics.CheckSphere(groundChecker.position, groundDistance, Ground, QueryTriggerInteraction.Ignore);

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            jump = true;
            Messenger.Broadcast(GameEvent.PLAY_JUMP_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
            animator.Play("Jump");
        }
        else
        {
            jump = false;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            slide = true;
            Messenger.Broadcast(GameEvent.PLAY_SLIDE_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
            animator.Play("Running Slide");
        }
        else
        {
            slide = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            left = true;
        }
        else
        {
            left = false;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            right = true;
        }
        else
        {
            right = false;
        }

        animator.SetBool("isJump", jump);
        animator.SetBool("isSlide", slide);

        if (jump == true && isGrounded)
        {
            rigidBody.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
        }


        if (slide == true && !isGrounded)
        {
            rigidBody.AddForce(Vector3.down * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
        }

        if (left)
        {
            // rigidBody.AddForce(Vector3.left * Mathf.Sqrt(leftRightForce * -2f * Physics.gravity.y), ForceMode.VelocityChange);           
            // transform.Translate(-9f * Time.deltaTime, 0, 0);
            targetXPosition.x += -leftRightMovement;
        }

        if (right)
        {
            // rigidBody.AddForce(Vector3.right * Mathf.Sqrt(leftRightForce * -2f * Physics.gravity.y), ForceMode.VelocityChange);
            // transform.Translate(9f*Time.deltaTime, 0, 0);
            targetXPosition.x += leftRightMovement;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (PlayerPrefs.GetInt(GamePrefs.Keys.SHOP_SPECIAL_COIN_AMNT) > 0 && boost == false)
            {
                PlayerPrefs.SetInt(GamePrefs.Keys.SHOP_SPECIAL_COIN_AMNT, PlayerPrefs.GetInt(GamePrefs.Keys.SHOP_SPECIAL_COIN_AMNT) - 1);
                StartCoroutine(Boost());
            }
        }
    }

    void moveForward()
    {
        // move character
        transform.Translate(0, 0, speed * Time.deltaTime* speedAndScoreMultiplier);
    }

    void graduallyMoveLeftAndRight() {
        targetXPosition.y = transform.position.y;
        targetXPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, targetXPosition, horizontalSpeed * Time.deltaTime);
    }

    void updateScore()
    {
        currentScore += 5 * Time.deltaTime* speedAndScoreMultiplier;
        specialCoinText.text = "x" + PlayerPrefs.GetInt(GamePrefs.Keys.SHOP_SPECIAL_COIN_AMNT);
    }

    // Update is called once per frame
    void Update()
    {

        if (!gameStarted || gamePaused)
            return;

        updatePlayerMetadata();

        checkDead();

        playAudio();

        graduallyMoveLeftAndRight();

        updateScore();

        moveForward();

        processInput();

        checkBoost();
    }

    void OnTriggerEnter(Collider other) {

        if (other.gameObject.tag == "Player") {
            Destroy(trigger.gameObject);
        }

        if (other.gameObject.tag == "Coin") {
            Destroy(other.gameObject,0.5f);
            currentCoins += coinValue;
            speed += speedIncrement;
            animator.speed += animationSpeedIncrement;
        }

        if (other.gameObject.tag == "Obstacle" && !boost)
        {
            dead = true;
        }

        if (other.gameObject.tag == "Boost")
        {
            Destroy(other.gameObject, 0.5f);
            StartCoroutine(Boost());            
        }

    }

    IEnumerator Boost()
    {

        boost = true;
        fire.SetActive(true);
        speedAndScoreMultiplier = boostMultiplier;

        boostAbilityCounter.fillAmount = 1;
        boostAbilityCanvas.SetActive(true);
        boostAbilityCounter.enabled = true;

        yield return new WaitForSeconds(boostDuration);

        speedAndScoreMultiplier = defaultMultiplier;
        fire.SetActive(false);
        boostAbilityCounter.enabled = false;
        boostAbilityCanvas.SetActive(false);

        // wait another 1s with normal speed, so player can resume normal game
        yield return new WaitForSeconds(2);
        boost = false;
    }
}
