using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6.0f;
    Vector3 movement;
    Rigidbody rb;

    public float jumpForce = 8.0f; // 점프 힘
    public static bool isGrounded; // 땅에 닿았는지 여부
    public float jumpMaxY = 3.0f;
    public float jumpY = 0;
    public LayerMask jumpLayer;
    bool canJump = true;
    RaycastHit hit;
    Collider[] cols;

    public TextMeshProUGUI curHpText;
    public TextMeshProUGUI maxHpText;
    Coroutine reduceHp;
    bool isReduce = false;
    bool isGod = false;

    public static bool isDimension = false; // 1_1 차원 전환 아이템 소지 여부
    float maxDimensionGauge = 10.0f;
    public float curDimensionGauge = 10.0f;
    //public float preDimensionGauge = 10.0f;
    public static bool isChange = false;
    public LayerMask targetLayer; // 충돌을 감지할 레이어
    public float collisionThreshold = 1.0f; // 절반 이상 충돌되었다고 판단할 기준값

    //public GameObject dimensionGaugeParent;
    //public GameObject dimensionGaugePrefab;
    public Slider dimensionGaugeSlider;

    public TextMeshProUGUI scoreText;

    bool is2D = false;
    public Vector3 wallPos;
    public Vector3 targetPos;
    bool isWallCenter = false;

    public Animator animator;
    //float rotateSpeed = 20.0f;
    public GameObject playerSkinPrefab;

    AudioSource audioSource;

    public AudioClip footStepSound;
    public float footstepInterval = 1.5f;
    private float lastFootstepTime;

    public AudioClip jumpSound;
    public AudioClip hitSound;
    public AudioClip overDimensionSound;
    bool isFall = false;
    public AudioClip fallSound;
    float lastFallTime;
    float fallInterval = 1.5f;

    bool isGameOver = false;

    void Start()
    {
        GameManager.inputEnabled = false;

        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        transform.GetChild(2).localRotation = Quaternion.Euler(0, 90, 0);
        animator.SetTrigger("isStart");

        if (GameManager.level > 2)
            isDimension = true;

        //for (int i = 0; i < (int)maxDimensionGauge; i++)
        //{
        //    GameObject gauge = Instantiate(dimensionGaugePrefab);
        //    gauge.transform.parent = dimensionGaugeParent.transform;
        //}

        TextInit();
    }

    void TextInit()
    {
        curHpText = GameObject.Find("CurHP").GetComponent<TextMeshProUGUI>();
        maxHpText = GameObject.Find("MaxHP").GetComponent<TextMeshProUGUI>();
        dimensionGaugeSlider = GameObject.Find("DimensionGaugeSlider").GetComponent<Slider>();
        scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        //Debug.Log("ypos : " + transform.position.y);
        //Debug.Log("velo : " + rb.velocity.magnitude);
        if (curHpText == null)
        {
            TextInit();
        }

        if (GameManager.inputEnabled)
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                jumpY = transform.position.y;
                Jump(jumpForce);
            }
        }

        if (GameManager.instance.GetCurHp() <= 0)
        {
            GameManager.instance.SetCurHp(0);
            Debug.Log("게임종료");
            if (!isGameOver)
            {
                GameManager.instance.GameOver();
                isGameOver = true;
            }
            //return;
        }

        if (GameManager.instance.GetCurHp() > GameManager.instance.GetMaxHp())
            GameManager.instance.SetCurHp(GameManager.instance.GetMaxHp());

        if (isGod)
            gameObject.layer = 8;
        else
            gameObject.layer = 7;

        // 추락
        // 디버그를 찍어보니 transform.position.y가 -10 이하가 돼도
        // 그냥 계속 떨어지는 현상이 간혹 발생한다. 이유를 모르겠다. 너무 빨라서 그런가?
        // 진짜 너무 빨라서 그런거였네.
        //if (transform.position.y < -12.0f || rb.velocity.magnitude > 13.0f)
        //{
        //    transform.position = new Vector3(0, -4.1f, 0);
        //}

        // 연속 추락은 그냥 로직 순서 문제였구나. 인터벌도 필요하긴했다.
        if (!isFall && transform.position.y < -10.0f && Time.time - lastFallTime > fallInterval)
        {
            isFall = true;
            lastFallTime = Time.time;
        }

        if (isFall)
        {
            PlayerFall();
        }

        if (isReduce)
        {
            ReduceHpAndResetDimensionGauge();
        }

        if (GameManager.inputEnabled && movement.magnitude != 0 && isGrounded)
        {
            if (Time.time - lastFootstepTime > footstepInterval)
            {
                //AudioSource.PlayClipAtPoint(footStepSound, transform.position);
                audioSource.PlayOneShot(footStepSound);
                lastFootstepTime = Time.time;
            }
        }

        curHpText.text = GameManager.instance.GetCurHp().ToString();
        maxHpText.text = GameManager.instance.GetMaxHp().ToString();
        if (isDimension)
            dimensionGaugeSlider.fillRect.GetComponent<Image>().color = Color.red;
        else
            dimensionGaugeSlider.fillRect.GetComponent<Image>().color = Color.black;
        dimensionGaugeSlider.value = curDimensionGauge;
        scoreText.text = "SCORE : " + GameManager.instance.GetScore();
    }

    void PlayerFall()
    {
        Debug.Log("추락");

        audioSource.PlayOneShot(fallSound);
        GameManager.instance.AddCurHp(-3);

        animator.SetTrigger("isDrop");
        animator.SetFloat("Move", 0);
        StartCoroutine(InputDelayAndToggleGod(2.0f));
        isFall = false;
    }

    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        // Rigidbody의 속도가 매우 빠른 경우에는
        // 한 프레임 동안의 이동 거리가 너무 커서 Update 함수가 호출되지 않을 수 있다. 
        if (transform.position.y < -12.0f)
        {
            transform.position = new Vector3(0, -4.1f, 0);
        }

        CheckIsGrounded();
        // 바닥에 스치기만해도 점프끝남 - Ray수정
        animator.SetBool("isGrounded", isGrounded);

        // 연속으로 차원 전환하는게 좀 이상한데 몰루
        if (CameraMove.mainCam.orthographic) // 2D일때, 3D에서 2D로 갈때
        {
            Debug.Log("if문 3D에서 2D로");

            //if (CheckOverlap())
            //{
            //    Debug.Log("겹치면않되");
            //    // 대충 피깎고 다시 전환하는 기능
            //}

            if (isChange)
            {
                animator.SetTrigger("isChange");
                animator.SetFloat("Move", 0);
                transform.GetChild(2).localRotation = Quaternion.Euler(0, 90, 0);
                StartCoroutine(DimensionGaugeChange(2.0f));
            }

            isWallCenter = false;

            // 2초뒤에 x좌표를 0으로 옮겨서 가운데에서만 이동하게끔
            if (is2D)
                StartCoroutine(MoveToCenterWithDelay(1.9f));

            // Orthographic 2D일 때는 좌우키로 z축 이동
            movement = new Vector3(0f, 0f, moveHorizontal).normalized;

            // 애니메이션
            if (GameManager.inputEnabled)
            {
                animator.SetFloat("Move", Mathf.Abs(moveHorizontal));

                // 깡으로 회전넣음
                if (moveHorizontal < 0)
                    transform.GetChild(2).localRotation = Quaternion.Euler(0, 180, 0);
                else if (moveHorizontal > 0)
                    transform.GetChild(2).localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
        else // 3D일때, 2D에서 3D로 갈때
        {
            Debug.Log("else문 2D에서 3D로");

            if (isChange)
            {
                animator.SetTrigger("isChange");
                animator.SetFloat("Move", 0);
                transform.GetChild(2).localRotation = Quaternion.Euler(0, 180, 0);
                StartCoroutine(DimensionGaugeChange(2.0f));
            }

            is2D = true;

            if (!isWallCenter)
                MoveToWallCenter();

            // Perspective 3D일 때는 상하키로 z축, 좌우키로 x방향 이동
            movement = new Vector3(moveHorizontal, 0f, moveVertical).normalized;

            // 애니메이션
            if (GameManager.inputEnabled)
            {
                animator.SetFloat("Move", movement.magnitude);

                // 깡으로 회전넣음
                if (movement.magnitude != 0)
                    transform.GetChild(2).forward = movement;
                //transform.GetChild(2).forward = Vector3.Lerp(transform.GetChild(2).forward, movement, rotateSpeed * Time.deltaTime);
            }
        }

        if (GameManager.inputEnabled)
        {
            //transform.Translate(movement * moveSpeed * Time.deltaTime);
            //rb.velocity = movement * moveSpeed;
            // Translate를 쓰면 트리거 충돌이 2번씩 되고 velocity를 쓰면 이동이 끊기고 점프가 안되네
            // https://rito15.github.io/posts/unity-fixed-update-and-stuttering/
            // 프레임 설정 문제였다?
            // 그냥 마리오처럼 가속을 받는게 좋을까?
            // 나는 그냥 이동하는게 좋은데.

            //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            //{
            //    jumpY = transform.position.y;
            //    Jump(jumpForce);
            //}

            //if (transform.position.y > jumpMaxY + jumpY)
            //    rb.velocity = new Vector3(rb.velocity.x, -jumpForce / 2, rb.velocity.z);

            //Vector3 moveDir = new Vector3(movement.x, 0, movement.z);
            //movement.y = 0;
            //rb.velocity = movement * moveSpeed * Time.fixedDeltaTime;

            //rb.velocity = new Vector3(movement.x * moveSpeed,
            //                          rb.velocity.y,
            //                          movement.z * moveSpeed);

            // 왜 갑자기 벽이 전부 뚫리는거지?
            // https://blog.joe-brothers.com/unity-how-to-prevent-player-going-through-walls-using-raycast/ 이런거라도?
            rb.MovePosition(rb.position + new Vector3(movement.x * moveSpeed * Time.deltaTime,
                            rb.velocity.y * Time.deltaTime,
                            movement.z * moveSpeed * Time.deltaTime));

            //rb.AddForce(movement, ForceMode.VelocityChange);
        }
    }

    public void Jump(float force)
    {
        //transform.Translate(Vector3.up * jumpForce * Time.deltaTime);
        //transform.Translate(0, jumpForce * Time.deltaTime, 0);
        //if (transform.position.y > jumpMaxY + jumpY)
        //    transform.Translate(0, -jumpForce * Time.deltaTime, 0);

        //rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        //rb.velocity = new Vector3(0f, jumpForce, 0f);
        //rb.velocity = new Vector3(rb.velocity.x, force, rb.velocity.z);
        //rb.velocity = Vector3.up * force;

        //rb.MovePosition(rb.position + Vector3.up * force * Time.deltaTime);
        if (canJump)
        {
            rb.AddForce(new Vector3(0, force, 0f));
            //AudioSource.PlayClipAtPoint(jumpSound, transform.position);
            audioSource.PlayOneShot(jumpSound);
            isGrounded = false;
            canJump = false;
            StartCoroutine(JumpCool(0.5f));
        }

    } 

    IEnumerator JumpCool(float cool)
    {
        yield return new WaitForSecondsRealtime(cool);
        canJump = true;
    }

    //IEnumerator DimensionGaugeChange(float delay)
    //{
    //    isChange = false;
    //    Debug.Log("차원 전환 게이지 변화중");

    //    if (CameraMove.mainCam.orthographic)
    //    {
    //        yield return new WaitForSecondsRealtime(delay);
    //        if (curDimensionGauge < 10.0f)
    //        {
    //            curDimensionGauge += Time.fixedDeltaTime;
    //            //curDimensionGauge = Mathf.Min(curDimensionGauge, 10.0f);
    //        }
    //        if (curDimensionGauge >= 10.0f)
    //            curDimensionGauge = 10.0f;
    //    }
    //    else
    //    {
    //        yield return new WaitForSecondsRealtime(delay);
    //        if (curDimensionGauge > 0)
    //        {
    //            curDimensionGauge -= Time.fixedDeltaTime;
    //            //curDimensionGauge = Mathf.Max(curDimensionGauge, 0.0f);
    //        }
    //        if (curDimensionGauge <= 0)
    //            curDimensionGauge = 0;

    //    }
    //}

    IEnumerator DimensionGaugeChange(float delay)
    {
        isChange = false;
        Debug.Log("차원 전환 게이지 변화중");

        yield return new WaitForSecondsRealtime(delay); // 딜레이 만큼 대기

        //DimensionGaugeCount();

        if (CameraMove.mainCam.orthographic)
        {
            while (true)
            {
                yield return null;
                curDimensionGauge += Time.deltaTime * 2;
                Debug.Log("차원 전환 게이지 증가중");

                if (!CameraMove.mainCam.orthographic)
                    break;

                if (curDimensionGauge >= maxDimensionGauge)
                {
                    curDimensionGauge = maxDimensionGauge;
                    break;
                }
            }
        }
        else
        {
            while (true)
            {
                yield return null;
                curDimensionGauge -= Time.deltaTime;
                Debug.Log("차원 전환 게이지 감소중");

                if (CameraMove.mainCam.orthographic)
                    break;

                if (curDimensionGauge <= 0)
                {
                    curDimensionGauge = 0;
                    isReduce = true;
                    //break;
                }
            }
        }

    }
    
    public void DimensionGaugeCount()
    {
        //if (curDimensionGauge != preDimensionGauge)
        //{
        //    if (curDimensionGauge < preDimensionGauge)
        //    {
        //        int countToRemove = (int)preDimensionGauge - (int)curDimensionGauge;
        //        for (int i = 0; i < countToRemove; i++)
        //        {
        //            if (dimensionGaugeParent.transform.childCount > 0)
        //            {
        //                GameObject lastChild = dimensionGaugeParent.transform.GetChild(dimensionGaugeParent.transform.childCount - 1).gameObject;
        //                Destroy(lastChild);
        //            }
        //        }
        //    }
        //    else if (curDimensionGauge > preDimensionGauge)
        //    {
        //        int countToAdd = (int)curDimensionGauge - (int)preDimensionGauge;
        //        for (int i = 0; i < countToAdd; i++)
        //        {
        //            GameObject gauge = Instantiate(dimensionGaugePrefab);
        //            gauge.transform.parent = dimensionGaugeParent.transform;
        //        }
        //    }

        //    preDimensionGauge = curDimensionGauge;
        //}
    }

    IEnumerator ReduceHp(float delay)
    {
        isReduce = false;
        while (true)
        {
            yield return new WaitForSecondsRealtime(delay);
            Debug.Log("HP 감소");
            GameManager.instance.AddCurHp(-1);
        }
    }

    public void ReduceHpAndResetDimensionGauge()
    {
        GameManager.instance.AddCurHp(-1);
        audioSource.PlayOneShot(overDimensionSound);
        curDimensionGauge = maxDimensionGauge / 2;
        isReduce = false;
    }

    IEnumerator MoveToCenterWithDelay(float delay)
    {
        Debug.Log("3D에서 2D로");
        is2D = false;
        yield return new WaitForSecondsRealtime(delay); // 딜레이 만큼 대기
        transform.position = new Vector3(0.0f, transform.position.y, transform.position.z); // 중앙으로 이동
    }

    void MoveToWallCenter()
    {
        Debug.Log("2D에서 3D로");
        Debug.Log("가야할 x좌표 " + wallPos.x);
        
        targetPos = new Vector3(wallPos.x, transform.position.y, transform.position.z); // Wall 중앙으로 이동
        
        Debug.Log("타겟 " + targetPos);

        transform.position = targetPos;

        isWallCenter = true;
    }

    // 무한충돌문제가 간혹 생기는데 몰루 - 몬스터에 공격주기 넣어서 해결
    IEnumerator InputDelayAndToggleGod(float delay)
    {
        GameManager.inputEnabled = false;
        yield return new WaitForSecondsRealtime(delay); // 입력 딜레이 시간만큼 대기

        //무적모드온
        ToggleGod();
        GameManager.inputEnabled = true; // 입력 활성화
        yield return new WaitForSecondsRealtime(delay * 2.0f);
        //무적모드오프
        ToggleGod();

        //Time.timeScale = 1.0f;
    }

    public void ToggleGod()
    {
        isGod = !isGod;

        // 뭔가 깜빡이는 효과?
        if (isGod)
        {
            InvokeRepeating(nameof(ToggleBlink), 0, 0.1f);
        }
        else
        {
            CancelInvoke(nameof(ToggleBlink));
            playerSkinPrefab.SetActive(true);
        }
    }

    void ToggleBlink()
    {
        playerSkinPrefab.SetActive(!playerSkinPrefab.activeSelf);
    }

    public void HitMonster(float time)
    {
        animator.SetTrigger("Hit");
        //AudioSource.PlayClipAtPoint(hitSound, transform.position);
        audioSource.PlayOneShot(hitSound);
        StartCoroutine(InputDelayAndToggleGod(time));
    }

    private void CheckIsGrounded()
    {
        BoxCollider playerCollider = GetComponent<BoxCollider>();

        Vector3[] raycastOrigins = new Vector3[9];
        float offsetX = 0.14f;
        float offsetZ = 0.16f;
        bool isRay = false;

        // 3D 기준
        raycastOrigins[0] = transform.position + new Vector3(offsetX, 0.0f, offsetZ); // 앞 오른쪽 꼭지점
        raycastOrigins[1] = transform.position + new Vector3(0.0f, 0.0f, offsetZ); // 앞 가운데 꼭지점
        raycastOrigins[2] = transform.position + new Vector3(-offsetX, 0.0f, offsetZ); // 앞 왼쪽 꼭지점

        raycastOrigins[3] = transform.position + new Vector3(offsetX, 0.0f, 0.0f); // 가운데 오른쪽 꼭지점
        raycastOrigins[4] = transform.position; // 가운데 꼭지점
        raycastOrigins[5] = transform.position + new Vector3(-offsetX, 0.0f, 0.0f); // 가운데 왼쪽 꼭지점

        raycastOrigins[6] = transform.position + new Vector3(offsetX, 0.0f, -offsetZ); // 뒤 오른쪽 꼭지점
        raycastOrigins[7] = transform.position + new Vector3(0.0f, 0.0f, -offsetZ); // 뒤 가운데 꼭지점
        raycastOrigins[8] = transform.position + new Vector3(-offsetX, 0.0f, -offsetZ); // 뒤 왼쪽 꼭지점

        //float raycastDistance = playerCollider.size.y / 2f;
        float raycastDistance = 0.42f;

        for (int i = 0; i < raycastOrigins.Length; i++)
            Debug.DrawRay(raycastOrigins[i], Vector3.down * raycastDistance, Color.black);

        foreach (Vector3 origin in raycastOrigins)
        {
            if (Physics.Raycast(origin, Vector3.down, raycastDistance, jumpLayer))
            {
                isGrounded = true;
                isRay = true;
                break;
            }
        }

        if (!isRay)
        {
            isGrounded = false;
        }

        //Debug.DrawRay(transform.position + offset, Vector3.down * raycastDistance, Color.black);
        //Debug.DrawRay(transform.position - offset, Vector3.down * raycastDistance, Color.black);

        // 점프하고 착지할때 모서리로 착지하면 false로 못가고 그냥 모서리로 바로 가도 true
        //if (Physics.Raycast(transform.position, Vector3.down, raycastDistance, jumpLayer))
        //    isGrounded = true;
        //else
        //    isGrounded = false;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            wallPos.x = 0;
            isWallCenter = true;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            // 2개 이상의 Wall에 닿을때 문제가 있긴함
            wallPos = collision.gameObject.GetComponent<Wall>().GetWallOriPos();
        }

        if (collision.gameObject.CompareTag("Block"))
        {
            wallPos.x = 0;
            isWallCenter = true;
        }

        // 안된다.
        // 충돌한 오브젝트가 특정 레이어에 속하는지 확인
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            // 충돌 지점을 확인
            ContactPoint contactPoint = collision.contacts[0];
            // 충돌 지점과 플레이어의 중심 사이의 거리를 계산
            float distanceToCenter = Vector3.Distance(contactPoint.point, transform.position);
            // 플레이어의 BoxCollider의 절반 크기 계산
            Vector3 boxColliderHalfExtents = GetComponent<BoxCollider>().size / 2f;

            // 절반 이상 충돌되었는지 확인
            if (distanceToCenter > boxColliderHalfExtents.magnitude * collisionThreshold)
            {
                Debug.Log("플레이어의 BoxCollider와 절반 이상 충돌되었습니다.");
                // 이후 원하는 처리를 수행
            }
        }
    }

}
