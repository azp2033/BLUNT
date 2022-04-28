using UnityEngine;

namespace BLUNT
{
    public class Ldr
    {
        public static void Init()
        {
            Object.DontDestroyOnLoad(new GameObject(null, typeof(Main)) { hideFlags = HideFlags.HideAndDontSave });
        }
    }
}
