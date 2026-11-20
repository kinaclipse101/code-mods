using HG;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class captain
{
    public void AddSkinnedDefenseMatrixController()
    {
        // the item is spawned in a bool { set } and i am not hooking that thats scary </3 ,..
        Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_CaptainDefenseMatrix.CaptainDefenseMatrixItemBodyAttachment_prefab).Completed +=
            handle =>
            {
                handle.Result.AddComponent<SkinnedDefenseMatrixController>();
            };
    }

    class SkinnedDefenseMatrixController : MonoBehaviour
    {
        private ModelSkinController targetSkinController;
        public void Start()
        {
            CharacterBody body = transform.parent?.AsValidOrNull()?.GetComponent<CharacterBody>();
            var ownerMaster = body?.master?.AsValidOrNull()?.minionOwnership?.AsValidOrNull()?.ownerMaster?.AsValidOrNull();
            if (ownerMaster)
                targetSkinController = ownerMaster.GetBody()?.AsValidOrNull().modelLocator?.AsValidOrNull().modelTransform?.AsValidOrNull()?.GetComponent<ModelSkinController>();
            else
                targetSkinController = body?.modelLocator?.AsValidOrNull()?.modelTransform?.AsValidOrNull()?.GetComponent<ModelSkinController>();
            
            if (targetSkinController?.AsValidOrNull())
            {
                if(ownerMaster)
                    OnSkinApplied(targetSkinController.currentSkinIndex);
                targetSkinController.onSkinApplied += OnSkinApplied;
            }
        }

        private void OnSkinApplied(int skinIndex)
        {
            SkinDef skinDef = targetSkinController.skins[skinIndex];
            // check for specific skindef here ,.,.
            
            Transform newMeshParent = gameObject.transform.Find("Spinner");
            if (!newMeshParent?.AsValidOrNull()) return;

            GameObject oldDefenseMatrix = newMeshParent.GetChild(0).gameObject;
            GameObject newMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            //just setting these for example ,..
            newMesh.transform.SetParent(newMeshParent.transform, false);
            newMesh.transform.localPosition = oldDefenseMatrix.transform.localPosition;
            newMesh.transform.localScale = oldDefenseMatrix.transform.localScale;
            
            Destroy(oldDefenseMatrix.gameObject);
        }
    }
}