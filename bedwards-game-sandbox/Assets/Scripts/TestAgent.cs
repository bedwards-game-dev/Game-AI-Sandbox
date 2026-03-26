using UnityEngine;

public class TestAgent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActionList.ActionList.Instance.Add(new ActionList.ActionList.MoveAction(this.gameObject, new Vector3(0, 5, 0), 2.0f, ActionList.ActionList.EaseInOut));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
