using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    GameManager gameManager;

    public float moveSpeed = 6.0f;
    Vector3 movement;
    Rigidbody rb;

    public float jumpForce = 8.0f; // 점프 힘
    public static bool isGrounded; // 땅에 닿았는지 여부
    public float jumpMaxY = 3.0f;
    public float jumpY = 0;
    RaycastHit hit;
    Collider[] cols;

    public int curHp = 10;
    public int maxHp = 10;
    public TextMeshProUGUI curHpText;
    public TextMeshProUGUI maxHpText;
    Coroutine reduceHp;
    bool isReduce = false;

    public static bool isDimension = false; // 1_1 차원 전환 아이템 소지 여부
    float maxDimensionGauge = 10.0f;
    public float curDimensionGauge = 10.0f;
    public float preDimensionGauge = 10.0f;
    public static bool isChange = false;

    //public GameObject dimensionGaugeParent;
    //public GameObject dimensionGaugePrefab;
    public Slider dimensionGaugeSlider;

    int score = 0;
    public TextMeshProUGUI scoreText;

    bool is2D = false;
    public Vector3 wallPos;
    public Vector3 targetPos;
    bool isWallCenter = false;

    //public static PlayerMovement instance;

    //private void Awake()
    //{
    //    if (instance == null)
    //        instance = this;
    //    else
    //        Destroy(gameObject);
    //}

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody>();

        //if (GameManager.level > 2)
            isDimension = true;

        //for (int i = 0; i < (int)maxDimensionGauge; i++)
        //{
        //    GameObject gauge = Instantiate(dimensionGaugePrefab);
        //    gauge.transform.parent = dimensionGaugeParent.transform;
        //}

        curHpText = GameObject.Find("CurHP").GetComponent<TextMeshProUGUI>();
        maxHpText = GameObject.Find("MaxHP").GetComponent<TextMeshProUGUI>();
        dimensionGaugeSlider = GameObject.Find("DimensionGaugeSlider").GetComponent<Slider>();
        scoreText = GameObject.Find("Score").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        IsGrounded();

        if (curHp <= 0)
        {
            curHp = 0;
            Debug.Log("게임종료");
            //return;
        }
        if (curHp > maxHp)
            curHp = maxHp;

        // 연속으로 차원 전환하는게 좀 이상한데 몰루
        if (CameraMove.mainCam.orthographic) // 2D일때, 3D에서 2D로 갈때
        {
            Debug.Log("if문 3D에서 2D로");

            if (isChange)
                StartCoroutine(DimensionGaugeChange(2.0f));

            isWallCenter = false;

            // 2초뒤에 x좌표를 0으로 옮겨서 가운데에서만 이동하게끔
            if (is2D)
                StartCoroutine(MoveToCenterWithDelay(2.0f));

            // Orthographic 2D일 때는 좌우키로 z축 이동
            movement = new Vector3(0f, 0f, moveHorizontal).normalized;
        }
        else // 3D일때, 2D에서 3D로 갈때
        {
            Debug.Log("else문 2D에서 3D로");

            if (isChange)
                StartCoroutine(DimensionGaugeChange(2.0f));

            is2D = true;

            if (!isWallCenter)
                MoveToWallCenter();

            // Perspective 3D일 때는 상하키로 z축, 좌우키로 x방향 이동
            movement = new Vector3(moveHorizontal, 0f, moveVertical).normalized;
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

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                jumpY = transform.position.y;
                Jump(jumpForce);
            }

            //if (transform.position.y > jumpMaxY + jumpY)
            //    rb.velocity = new Vector3(rb.velocity.x, -jumpForce / 2, rb.velocity.z);
        }

        if (isReduce)
        {
            //reduceHp = StartCoroutine(ReduceHp(1.0f));
            ReduceHpAndResetDimensionGauge();
        }

        //if (CameraMove.mainCam.orthographic && reduceHp != null)
        //{
        //    Debug.Log("HP감소멈춰");
        //    StopCoroutine(reduceHp);
        //    reduceHp = null;
        //}

        curHpText.text = curHp.ToString();
        maxHpText.text = maxHp.ToString();
        dimensionGaugeSlider.value = curDimensionGauge;
        scoreText.text = "SCORE : " + score;
    }

    void FixedUpdate()
    {
        if (GameManager.inputEnabled)
        {
            //Vector3 moveDir = new Vector3(movement.x, 0, movement.z);
            //movement.y = 0;
            //rb.velocity = movement * moveSpeed * Time.fixedDeltaTime;

            //rb.velocity = new Vector3(movement.x * moveSpeed,
            //                          rb.velocity.y,
            //                          movement.z * moveSpeed);

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
        rb.AddForce(new Vector3(0, force, 0f));

        isGrounded = false;
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

        DimensionGaugeCount();

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
            curHp--;
        }
    }

    public void ReduceHpAndResetDimensionGauge()
    {
        curHp--;
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

    public void AddScore(int score)
    {
        this.score += score;
    }

    public void AddCurHp(int hp)
    {
        this.curHp += hp;
    }

    public void AddMaxHp(int hp)
    {
        this.maxHp += hp;
    }

    IEnumerator InputDelay(float delay)
    {
        GameManager.inputEnabled = false;
        yield return new WaitForSecondsRealtime(delay); // 입력 딜레이 시간만큼 대기
        GameManager.inputEnabled = true; // 입력 활성화

        //무적모드온
        //ToggleGod();
        //yield return new WaitForSecondsRealtime(delay);
        //무적모드오프
        //ToggleGod();

        //Time.timeScale = 1.0f;
    }

    public void ToggleGod()
    {

    }

    private void IsGrounded()
    {
        //Vector3 dir = transform.TransformDirection(Vector3.down) * 0.45f;
        //Debug.DrawRay(transform.position, dir, Color.yellow);

        // 점프하고 착지할때 모서리로 착지하면 false로 못가고 그냥 모서리로 바로 가도 true
        if (Physics.Raycast(transform.position, Vector3.down, 0.45f))
            isGrounded = true;
        else
            isGrounded = false;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            //isGrounded = true;
            isWallCenter = true;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            //isGrounded = true;
            // 2개 이상의 Wall에 닿을때 문제가 있긴함
            wallPos = collision.gameObject.GetComponent<Wall>().GetWallOriPos();
        }

        if (collision.gameObject.CompareTag("Block"))
        {
            //isGrounded = true;
            isWallCenter = true;
        }

        if (collision.gameObject.CompareTag("Monster") && GameManager.inputEnabled)
        {
            if (rb != null)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    // Player의 아랫면과 Monster의 윗면이 닿았는지 확인
                    if (contact.normal.y > 0.9f)
                    {
                        Debug.Log("몬스터컷");
                        rb.velocity = new Vector3(rb.velocity.x, jumpForce * 0.012f, rb.velocity.z);
                        Destroy(collision.gameObject);
                        AddScore(collision.gameObject.GetComponent<Monster>().score);
                        return;
                    }
                }

                Debug.Log("몬스터충돌");
                StartCoroutine(InputDelay(0.5f));
                AddCurHp(-collision.gameObject.GetComponent<Monster>().atk);

                Vector3 direction = transform.position - collision.transform.position;
                direction.y = 0;
                direction.Normalize();
                //rb.velocity = direction * 5.0f;
                rb.AddForce(direction * 5.0f, ForceMode.VelocityChange);
            }
        }

    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Wall"))
    //    {
    //        isGrounded = true;

    //        if (collision.gameObject.CompareTag("Wall"))
    //        {
    //            // 2개 이상의 Wall에 닿을때 문제가 있긴함
    //            wallPos = collision.gameObject.GetComponent<Wall>().GetWallOriPos();
    //        }
    //    }

    //}

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Wall"))
        {
            //isGrounded = false;
            wallPos.x = 0;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coin"))
        {
            AddScore(100);
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("Treasure"))
        {
            AddScore(1000);
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("SmallHealKit"))
        {
            AddCurHp(1);
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("BigHealKit"))
        {
            AddCurHp(5);
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("Potion"))
        {
            AddMaxHp(1);
            AddCurHp(1);
            Destroy(other.transform.gameObject);
        }

        //if (other.gameObject.CompareTag("MonsterHead"))
        //{
        //    //rb.AddForce(new Vector3(0, jumpForce * 0.7f, 0f));
        //    rb.velocity = new Vector3(rb.velocity.x, jumpForce * 0.012f, rb.velocity.z);
        //    Destroy(other.transform.parent.gameObject);
        //    AddScore(100);
        //}

        if (other.gameObject.CompareTag("Orb"))
        {
            isDimension = true;
            Destroy(other.transform.gameObject);
        }

        if (other.gameObject.CompareTag("Portal"))
        {
            GameManager.level++;
            GameManager.LoadNextMap();
        }
    }


}
