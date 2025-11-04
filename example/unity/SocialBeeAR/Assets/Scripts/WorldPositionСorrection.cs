using System.Collections;
using System.Collections.Generic;
using SocialBeeAR;
using SocialBeeARDK;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class WorldPositionСorrection : MonoBehaviour {

    //[SerializeField] private ARSessionOrigin arSessionOrigin;
    [SerializeField] private XROrigin arSessionOrigin;
    // [SerializeField] private Text yText;

    private Camera camera;
    private Vector3 firstPos;
    private Vector3 secondPos;
    List<GameObject> anchorObjList = new List<GameObject>();
    private List<Vector3> anchorPositions = new List<Vector3>();
    private GameObject onBoardAnchor;
    private Vector3 onBoardAnchorPosition;

    private float yFirstRotation;
    private float ySecondRotation;

    private float allRotation = 0;
    private Vector3 allMoving;

    private static WorldPositionСorrection _instance;

    public static WorldPositionСorrection Instance {
        get { return _instance; }
    }

    [HideInInspector]
    private static IAnchorManager ActiveAnchorManager =>
        SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
            ? AnchorManager.Instance
            : WayspotAnchorManager.Instance;

    private void Awake() {
        _instance = this;
    }
    
    void Start() {
        camera = Camera.main;
        // InvokeRepeating("ShowRotated", 5, 1);
    }

    // void ShowRotated() {
    //     yText.text = $"Y = {camera.transform.rotation.eulerAngles.y.ToString("R")}\npos = {camera.transform.position}";
    // }

    public void FirstRotation() {
        RecordManager.Instance.HideARContent();
        firstPos = camera.transform.position;
        yFirstRotation = camera.transform.rotation.eulerAngles.y;
        
        
        if(ActiveAnchorManager.GetAnchorObjectList() != null) {
            if(ActiveAnchorManager.GetAnchorObjectList().Count > anchorObjList.Count) {
                anchorObjList = ActiveAnchorManager.GetAnchorObjectList();
                for (int i = 0; i < anchorObjList.Count; i++) {
                    anchorPositions.Add(anchorObjList[i].transform.position);
                    print($"-=- anchorPositions.Add = {anchorObjList[i].transform.position}");
                }
            }
        }
        
        if (OnBoardManager.Instance.anchorObj != null) {
            print($"-=- OnBoardManager.Instance.anchorObj != null");
            onBoardAnchor = OnBoardManager.Instance.anchorObj;
            onBoardAnchorPosition = onBoardAnchor.transform.position;
        }

        // when we call it "camera.transform.rotation" will be changed (will be on 180 degree more)
        RotateArSessionOrigin(yFirstRotation - 180);
        
        // keep all rotation in current session
        allRotation += yFirstRotation - 180;
        print($"-=- WorldPositionСorrection FirstRotation()\nyFirstRotation = {yFirstRotation.ToString("R")}, firstPos = {firstPos}, anchorObjList.Count = {anchorObjList.Count}");
    }

    public void CleanAnchors() {
        anchorPositions.Clear();
        anchorObjList.Clear();
    }

    public void SecondRotation() {
        secondPos = camera.transform.position;
        ySecondRotation = camera.transform.rotation.eulerAngles.y;

        StartCoroutine(SecondRotationWaitFlipCamera());
    }

    IEnumerator SecondRotationWaitFlipCamera() {
        yield return new WaitForSeconds(1); // <-- need 1 second
        
        Vector3 subsequentErrors = Vector3.zero;

        RotateArSessionOrigin(ySecondRotation - yFirstRotation); // <-- it works

        // keep all rotation in current session
        allRotation += ySecondRotation - yFirstRotation;
        
        subsequentErrors = camera.transform.position - secondPos;        
        print($"-=- WorldPositionСorrection SecondRotationWaitFlipCamera()\nfirstPos = {firstPos.ToString("R")}, secondPos = {secondPos.ToString("R")}, subsequentErrors = {subsequentErrors}");
        
        MoveArSessionOrigin(Vector3.zero - firstPos + secondPos + subsequentErrors); // <-- it works
        
        // keep all moving in current session
        allMoving += (Vector3.zero - firstPos + secondPos + subsequentErrors);
        // yield return new WaitForSeconds(4);
        // MoveArSessionOrigin(subsequentErrors);
        MoveBackAnchors();
        
        RecordManager.Instance.ShowARContent();
    }

    void RotateArSessionOrigin(float y) {
        // Dev ToDo: arSessionOrigin.MakeContentAppearAt(arSessionOrigin.transform, Vector3.zero, Quaternion.Euler(0, y, 0));
    }

    void MoveArSessionOrigin(Vector3 vec) {
        // Dev ToDo: arSessionOrigin.MakeContentAppearAt(arSessionOrigin.transform, vec, Quaternion.identity);
    }

    // void RotateAnchors(float y) {
    //     foreach (var anchorObj in anchorObjList) {
    //         anchorObj.transform.RotateAround(Vector3.zero, Vector3.up, y);
    //     }
    // }

    void MoveBackAnchors() {
        for (int i = 0; i < anchorObjList.Count; i++) {
            anchorObjList[i].transform.position = anchorPositions[i];
            print($"-=- anchorObjList[{i}].transform.position = {anchorPositions[i]}");
        }

        if(onBoardAnchor) {
            print($"-=- if(onBoardAnchor)");
            onBoardAnchor.transform.position = onBoardAnchorPosition;
        }
    }

    public void MoveAndRotateBackToInit() {
        if(allRotation != 0) {
            
            // Dev ToDo: arSessionOrigin.MakeContentAppearAt(arSessionOrigin.transform, Vector3.zero,
              //  Quaternion.Euler(0, -allRotation, 0));
            //arSessionOrigin.MakeContentAppearAt(arSessionOrigin.transform, -allMoving, Quaternion.identity);
            allRotation = 0;
            allMoving = Vector3.zero;
        }
    }
}
