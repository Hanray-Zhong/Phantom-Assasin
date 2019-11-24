using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    [Header("Game Input")]
    public GameInput PlayerGameInput;
    public Dropdown dropdown;
    [Header("Move")]
    public float MoveSpeed;
    private float normalSpeed;
    public float NormalSpeed {
        get {return normalSpeed;}
    }
    public float JumpForce;
    public float distance_toGround;
    public Vector2 x_offset;
    [Header("Throw Cube")]
    public GameObject CubePrefab;
    public Transform ThrowPos;
    public float ThrowForce;
    public GameObject Arrow;
    [Range(0, 150)]
    public float CubeCDTimer = 0;
    [Range(0, 150)]
    public float Bullet_Timer = 0;
    public bool onBulletTime = false;
    public GameObject FlashEffect;
    public TrailRenderer trailRenderer;
    [Header("Hook")]
    public GameObject hookPrefab;
    public bool onHook;
    public float HookCircleRadius;
    public float SwingForce;
    [Range(0, 250)]
    public float Hang_Time = 0;
    [Range(0, 150)]
    public float Hook_CD_Time = 150;
    public float ClimbSpeed;
    public float Max_RopeLength;
    public float Min_RopeLength;
    

    [Header("move")]
    private Vector2 moveDir;
    public Vector2 MoveDir {
        get {return moveDir;}
    }
    private bool faceRight = true;
    public bool FaceRight {
        get {return this.faceRight;}
    }
    [Header("jump")]
    private bool onGround;
    private float delta_JumpInteraction;
    private float lastJumpInteraction = 0;
    public bool OnGround {
        get {return onGround;}
    }
    private bool allowJump = false;
    public bool AllowJump {
        get {return this.allowJump;}
        set {allowJump = value;}
    }
    private bool secJump = false;
    public bool SecJump {
        get {return this.secJump;}
        set {secJump = value;}
    }
    // private float jumpInteract;
    
    [Header("throw cube")]
    private bool allowThrow = true;
    private Cube currentCube;
    private float lastThrowInteraction = 0;
    private float delta_ThrowInteraction;
    private SpriteRenderer sprite;
    [Header("flash")]
    private bool FlashOver = true;
    [Header("hook")]
    private float nearestDistance;
    private GameObject nearestHook = null;
    private GameObject currentHook;
    private Vector2 velocity_beforeHook;
    private bool justHook = false;
    private float lastHookInteraction = 0;
    private float delta_HookInteraction; 
    // Component
    private Rigidbody2D _rigidbody;
    private Transform _transform;
    private PlayerUnit _unit;

    private void Awake() {
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _transform = gameObject.GetComponent<Transform>();
        _unit = gameObject.GetComponent<PlayerUnit>();
        sprite = gameObject.GetComponent<SpriteRenderer>();
    }
    private void Update() {
        // if (_unit.IsDead) return;
        this.GetMoveDir();
    }
    private void LateUpdate() {
        this.Move();
        this.Jump();
        this.CheckCubeCD();
        this.ThrowCube();
        this.Flash();
        this.GetHook();
    }
    private void FixedUpdate() {
        this.CubeTimer();
    }

    private void GetMoveDir() {
        moveDir = PlayerGameInput.GetMoveDir();
        if (moveDir.x > 0)
            faceRight = true;
        if (moveDir.x < 0)
            faceRight = false;
        // Debug.Log(moveDir);
    }
    private void Move() {
        // _rigidbody.AddForce(moveDir * MoveSpeed * Time.deltaTime, ForceMode2D.Force);
        // _transform.Translate(moveDir * MoveSpeed * Time.deltaTime, Space.World);
        if (!onHook && Bullet_Timer == 0) {
            _rigidbody.freezeRotation = true;
            _rigidbody.velocity = new Vector2(moveDir.x * MoveSpeed, _rigidbody.velocity.y);
            transform.rotation = Quaternion.identity;
        }

        if (onHook) {
            _rigidbody.freezeRotation = false;
            if (justHook) {
                _rigidbody.velocity = velocity_beforeHook;
                justHook = false;
            }
            transform.up = nearestHook.transform.position - transform.position;
        }
    }
    public bool Jump() {
        // jumpInteract = PlayerGameInput.GetJumpInteraction();
        // if (jumpInteract > 0) {
        //     _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
        // }
        delta_JumpInteraction = PlayerGameInput.GetJumpInteraction() - lastJumpInteraction;
        IsOnGround();
        if (onGround) 
            allowJump = true;
        else
            allowJump = false;
        if (delta_JumpInteraction > 0 && (allowJump || secJump)) {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0);
            _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);

            Debug.Log("Jump!");

            allowJump = false;
            secJump = false;
            lastJumpInteraction = PlayerGameInput.GetJumpInteraction();
            return true;
        }
        lastJumpInteraction = PlayerGameInput.GetJumpInteraction();
        return false;
    }
    private void IsOnGround() {
        onGround = Physics2D.Raycast(transform.position, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground")) || 
                    Physics2D.Raycast(transform.position + (Vector3)x_offset, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground")) || 
                    Physics2D.Raycast(transform.position - (Vector3)x_offset, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground"));
    }
    private void ThrowCube() {
        delta_ThrowInteraction = PlayerGameInput.GetThrowInteraction() - lastThrowInteraction;
        if (PlayerGameInput.GetThrowInteraction() == 1 && allowThrow) {
            Time.timeScale = 0.1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            onBulletTime = true;
            Bullet_Timer++;
            Arrow.SetActive(true);
            if (moveDir != Vector2.zero)
                Arrow.transform.up = moveDir;
            else if (faceRight)
                Arrow.transform.up = Vector2.right;
            else
                Arrow.transform.up = Vector2.left;
        }
        if ((delta_ThrowInteraction < 0 || Bullet_Timer >= 150) && allowThrow) {
            currentCube = Instantiate(CubePrefab, ThrowPos.position, Quaternion.identity).GetComponent<Cube>();
            currentCube.Player = gameObject;
            Rigidbody2D cube_rigidbody = currentCube.gameObject.GetComponent<Rigidbody2D>();
            if (cube_rigidbody != null) {
                if (moveDir != Vector2.zero)
                    cube_rigidbody.AddForce(moveDir * ThrowForce, ForceMode2D.Impulse);
                else if (faceRight)
                    cube_rigidbody.AddForce(Vector2.right * ThrowForce, ForceMode2D.Impulse);
                else
                    cube_rigidbody.AddForce(Vector2.left * ThrowForce, ForceMode2D.Impulse);
            }
            CubeCDTimer = 0;
            allowThrow = false;
            Bullet_Timer = 0;
            Arrow.SetActive(false);
            Time.timeScale = 1;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            onBulletTime = false;
        }
        lastThrowInteraction = PlayerGameInput.GetThrowInteraction();
    }
    private void CubeTimer() {
        if (CubeCDTimer < 150)
            CubeCDTimer++;
        if (CubeCDTimer == 150 && FlashOver)
            allowThrow = true;
        else
            allowThrow = false;
    }
    private void CheckCubeCD() {
        Color Transparent = new Color(1, 1, 1, 0.5f);
        Color white = new Color(1, 1, 1, 1f);
        if (CubeCDTimer < 150) {
            sprite.color = Transparent;
        }
        else {
            sprite.color = white;
        }
    }
    private bool Flash() {
        if (!allowThrow && currentCube != null && CubeCDTimer > 10) {
            if (delta_ThrowInteraction > 0) {
                FlashOver = false;
                if (FlashEffect != null) {
                    trailRenderer.emitting = true;
                    Instantiate(FlashEffect, transform.position, Quaternion.identity);
                }
                if (currentCube.HitGround) {
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    CubeCDTimer = 140;
                    Destroy(currentCube.gameObject);
                }
                if (!currentCube.HitEnemy && !currentCube.HitInteractiveItem) {
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    Destroy(currentCube.gameObject);
                    _rigidbody.velocity = Vector2.zero;
                }
                if (currentCube.HitEnemy) {
                    currentCube.Enemy.SetActive(false);
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    CubeCDTimer = 140;
                    Destroy(currentCube.gameObject);
                    _rigidbody.velocity = Vector2.zero;
                }
                if (currentCube.HitInteractiveItem) {
                    currentCube.InteractiveItem.GetComponent<InterActiveItem>().InterAction();
                    Destroy(currentCube.gameObject);
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    CubeCDTimer = 140;
                }
                if (FlashEffect != null) {
                    Invoke("SetTrailRendererFalse", 0.1f);
                    
                    Instantiate(FlashEffect, transform.position, Quaternion.identity);
                }
                secJump = true;
                return true;
            }
        }
        if (delta_ThrowInteraction < 0)
            FlashOver = true;
        return false;
    }
    private void SetTrailRendererFalse() {
        trailRenderer.emitting = false;
    }
    private void GetHook() {
        delta_HookInteraction = PlayerGameInput.GetHookInteraction() - lastHookInteraction;
        nearestDistance = HookCircleRadius;
        if (Hook_CD_Time < 150)
            Hook_CD_Time++;
        if (!onGround && !onHook && Hook_CD_Time >= 150) {
            if (nearestHook != null) {
                float Distance_nearestHook_Player = Vector2.Distance(nearestHook.transform.position, transform.position);
                if (Distance_nearestHook_Player > HookCircleRadius)
                    nearestHook = null;
            }
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, HookCircleRadius, 1 << LayerMask.NameToLayer("Hook"));
            if (cols.Length != 0) {
                foreach (var col in cols) {
                    if ((faceRight && col.gameObject.transform.position.x < _transform.position.x) || (!faceRight && col.gameObject.transform.position.x > _transform.position.x))
                        continue;
                    float currentDistance = Vector2.Distance(col.gameObject.transform.position, transform.position);
                    if (nearestDistance > currentDistance) {
                        nearestHook = col.gameObject;
                        nearestDistance = currentDistance;
                    }
                }
                if (nearestHook == null) {
                    return;
                }
                if (delta_HookInteraction > 0) {
                    onHook = true;
                    secJump = true;
                    velocity_beforeHook = _rigidbody.velocity;
                    justHook = true;
                    // nearestHook.GetComponent<HingeJoint2D>().connectedBody = _rigidbody;
                    Vector2 destination = nearestHook.transform.position;
				    currentHook = (GameObject)Instantiate (hookPrefab, transform.position, Quaternion.identity);
				    currentHook.GetComponent<Rope>().player = gameObject;
				    currentHook.GetComponent<Rope>().destination = destination;
                }
            }
        }
        if (onHook && nearestHook != null) {
            Hang_Time++;
            // // 上下攀爬
            // Vector2 player_hook_dir = nearestHook.transform.position - transform.position;
            // if (moveDir.y > 0 && transform.position.y < nearestHook.transform.position.y && player_hook_dir.magnitude > Min_RopeLength) 
            //     transform.Translate(player_hook_dir * ClimbSpeed * Time.deltaTime);
            // if (moveDir.y < 0 && transform.position.y < nearestHook.transform.position.y && player_hook_dir.magnitude < Max_RopeLength) 
            //     transform.Translate(-player_hook_dir * ClimbSpeed * Time.deltaTime);
            // // 左右晃动
            if (transform.position.y < nearestHook.transform.position.y) {
                Vector2 swingDir = new Vector2(moveDir.x, 0);
                _rigidbody.AddForce(swingDir * SwingForce * Time.deltaTime, ForceMode2D.Force);
            }
            // 断开连接 (跳跃断开判定有点奇怪)
            if (delta_HookInteraction < 0 || onGround || !secJump || this.Flash()) {
                onHook = false;
                Destroy (currentHook);
                Hang_Time = 0;
            }
            if (Hang_Time >= 250) {
                onHook = false;
                Destroy (currentHook);
                Hang_Time = 0;
                Hook_CD_Time = 0;
            }
        }
        lastHookInteraction = PlayerGameInput.GetHookInteraction();
    }
    
    public void ResetStatus() {
        CubeCDTimer = 150;
        Bullet_Timer = 0;
        Hang_Time = 0;
        Hook_CD_Time = 150;
        onHook = false;
        // lastThrowInteraction = 0;
        // lastThrowInteraction = 0;
    }
    public void SwitchGameInput() {
        switch (dropdown.value) {
            case 0 : PlayerGameInput = gameObject.GetComponent<PC_Input>(); break;
            case 1 : PlayerGameInput = gameObject.GetComponent<XBox_Input>(); break;
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -distance_toGround, 0));
        Gizmos.DrawLine(transform.position + (Vector3)x_offset, transform.position + new Vector3(0, -distance_toGround, 0));
        Gizmos.DrawWireSphere(transform.position, HookCircleRadius);
    }
}