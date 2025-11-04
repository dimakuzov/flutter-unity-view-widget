using UnityEngine;
using System.Collections;

public class LookAt : MonoBehaviour
{
    public Transform lookAt;
    public float smooth = 5F;
    public float lookAhead = 0F;

    Quaternion lastRotation;
    Quaternion goalRotation;

    GameObject DummyT;
    public float Offset;

    void FixedUpdate()
    {
        if(!DummyT)
            DummyT = new GameObject();

        // ToDo: needed for now to prevent the error: NullReferenceException: Object reference not set to an instance of an object.        at LookAt.FixedUpdate()
        if (DummyT == null || lookAt == null) return;

        DummyT.transform.position = lookAt.position;

        if (!lookAt.name.Contains("Directional Light"))
        {
            Vector3 TempV3 = DummyT.transform.position;
            TempV3.z = -Offset;
            DummyT.transform.position = TempV3;
        }

        Debug.DrawLine(DummyT.transform.position, transform.position, Color.gray);

        Vector3 difference = DummyT.transform.TransformPoint(new Vector3(lookAhead, 0F, 0F)) - transform.position;
        Vector3 upVector = DummyT.transform.position - DummyT.transform.TransformPoint(Vector3.down);
        goalRotation = Quaternion.LookRotation(difference, upVector);
    }

    void Awake()
    {
        lastRotation = transform.rotation;
        goalRotation = lastRotation;
    }

    void LateUpdate()
    {
        lastRotation = Quaternion.Slerp(lastRotation, goalRotation, smooth * Time.deltaTime);
        transform.rotation = lastRotation;
    }

}