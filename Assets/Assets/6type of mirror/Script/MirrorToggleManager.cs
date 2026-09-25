using UnityEngine;

public class MirrorToggleManager : MonoBehaviour
{
    [SerializeField] private GameObject[] mirrors; // The 8 mirrors
    private int activeMirrorIndex = -1; // Index of the mirror that is currently on

    public void ToggleMirror(int mirrorIndex)
    {
        if (mirrorIndex < 0 || mirrorIndex >= mirrors.Length) return;

        // Pressing the same mirror button again turns it off.
        if (activeMirrorIndex == mirrorIndex)
        {
            mirrors[mirrorIndex].SetActive(false);
            activeMirrorIndex = -1;
        }
        else
        {
            // Turn the previous mirror off first.
            if (activeMirrorIndex != -1)
                mirrors[activeMirrorIndex].SetActive(false);

            mirrors[mirrorIndex].SetActive(true);
            activeMirrorIndex = mirrorIndex;
        }
    }

    public void ToggleMirror_0() { ToggleMirror(0); }
    public void ToggleMirror_1() { ToggleMirror(1); }
    public void ToggleMirror_2() { ToggleMirror(2); }
    public void ToggleMirror_3() { ToggleMirror(3); }
    public void ToggleMirror_4() { ToggleMirror(4); }
    public void ToggleMirror_5() { ToggleMirror(5); }
    public void ToggleMirror_6() { ToggleMirror(6); }
    public void ToggleMirror_7() { ToggleMirror(7); }
}
