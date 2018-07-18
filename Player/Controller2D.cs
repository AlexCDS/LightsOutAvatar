using UnityEngine;

// *********************************************************
// * By AlexC
// * Manages player velocity and collisions detection. 
// *********************************************************

public class Controller2D : RaycastController {

    public LayerDepth CurrentLayerDepth { get; private set; }
    public CollisionInfo collisions;

    public LayerMask currentCollisionMask;
    [Header("Don't touch these")]
    [SerializeField] LayerMask frontCollisionMask;
    [SerializeField] LayerMask backCollisionMask;

    public override void Start()
    {
        base.Start();
    }

    public void Move(Vector3 velocity)
    {
        if (velocity.x != 0)
            HorizontalCollisions(ref velocity);

        if(velocity.y != 0)
            VerticalCollisions(ref velocity);
    }

    public void ResetController(ref Vector3 velocity)
    {
        collisions.Reset();
        UpdateRaycastOrigins();
        collisions.velocityOld = velocity;
    }

    public void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLenght = Mathf.Abs(velocity.y) + SKINWIDTH;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLenght, currentCollisionMask);

            if (hit)
            {
                collisions.numberOfHitsY++;

                if (hit.distance == 0 && collisions.numberOfHitsY == 1)
                {
                    Vector3 direction = (directionY == 1) ? Vector3.down : Vector3.up;
                    transform.position += direction * SKINWIDTH;
                    Debug.Log("Adjusted position Y");
                }


                if (hit.collider.GetComponent<Ing_MonkeyBar>() != null)
                {
                    collisions.onMonkeyBar = true;
                }

                velocity.y = (hit.distance - SKINWIDTH) * directionY;
                rayLenght = hit.distance;

                if (collisions.climbingSlope){
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;

                velocity.y = (hit.distance - SKINWIDTH) * directionY;

            }
        }
    }

    public void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLenght = Mathf.Abs(velocity.x) + SKINWIDTH;

        if (Mathf.Abs(velocity.x) < SKINWIDTH)
            rayLenght = 2 * SKINWIDTH;

        RaycastHit2D previousHit = new RaycastHit2D();
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLenght, currentCollisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLenght, Color.red);

            if (hit)
            {
                collisions.numberOfHitsX++;

                if (hit.distance == 0 && collisions.numberOfHitsX == 1)
                {
                    Vector3 direction = (directionX == 1) ? Vector3.left : Vector3.right;
                    transform.position += direction * SKINWIDTH;
                    Debug.Log("Adjusted position X");
                }

                velocity.x = (hit.distance - SKINWIDTH) * directionX;

                collisions.left = directionX == -1;
                collisions.right = directionX == 1;

                previousHit = hit;
            }

            if (!hit && collisions.numberOfHitsX >= horizontalRayCount - 1)
            {

                float distance = Vector3.Distance(rayOrigin, previousHit.point);

                for (int j = 1; j < distance * 10; j++)
                {
                    rayOrigin.y -= 0.05f * j;
                    hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLenght, currentCollisionMask);

                    if (hit)
                        break;
                }
                
                if (hit)
                {
                    BoxCollider2D target = (BoxCollider2D)hit.collider;
                    if (target != null)
                    {
                        float collisionPositionY = target.size.y + target.transform.position.y;

                        float collisionPositionX = ((directionX == 1) ? 0 : target.size.x) + target.transform.position.x;
                        //float collisionPositionX = (directionX == 1)? 0 : (target.size.x) + target.transform.position.x;

                        collisions.onCorner = true;
                        collisions.cornerLocation = new Vector2(collisionPositionX, collisionPositionY);
                    }
                }

            }
        }
    }

    public void ChangeLayerDepth(LayerDepth depth, Vector3 newPos)
    {
        CurrentLayerDepth = depth;

        if (Man_LevelManager.currentLevel == null)
        {
            Debug.LogError("Tu devrais avoir un Man_LevelManager dans ta scene, aucune profondeur de level n'a été trouvé");
            transform.position = newPos;
        }
        else
        {
            transform.position = new Vector3(newPos.x, newPos.y, Man_LevelManager.GetDepthFromLayerDepth(CurrentLayerDepth));
        }

        switch (depth)
        {
            case LayerDepth.front:
                currentCollisionMask = frontCollisionMask;
                break;
            case LayerDepth.back:
                currentCollisionMask = backCollisionMask;
                break;
        }
    }

    public void SnapToCurrentLayerDepth()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, Man_LevelManager.GetDepthFromLayerDepth(CurrentLayerDepth));
    }
}
