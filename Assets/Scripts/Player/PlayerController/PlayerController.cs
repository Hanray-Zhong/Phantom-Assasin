using UnityEngine;

public class PlayerController : MonoBehaviour {
    [Header("Game Input")]
    public GameInput Game_Input;
    [Header("Move")]
    public float MoveSpeed;
    public float JumpForce;
    public float distance_toGround;
    public Vector2 x_offset;
    [Header("Throw Cube")]
    public GameObject CubePrefab;
    public Transform ThrowPos;
    public float ThrowForce;
    public GameObject Arrow;
    [Range(0, 150)]
    public float Flash_CD_Time = 0;
    [Range(0, 150)]
    public float Bullet_Time = 0;
    [Header("Hook")]
    public bool onHook;
    public float HookCircleRadius;
    public float SwingForce;
    [Range(0, 250)]
    public float Hang_Time = 0;
    [Range(0, 150)]
    public float Hook_CD_Time = 150;
    public float ClimbSpeed;
    public LineRenderer Rope;
    public float Max_RopeLength;
    public float Min_RopeLength;
    

    // move
    private Vector2 moveDir;
    private bool faceRight;
    // jump
    private bool onGround;
    private bool allowJump = false;
    private bool secJump = false;
    // private float jumpInteract;
    
    // Throw Cube
    private bool allowThrow = true;
    private Cube currentCube;
    private float lastThrowInteraction = 0;
    private float delta_ThrowInteraction;
    // Flash
    private bool FlashOver = true;
    // Hook
    private float nearestDistance;
    private GameObject nearestHook = null;
    private Vector2 velocity_beforeHook;
    private bool justHook = false;
    private float lastHookInteraction = 0;
    private float delta_HookInteraction; 
    // Component
    private Rigidbody2D _rigidbody;
    private Transform _transform;

    private void Awake() {
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _transform = gameObject.GetComponent<Transform>();
        Rope.positionCount = 2;
    }
    private void Update() {
        this.GetMoveDir();
        this.Move();
        this.Jump();
        this.ThrowCube();
        this.Flash();
        this.GetHook();
    }
    private void FixedUpdate() {
        this.CubeTimer();
    }

