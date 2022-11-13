using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
// TODO copy from the other game!
{
    public Sprite[] sprites;
    public Texture[] textures;

    #region Core

    // Start is called before the first frame update
    void Start()
    {   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #endregion

    #region Util

    public Sprite GetSprite(string name) {
        foreach (Sprite sprite in sprites) {
            if (sprite.name == name)
                return sprite;
        }
        return null;
    }

    public Texture GetTexture(string name) {
        foreach (Texture texture in textures) {
            if (texture.name == name)
                return texture;
        }
        return null;
    }

    private void BuildTexturix() {

    }

    #endregion
}
