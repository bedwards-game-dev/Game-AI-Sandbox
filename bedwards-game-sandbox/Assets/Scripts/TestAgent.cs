using UnityEngine;

public class TestAgent : MonoBehaviour
{
    [SerializeField] 
    private AnimationCurve testCurve;

    [SerializeField] private Vector3 endPosition = new Vector3(0, 0, 0);
    [SerializeField] private float moveDelay = 2.0f;
    [SerializeField] private float moveTime = 5.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActionList.ActionList.Instance.Add(new ActionList.ActionList.MoveAction(gameObject, endPosition, moveTime, testCurve, moveDelay));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
