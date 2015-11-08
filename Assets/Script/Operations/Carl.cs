using UnityEngine;
using System.Collections;

public class Carl : MonoBehaviour
{
    const float BLOCK_WID = 0.4f, HALF_BLOCK = 0.2f;
    TweenPosition[] jumps;
    bool isJumping, onLadder, isClimbing;
    Vector3 jumpHigh = new Vector3(0, 0.5f, 0);
    Animator animator;
    Rigidbody rigid;
    public Transform camera;

    void Start()
    {
        jumps = GetComponents<TweenPosition>();
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody>();
        jumps[0].SetOnFinished(delegate()
        {
            jumps[1].from = transform.position;
            jumps[1].to = transform.position - jumpHigh;
            jumps[1].ResetToBeginning();
            jumps[1].PlayForward();
        });
        jumps[1].SetOnFinished(delegate()
        {
            isJumping = false;
            idle();
        });
        onLadder = isClimbing = false;
    }
    public void move(bool right)
    {
        if (!isClimbing)
        {
           // Vector3 scoutRight = transform.position + (right ? transform.right * BLOCK_WID : -transform.right * BLOCK_WID);
            if (Physics.Raycast(transform.position, right ? transform.right : -transform.right,
               BLOCK_WID, 1 << LayerMask.NameToLayer("Obstacle")))
                //以主角的位置向将要移动的方向发射线，看将要移动的方向是否被东西挡住了，如果被挡住了则要把角色往前移
                moveIfForward(right);

            if (!Physics.Raycast(transform.position, Vector3.down, 0.6f, 1 << LayerMask.NameToLayer("Obstacle")))
           //     if (!Physics.Raycast(scoutRight, Vector3.down, 0.6f, 1 << LayerMask.NameToLayer("Obstacle")))
            //以右向探子向下发射线，看将要移动的方向是否有路，如果没有路则要从前往后看有没有路
                if (!moveIfForward(right))
                    moveIfBack(right);

            //然后使主角移动
            transform.Translate(Time.deltaTime * (right ? Vector3.right : Vector3.left));

            animator.Play("walk");
        }
    }
    bool moveIfForward(bool right)//如果前方右路则把角色往前移，返回值是是否移动了
    {
        //以脚下探子向前后发射线，并从前往后按每个砖向要走的方向发射线，碰到路后再以路向上发射线，如果是空的则将主角移动到这一点
        Vector3 scoutDown = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);
        RaycastHit[] hits = Physics.RaycastAll(scoutDown, -transform.forward, 50, 1 << LayerMask.NameToLayer("Obstacle"));

        //先按距离远近排序，越远越拍前（即距离屏幕越近，数量不会很多，所以用冒泡即可）
        for (int x = 0; x < hits.Length; ++x)
            for (int y = x + 1; y < hits.Length; ++y)
                if (Vector3.Distance(hits[y].point, scoutDown) > Vector3.Distance(hits[x].point, scoutDown))
                {
                    RaycastHit hitTemp = hits[x];
                    hits[x] = hits[y];
                    hits[y] = hitTemp;
                }

        Vector3 rayDir = right ? transform.right : -transform.right;
        float euler = Mathf.Abs(transform.eulerAngles.y) % 180;
        foreach (RaycastHit hit in hits)
        {
            Vector3 from = hit.point - transform.forward * HALF_BLOCK;
            RaycastHit newHit;
            if (Physics.Raycast(from, rayDir, out newHit, BLOCK_WID, 1 << LayerMask.NameToLayer("Obstacle"))
                && newHit.transform.tag == "ground")//如果要走的方向有路
            {
                Vector3 to = from;// +rayDir * HALF_BLOCK;
                transform.position = new Vector3(to.x, transform.position.y, to.z);
                if (euler > 45)
                    camera.position = new Vector3(to.x, camera.position.y, camera.position.z);
                else
                    camera.position = new Vector3(camera.position.x, camera.position.y, to.z);
                return true;
            }
        }
        return false;
    }
    void moveIfBack(bool right)//如果后方右路则把角色往后移，无需返回
    {
        Vector3 scoutDown = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);
        RaycastHit[] hits = Physics.RaycastAll(scoutDown, transform.forward, 50, 1 << LayerMask.NameToLayer("Obstacle"));

        //先按距离远近排序，越近越拍前（即距离屏幕越近，数量不会很多，所以用冒泡即可）
        for (int x = 0; x < hits.Length; ++x)
            for (int y = x + 1; y < hits.Length; ++y)
                if (Vector3.Distance(hits[y].point, scoutDown) < Vector3.Distance(hits[x].point, scoutDown))
                {
                    RaycastHit hitTemp = hits[x];
                    hits[x] = hits[y];
                    hits[y] = hitTemp;
                }

        Vector3 rayDir = right ? transform.right : -transform.right;
        float euler = Mathf.Abs(transform.eulerAngles.y) % 180;
        foreach (RaycastHit hit in hits)
        {
            Vector3 from = hit.point + transform.forward * HALF_BLOCK;
            RaycastHit newHit;
            if (Physics.Raycast(from, rayDir, out newHit, BLOCK_WID, 1 << LayerMask.NameToLayer("Obstacle"))
                && newHit.transform.tag == "ground")//如果要走的方向有路
            {
                Vector3 to = from;// +rayDir * HALF_BLOCK;
                transform.position = new Vector3(to.x, transform.position.y, to.z);
                if (euler > 45)
                    camera.position = new Vector3(to.x, camera.position.y, camera.position.z);
                else
                    camera.position = new Vector3(camera.position.x, camera.position.y, to.z);
            }
        }
    }
    public void jump(bool up)
    {
        if (onLadder)//如果在楼梯上就上楼梯
        {
            if (!isClimbing)
                StartCoroutine(climbLadder(up));
        }
        else//如果不在楼梯上就跳
        {
            if (!isJumping)
            {
                rigid.useGravity = false;
                isJumping = true;
                jumps[0].from = transform.position;
                jumps[0].to = transform.position + jumpHigh;
                jumps[0].ResetToBeginning();
                jumps[0].PlayForward();
                animator.Play("jump");
            }
        }

    }
    IEnumerator climbLadder(bool up)
    {
        isClimbing = true;
        while (onLadder)
        {
            transform.Translate((up ? Vector3.up : Vector3.down) * Time.deltaTime);
            yield return null;
        }
        isClimbing = false;
    }
    Transform ladder;
    IEnumerator jumpOffLadder()
    {
        rigid.useGravity = true;
        yield return 1;

        while (rigid.velocity.magnitude != 0)
        {//说明还在动
            yield return null;
        }
        rigid.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "ladder")
        {
            print("onLadder");
            onLadder = true;
            rigid.velocity = Vector3.zero;
            rigid.useGravity = false;

        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "ladder")
        {
            onLadder = false;
            rigid.useGravity = true;
            if (isClimbing)
            {
                Transform top = other.transform.FindChild("top"),
                    bottom = other.transform.FindChild("bottom");
                Vector3 endPos = (top.position.y + bottom.position.y) / 2 > transform.position.y ?
                    bottom.position : top.position;
                transform.position = new Vector3(transform.position.x, endPos.y, endPos.z);
                print("success climb");
            }
            print("exit Ladder");
        }
    }
    public void idle()
    {
        //transform
        if (!isJumping)
            animator.Play("idle");
    }
    public LineRenderer line;
    void Update()
    {
        if (rigid.velocity.magnitude == 0 && !isClimbing)
            idle();
    }
}
