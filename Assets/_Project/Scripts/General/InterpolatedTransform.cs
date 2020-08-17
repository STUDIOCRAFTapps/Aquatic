using UnityEngine;

public class InterpolatedTransform : MonoBehaviour {

    #region Monobehaviour and References
    public bool disableInterpolation = false;
    
    private Vector3 currentOffset;
    private TransformData[] lastTransforms;
    private int newTransformIndex;

    public void Start () {
        CameraNavigator.inst.OnApplyLerpEvent += ApplyLerp;
        CameraNavigator.inst.OnRevertLerpEvent += RevertLerp;
    }

    private void OnDestroy () {
        if(CameraNavigator.inst != null) {
            CameraNavigator.inst.OnApplyLerpEvent -= ApplyLerp;
            CameraNavigator.inst.OnRevertLerpEvent -= RevertLerp;
        }
    }
    #endregion

    #region Set Values
    public void SetTransformPosition (Vector3 position) {
        transform.localPosition = position;
        lastTransforms[0].position = position;
        lastTransforms[1].position = position;
    }

    public void SetTransformRotation (Quaternion rotation) {
        transform.localRotation = rotation;
        lastTransforms[0].rotation = rotation;
        lastTransforms[1].rotation = rotation;
    }

    public void SetTransformScale (Vector3 scale) {
        transform.localScale = scale;
        lastTransforms[0].scale = scale;
        lastTransforms[1].scale = scale;
    }

    public void SetOffset (Vector3 offset) {
        currentOffset = offset;
    }

    void OnEnable () {
        ClearAndResampleData();
    }
    #endregion

    private void ApplyLerp () {
        if(disableInterpolation) {
            return;
        }

        TransformData newestTransform = lastTransforms[newTransformIndex];
        TransformData olderTransform = lastTransforms[OldTransformIndex()];

        if(disableInterpolation) {
            return;
        }
        transform.localPosition = Vector3.Lerp(
                                    olderTransform.position,
                                    newestTransform.position,
                                    InterpolationManager.InterpolationFactor);
        transform.localRotation = Quaternion.Slerp(
                                    olderTransform.rotation,
                                    newestTransform.rotation,
                                    InterpolationManager.InterpolationFactor);
        transform.localScale = Vector3.Lerp(
                                    olderTransform.scale,
                                    newestTransform.scale,
                                    InterpolationManager.InterpolationFactor);

        transform.localPosition += currentOffset;
    }

    private void RevertLerp () {
        if(disableInterpolation) {
            return;
        }
        ApplyData(lastTransforms[newTransformIndex]);
    }

    void FixedUpdate () {
        if(disableInterpolation) {
            return;
        }

        // Swap the new data to the old and fill the new data with newest data.
        newTransformIndex = OldTransformIndex();
        lastTransforms[newTransformIndex] = SampleData();
    }

    #region Transform Data
    private int OldTransformIndex () {
        return 1 - newTransformIndex;
    }

    private TransformData SampleData () {
        return new TransformData(
            transform.localPosition,
            transform.localRotation,
            transform.localScale);
    }

    private void ApplyData (TransformData data) {
        transform.localPosition = data.position;
        transform.localRotation = data.rotation;
        transform.localScale = data.scale;
    }

    private void ClearAndResampleData () {
        lastTransforms = new TransformData[2];
        TransformData t = new TransformData(
            transform.localPosition,
            transform.localRotation,
            transform.localScale);
        lastTransforms[0] = t;
        lastTransforms[1] = t;
        newTransformIndex = 0;
    }

    private struct TransformData {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformData (Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }
    #endregion
}