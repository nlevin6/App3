using UnityEngine;

[CreateAssetMenu(fileName = "NewRecoilPattern", menuName = "Recoil/RecoilPattern")]
public class RecoilPattern : ScriptableObject
{
    public float[] horizontalRecoil;
    public float[] verticalRecoil;
}
