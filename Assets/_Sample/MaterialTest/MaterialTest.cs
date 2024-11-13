using UnityEngine;

namespace MySample
{
    public class MaterialTest : MonoBehaviour
    {
        #region Variables
        private Renderer renderer;

        private MaterialPropertyBlock materialPropertyBlock;
        #endregion

        void Start ()
        {
            renderer = GetComponent<Renderer>();

            //머테리얼 컬러 바꾸기
            //renderer.material.SetColor("_BaseColor", Color.red);
            // renderer.sharedMaterial.SetColor("_BaseColor", Color.red);

            //
            materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetColor("_BaseColor", Color.red);
            renderer.SetPropertyBlock(materialPropertyBlock);
        }
    }    
}
