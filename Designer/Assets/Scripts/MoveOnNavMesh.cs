using UnityEngine;
using UnityEngine.AI;

public class MoveOnNavMesh : MonoBehaviour
{
    public NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 클릭으로 이동
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}