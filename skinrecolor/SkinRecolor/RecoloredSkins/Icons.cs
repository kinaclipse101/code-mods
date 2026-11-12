using System.Threading.Tasks;
using RoR2;
using UnityEngine;

namespace SkinRecolor.SkinManager;

public static class Icons
{
    public static async Task IconReplacement(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef)
    {
        Texture2D newIcon = new Texture2D(Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.width), Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.height));
        var pixels = recoloredSkinDef.icon.texture.GetPixels(  
            Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.x), 
            Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.y), 
            Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.width), 
            Mathf.FloorToInt(recoloredSkinDef.icon.textureRect.height) );
        newIcon.SetPixels(pixels);
        newIcon.Apply();
        
        Texture2D newTexture = await Utils.hsvModifyTexture(newIcon, 
            recoloredSkinJson, 
            dontAdd);
            
        Sprite newIconSprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), new Vector2(newTexture.width / 2, newTexture.height / 2));
        recoloredSkinDef.icon = newIconSprite;
    }
}