    private void GetMoveDir() {
        moveDir = Game_Input.GetMoveDir();
        if (moveDir.x > 0)
            faceRight = true;
        if (moveDir.x < 0)
            faceRight = false;
        // Debug.Log(moveDir);
    }
    private void Move() {
        // _rigidbody.AddForce(moveDir * MoveSpeed * Time.deltaTime, ForceMode2D.Force);
        // _transform.Translate(moveDir * MoveSpeed * Time.deltaTime, Space.World);
        if (!onHook && Bullet_Time == 0) {
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
    private bool Jump() {
        // jumpInteract = Game_Input.GetJumpInteraction();
        // if (jumpInteract > 0) {
        //     _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
        // }
        OnGround();
        if (onGround) 
            allowJump = true;
        else
            allowJump = false;
        if (Game_Input.GetJumpInteraction() == 1 && (allowJump || secJump)) {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0);
            _rigidbody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            allowJump = false;
            secJump = false;
            return true;
        }
        return false;
    }
    private void OnGround() {
        onGround = Physics2D.Raycast(transform.position, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground")) || 
                    Physics2D.Raycast(transform.position + (Vector3)x_offset, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground")) || 
                    Physics2D.Raycast(transform.position - (Vector3)x_offset, Vector2.down, distance_toGround, 1 << LayerMask.NameToLayer("Ground"));
    }
    private void ThrowCube() {
        delta_ThrowInteraction = Game_Input.GetThrowInteraction() - lastThrowInteraction;
        if (Game_Input.GetThrowInteraction() == 1 && allowThrow) {
            Time.timeScale = 0.1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            Bullet_Time++;
            Arrow.SetActive(true);
            if (moveDir != Vector2.zero)
                Arrow.transform.up = moveDir;
            else if (faceRight)
                Arrow.transform.up = Vector2.right;
            else
                Arrow.transform.up = Vector2.left;
        }
        if ((delta_ThrowInteraction < 0 || Bullet_Time >= 150) && allowThrow) {
            currentCube = Instantiate(CubePrefab, ThrowPos.position, Quaternion.identity).GetComponent<Cube>();
            currentCube.Player = gameObject;
            Rigidbody2D cube_rigidbody = currentCube.gameObject.GetComponent<Rigidbody2D>();
            if (cube_rigidbody != null) {
                Debug.Log(moveDir);
                if (moveDir != Vector2.zero)
                    cube_rigidbody.AddForce(moveDir * ThrowForce, ForceMode2D.Impulse);
                else if (faceRight)
                    cube_rigidbody.AddForce(Vector2.right * ThrowForce, ForceMode2D.Impulse);
                else
                    cube_rigidbody.AddForce(Vector2.left * ThrowForce, ForceMode2D.Impulse);
            }
            Flash_CD_Time = 0;
            allowThrow = false;
            Bullet_Time = 0;
            Arrow.SetActive(false);
            Time.timeScale = 1;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        lastThrowInteraction = Game_Input.GetThrowInteraction();
    }
    private void CubeTimer() {
        if (Flash_CD_Time < 150)
            Flash_CD_Time++;
        if (Flash_CD_Time == 150 && FlashOver)
            allowThrow = true;
        else
            allowThrow = false;
    }
    private bool Flash() {
        if (!allowThrow && currentCube != null && Flash_CD_Time > 10) {
            if (delta_ThrowInteraction > 0) {
                FlashOver = false;
                if (currentCube.HitGround) {
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    Flash_CD_Time = 140;
                    Destroy(currentCube.gameObject);
                }
                if (!currentCube.HitEnemy) {
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    Destroy(currentCube.gameObject);
                }
                if (currentCube.HitEnemy) {
                    Destroy(currentCube.Enemy);
                    gameObject.transform.position = currentCube.gameObject.transform.position;
                    Flash_CD_Time = 140;
                    Destroy(currentCube.gameObject);
                }
                _rigidbody.velocity = Vector2.zero;
                secJump = true;
                return true;
            }
        }
        if (delta_ThrowInteraction < 0)
            FlashOver = true;
        return false;
    }
    private void GetHook() {
        delta_HookInteraction = Game_Input.GetHookInteraction() - lastHookInteraction;
        nearestDistance = HookCircleRadius;
        if (Hook_CD_Time < 150)
            Hook_CD_Time++;
        if (!onHook)
            Rope.gameObject.SetActive(false);
        if (!onGround && !onHook && Hook_CD_Time >= 150) {
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
                    nearestHook.GetComponent<HingeJoint2D>().connectedBody = _rigidbody;
                }
            }
        }
        // 松开钩子情况 ：
        // 	松开”B”键
        // 	双脚落地
        // 	跳跃
        // 	瞬移
        // 	与另一个飞镖连接   ?????
        // 	连接时间到达5s（这种情况下断开后绳子会进入冷却，3s内不能再次使用）
        if (onHook && nearestHook != null) {
            Hang_Time++;
            // 上下攀爬
            Vector2 player_hook_dir = nearestHook.transform.position - transform.position;
            if (moveDir.y > 0 && transform.position.y < nearestHook.transform.position.y && player_hook_dir.magnitude > Min_RopeLength) 
                transform.Translate(player_hook_dir * ClimbSpeed * Time.deltaTime);
            if (moveDir.y < 0 && transform.position.y < nearestHook.transform.position.y && player_hook_dir.magnitude < Max_RopeLength) 
                transform.Translate(-player_hook_dir * ClimbSpeed * Time.deltaTime);
            // 左右晃动
            if (transform.position.y < nearestHook.transform.position.y) {
                Vector2 swingDir = new Vector2(moveDir.x, 0);
                _rigidbody.AddForce(swingDir * SwingForce * Time.deltaTime, ForceMode2D.Force);
            }
            // 断开连接 (跳跃断开判定有点奇怪)
            if (delta_HookInteraction < 0 || onGround || !secJump || this.Flash()) {
                onHook = false;
                nearestHook.GetComponent<HingeJoint2D>().connectedBody = null;
                Hang_Time = 0;
            }
            if (Hang_Time >= 250) {
                onHook = false;
                nearestHook.GetComponent<HingeJoint2D>().connectedBody = null;
                Hang_Time = 0;
                Hook_CD_Time = 0;
            }
        }
        if (onHook && nearestHook != null) {
            // 可视化Rope
            Rope.gameObject.SetActive(true);
            Rope.SetPosition(0, transform.position);
            Rope.SetPosition(1, nearestHook.transform.position);
        }
        lastHookInteraction = Game_Input.GetHookInteraction();
    }
    

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -distance_toGround, 0));
        Gizmos.DrawLine(transform.position + (Vector3)x_offset, transform.position + new Vector3(0, -distance_toGround, 0));
        Gizmos.DrawWireSphere(transform.position, HookCircleRadius);
    }
}