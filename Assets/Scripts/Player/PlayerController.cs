using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("事件监听")]
    public SceneLoadEventSO sceneLoadEvent;
    public VoidEventSO afterSceneLoadedEvent;
    public VoidEventSO loadDataEvent;
    public VoidEventSO backToMenuEvent;

    public PlayerInputControl inputControl;
    private Rigidbody2D rb;
    private CapsuleCollider2D coll;
    private SpriteRenderer spr;
    private float faceDir = 1;    //滑铲判断朝向用
    [SerializeField]
    public Vector2 inputDirection;
    public PhysicsCheck physicsCheck;
    public PlayerAnimation playerAnimation;
    private Character character;

    [Header("基本参数")]
    public float speed;
    private float runSpeed;
    private float walkSpeed => speed / 2.5f;

    public float jumpForce;
    public float wallJumpForce;
    //受伤反弹
    public float hurtForce;
    public float slideDistance;
    public float slideSpeed;
    public int slidePowerCost;


    // 记录最后一次有效的移动方向，用于翻转
    private float lastMovementDirectionX = 1f;  //默认向右
    
    //原始碰撞体参数
    private Vector2 originalOffset;
    private Vector2 originalSize;

    [Header("材质")]
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D wall;

    [Header("状态")]
    //下蹲状态标记
    public bool isCrouch;
    public bool isHurt;
    public bool isDead;
    public bool isAttack;
    public bool wallJump;
    public bool isSlide;

    // --- 新增: 用于引用 Attack Area ---
    [Header("Attack Area")]
    public Transform attackAreaParent; // 拖拽 Attack Area GameObject 到这里
    // --- end of 新增 ---

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        coll = GetComponent<CapsuleCollider2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        character = GetComponent<Character>();
        originalOffset = coll.offset;
        originalSize = coll.size;

        inputControl = new PlayerInputControl();
        //跳跃
        inputControl.Gameplay.Jump.started += Jump;

        #region 强制走路
        runSpeed = speed;
        inputControl.Gameplay.WalkButton.performed += ctx =>
        {
            if (physicsCheck.isGround)
                speed = walkSpeed;
        };

        inputControl.Gameplay.WalkButton.canceled += ctx =>
        {
            if (physicsCheck.isGround)
                speed = runSpeed;
        };
        #endregion
        spr = GetComponent<SpriteRenderer>();

        //攻击
        inputControl.Gameplay.Attack.started += PlayerAttack;

        //滑铲
        inputControl.Gameplay.Slide.started += Slide;
        inputControl.Enable();

    }


    private void OnEnable()
    {
        sceneLoadEvent.LoadRequestEvent += OnLoadEvent;
        afterSceneLoadedEvent.OnEventRaised += OnAfterSceneLoadedEvent;
        loadDataEvent.OnEventRaised += OnLoadDataEvent;
        backToMenuEvent.OnEventRaised += OnLoadDataEvent;
    }


    private void OnDisable()
    {
        inputControl.Disable();
        sceneLoadEvent.LoadRequestEvent -= OnLoadEvent;
        afterSceneLoadedEvent.OnEventRaised -= OnAfterSceneLoadedEvent;
        loadDataEvent.OnEventRaised -= OnLoadDataEvent;
        backToMenuEvent.OnEventRaised -= OnLoadDataEvent;
    }
    private void OnLoadDataEvent()
    {
        isDead = false;
    }

    //场景加载过程停止控制
    private void OnLoadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        inputControl.Gameplay.Disable();
    }

    //加载结束后启动控制
    private void OnAfterSceneLoadedEvent()
    {
        inputControl.Gameplay.Enable();
    }

    private void Update()
    {
        inputDirection = inputControl.Gameplay.Move.ReadValue<Vector2>();
        CheckState();
    }

    private void FixedUpdate()
    {
        if(!isHurt && !isAttack)
            Move();
    }

    public void Move()
    {
        // --- 添加死区检查 ---
        float deadzoneThreshold = 0.8f; // 可以根据需要调整这个阈值
        if (inputDirection.magnitude < deadzoneThreshold)
        {
            inputDirection = Vector2.zero; // 如果输入小于阈值，强制设为零
        }
        // 只有当输入方向不为零时，才更新最后的移动方向
        if(inputDirection.x != 0)
        {
            lastMovementDirectionX = inputDirection.x;  // 记录当前输入的 X 方向
        }
        //人物移动
        if(!isCrouch && !wallJump)
            rb.velocity = new Vector2(inputDirection.x * speed * Time.deltaTime, rb.velocity.y);

        // 使用记录的最后移动方向进行翻转
        if(lastMovementDirectionX < 0)
        {
            spr.flipX = true;
            faceDir = -1f;
            attackAreaParent.transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            spr.flipX= false;
            faceDir = 1f;
            attackAreaParent.transform.localScale = new Vector3(1, 1, 1);
        }
        //spr.flipX = lastMovementDirectionX < 0; // 如果最后移动方向是负的 (左)，则翻转 X 轴

        //下蹲
        isCrouch = inputDirection.y < -0.5f && physicsCheck.isGround;
        if (isCrouch)
        {
            //修改碰撞体大小和位移
            coll.offset = new Vector2(-0.05f, 0.85f);
            coll.size = new Vector2(0.7f, 1.7f);
        }
        else
        {
            //还原之前碰撞体的参数
            coll.size = originalSize;
            coll.offset = originalOffset;
        }
    }

    public void Jump(InputAction.CallbackContext obj)
    {
        if (physicsCheck.isGround)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            GetComponent<AudioDefination>()?.PlayAudioClip();

            //打断滑铲协程
            isSlide = false;
            StopAllCoroutines();
        }
        else if(physicsCheck.onWall)
        {
            rb.AddForce(new Vector2(-inputDirection.x, 2.5f) * wallJumpForce, ForceMode2D.Impulse);
            wallJump = true;
        }
    }

    private void PlayerAttack(InputAction.CallbackContext obj)
    {
        if (!physicsCheck.isGround)
            return;
        playerAnimation.PlayAttack();
        isAttack = true;
    }

    private void Slide(InputAction.CallbackContext obj)
    {
        if(!isSlide && physicsCheck.isGround && character.currentPower >= slidePowerCost)
        {
            isSlide = true;

            var targetPos = new Vector3(transform.position.x + slideDistance * faceDir, transform.position.y);

            StartCoroutine(TriggerSlide(targetPos));

            character.OnSlide(slidePowerCost);
        }
    }

    private IEnumerator TriggerSlide(Vector3 target)
    {
        float maxSlideDuration = 0.4f; // 最多滑动0.4秒
        float startTime = Time.time;
        do
        {
            yield return null;
            if (!physicsCheck.isGround)
                break;

            //滑动过程当中撞墙
            if(physicsCheck.touchLeftWall && faceDir < 0f || physicsCheck.touchRightWall && faceDir > 0f)
            {
                isSlide = false;
                break;
            }
            // 检查是否超时
            if (Time.time - startTime > maxSlideDuration)
            {
                Debug.Log("Slide interrupted: Timed out.");
                break;
            }
            rb.MovePosition(new Vector2(transform.position.x + faceDir * slideSpeed, transform.position.y));

        } while (MathF.Abs(target.x - transform.position.x) > 0.1f);
        isSlide = false;
    }

    #region UnityEvent
    /// <summary>
    /// 受伤反弹
    /// </summary>
    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;

        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
    }

    public void PlayerDie()
    {
        isDead = true;
        inputControl.Gameplay.Disable();

    }

    /// <summary>
    /// 死亡后避免敌人再次攻击死亡后避免敌人再次攻击
    /// </summary>
    private void CheckState()
    {
        coll.sharedMaterial = physicsCheck.isGround ? normal : wall;
        if (physicsCheck.onWall)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y / 2);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
        }

        if(wallJump && rb.velocity.y < 0f)
        {
            wallJump = false;
        }

        if (isDead || isSlide)
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        else
            gameObject.layer = LayerMask.NameToLayer("Player");
    }
    #endregion

}
