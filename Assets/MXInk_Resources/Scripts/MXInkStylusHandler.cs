using UnityEngine;

public class MXInkStylusHandler : MonoBehaviour
{
    [SerializeField] private GameObject mxInkModel;
    [SerializeField] private GameObject tip;
    [SerializeField] private GameObject clusterFront;
    [SerializeField] private GameObject clusterMiddle;
    [SerializeField] private GameObject clusterBack;

    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;

    public Color activeColor = Color.green;
    public Color doubleTapActiveColor = Color.cyan;
    public Color defaultColor = Color.white;

    private StylusInputs stylus;
    private LineRenderer currentLine;
    private bool isDrawing;
    
    // Defined action names.
    private const string MX_Ink_Pose_Right = "aim_right";
    private const string MX_Ink_Pose_Left = "aim_left";
    private const string MX_Ink_TipForce = "tip";
    private const string MX_Ink_MiddleForce = "middle";
    private const string MX_Ink_ClusterFront = "front";
    private const string MX_Ink_ClusterBack = "back";
    private const string MX_Ink_ClusterBack_DoubleTap = "back_double_tap";
    private const string MX_Ink_ClusterFront_DoubleTap = "front_double_tap";
    private const string MX_Ink_Docked = "docked";
    private const string MX_Ink_Haptic_Pulse = "haptic_pulse";
    private float hapticClickDuration = 0.011f;
    private float hapticClickAmplitude = 1.0f;

    private void UpdatePose()
    {
        var leftDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandLeft);
        var rightDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandRight);

        bool stylusIsOnLeftHand = leftDevice.Contains("logitech");
        bool stylusIsOnRightHand = rightDevice.Contains("logitech");

        stylus.isActive = stylusIsOnLeftHand || stylusIsOnRightHand;
        stylus.isOnRightHand = stylusIsOnRightHand;
        
        string MX_Ink_Pose = stylus.isOnRightHand ? MX_Ink_Pose_Right : MX_Ink_Pose_Left;

        mxInkModel.SetActive(stylus.isActive);
        rightController.SetActive(!stylus.isOnRightHand || !stylus.isActive);
        leftController.SetActive(stylus.isOnRightHand || !stylus.isActive);

        if (OVRPlugin.GetActionStatePose(MX_Ink_Pose, out OVRPlugin.Posef handPose))
        {
            transform.localPosition = handPose.Position.FromFlippedZVector3f();
            transform.localRotation = handPose.Orientation.FromFlippedZQuatf();
            stylus.inkingPose.position = transform.localPosition;
            stylus.inkingPose.rotation = transform.localRotation;
        }
    }

    void Update()
    {
        OVRInput.Update();
        UpdatePose();

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_TipForce, out stylus.tipValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_TipForce}");
        }

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_MiddleForce, out stylus.clusterMiddleValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_MiddleForce}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront, out stylus.clusterFrontValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack, out stylus.clusterBackValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront_DoubleTap, out stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack_DoubleTap, out stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_Docked, out stylus.docked))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_Docked}");
        }

        stylus.any = stylus.tipValue > 0 || stylus.clusterFrontValue ||
                        stylus.clusterMiddleValue > 0 || stylus.clusterBackValue ||
                        stylus.clusterBackDoubleTapValue;

        tip.GetComponent<MeshRenderer>().material.color = stylus.tipValue > 0 ? activeColor : defaultColor;
        clusterFront.GetComponent<MeshRenderer>().material.color = stylus.clusterFrontValue ? activeColor : defaultColor;
        clusterMiddle.GetComponent<MeshRenderer>().material.color = stylus.clusterMiddleValue > 0 ? activeColor : defaultColor;

        if (stylus.clusterBackValue)
        {
            clusterBack.GetComponent<MeshRenderer>().material.color = stylus.clusterBackValue ? activeColor : defaultColor;
        }
        else
        {
            clusterBack.GetComponent<MeshRenderer>().material.color = stylus.clusterBackDoubleTapValue ? doubleTapActiveColor : defaultColor;
        }


        if (stylus.clusterBackDoubleTapValue)
        {
            TriggerHapticClick();
        }
        
        DrawLine();
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        OVRPlugin.Hand holdingHand = stylus.isOnRightHand ? OVRPlugin.Hand.HandRight : OVRPlugin.Hand.HandLeft;
        OVRPlugin.TriggerVibrationAction(MX_Ink_Haptic_Pulse, holdingHand, duration, amplitude);
    }

    public void TriggerHapticClick()
    {
        TriggerHapticPulse(hapticClickAmplitude, hapticClickDuration);
    }
    
    private void DrawLine()
    {
        if (stylus.tipValue > 0)
        {
            if (!isDrawing)
            {
                // Start a new line if not currently drawing
                currentLine = new GameObject("Line").AddComponent<LineRenderer>();
                currentLine.positionCount = 0;
                currentLine.material = new Material(Shader.Find("Sprites/Default"));
                currentLine.startColor = defaultColor;
                currentLine.endColor = defaultColor;
                currentLine.startWidth = 0.0025f;
                currentLine.endWidth = 0.0025f;
                isDrawing = true;
            }

            // Update line with current position
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, stylus.inkingPose.position);
        }
        else if (isDrawing)
        {
            // Stop drawing
            isDrawing = false;
            currentLine = null;
        }
    }

}
