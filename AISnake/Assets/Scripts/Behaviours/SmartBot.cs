using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "SmartBot", menuName = "AIBehaviours/SmartBot")]
public class SmartBot : AIBehaviour {

    [SerializeField, Tooltip("Tags consideras como obstáculos que o bot vai fugir.")]
    private List<string> tagsToFlee = new List<string>();

    [SerializeField, Tooltip("Segundos que o bot foge do obstáculo antes de voltar a procurar por orbes.")]
    private float secondsToFlee = 1f;

    [SerializeField]
    private float orbCheckRadius = 10f;

    [SerializeField]
    private LayerMask orbsLayers;

    [SerializeField]
    private float obstacleCheckRadius = 1f;

    [SerializeField]
    private LayerMask obstacleLayers;

    List<Transform> orbsPositions = new List<Transform>();

    private enum SmartBotStates {
        Search,
        Flee
    }

    private SmartBotStates currentState;

    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        currentState = SmartBotStates.Search;

        // Randomize initial direction
        float angle = UnityEngine.Random.Range(0f, 2f);
        direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
    }

    private bool CheckSameSnake(GameObject hitObject)
    {
        SnakeMovement hitSnakeMovement = hitObject.GetComponent<SnakeMovement>();
        if (hitSnakeMovement == null)
        {
            var hitSnakeBody = hitObject.GetComponent<SnakeBody>();
            hitSnakeMovement = hitSnakeBody?.head.GetComponent<SnakeMovement>();
        }

        if (hitSnakeMovement == ownerMovement)
        {
            return true;
        }

        return false;
    }

    private bool CheckIsObstacle(GameObject hitObject)
    {
        if (tagsToFlee.Contains(hitObject.tag))
        {
            return true;
        }
        return false;
    }

    public override void Execute()
    {
        var orbsHits = Physics2D.CircleCastAll(owner.transform.position, orbCheckRadius, Vector2.zero, 0f, orbsLayers.value);
        var obstaclesHits = Physics2D.CircleCastAll(owner.transform.position, obstacleCheckRadius, Vector2.zero, 0f, obstacleLayers.value);

        orbsPositions.Clear();

        foreach (RaycastHit2D hit in obstaclesHits)
        {
            if (CheckSameSnake(hit.collider.gameObject)) { continue; }

            if (CheckIsObstacle(hit.collider.gameObject))
            {
                Flee(hit.point);
                break;
            }
        }

        if (currentState == SmartBotStates.Search) {
            foreach (RaycastHit2D hit in orbsHits)
            {
                if (CheckSameSnake(hit.collider.gameObject)) { continue; }

                // Checa se é uma orbe e guarda posição
                // TODO: Apenas procurar por orbes em estado de busca
                // TODO: Podemos continuar procurando em estado de coleta? (Se aperecer outra comida no caminho)
                var orb = hit.collider.GetComponent<OrbBehavior>();
                if (orb != null)
                {
                    orbsPositions.Add(orb.transform);
                }
            }

            if (orbsPositions.Count > 0)
            {
                // Procurar orbe mais próxima
                float minDistance = float.MaxValue;
                Vector3 closestOrbPosition = orbsPositions[0].position;

                foreach (Transform orbTransform in orbsPositions)
                {
                    float currentDistance = Vector2.Distance(orbTransform.position, owner.transform.position);

                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        closestOrbPosition = orbTransform.position;
                    }
                }

                ChangeDirection(closestOrbPosition);
            }
        }

        MoveForward();
    }

    void MoveForward()
    {
        owner.transform.position = Vector2.MoveTowards(owner.transform.position, owner.transform.position + direction, ownerMovement.speed * Time.deltaTime);
    }

    private void ChangeDirection(Vector3 closestOrbPosition)
    {
        direction = (closestOrbPosition - owner.transform.position);
        direction.z = 0;
        direction.Normalize();
    }

    private void Flee(Vector3 point)
    {
        // Getting opposite direction from collision
        direction = owner.transform.position - point;
        direction.z = 0;
        direction.Normalize();

        ownerMovement.StopCoroutine("FleeForSeconds");
        ownerMovement.StartCoroutine(FleeForSeconds(secondsToFlee));
    }

    private IEnumerator FleeForSeconds(float secondsToFlee)
    {
        currentState = SmartBotStates.Flee;
        yield return new WaitForSeconds(secondsToFlee);
        currentState = SmartBotStates.Search;
    }

    /*
    void MoveForward()
    {
        MouseRotationSnake();
        owner.transform.position = Vector2.MoveTowards(owner.transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition), ownerMovement.speed * Time.deltaTime);
    }

    void MouseRotationSnake()
    {

        direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - owner.transform.position;
        direction.z = 0.0f;

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);
    }
    */
}
