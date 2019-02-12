# Goal: Complete PNG encoding and decoding for Unity engine.

TODO:
- PNG.READ creating Texture2D in every useful TextureFormat (infered from file)
- PNG.READ with target TextureFormat argument
- PNG.READ with target width and height arguemnts
- Replace use of Color[] with NativeArray<byte>, maybe?
- Improve READ/WRITE speeds

HOW TO USE:
```C#
using System.Threading.Tasks;
using UnityEngine;
using using Pngcs.Unity;
public class PngcsTest : MonoBehaviour
{
    async void Awake ()
    {
        // THIS IS HOW YOU READ:
        Texture2D texture = await PNG.ReadAsync( @"D:/input.png" );

        // THIS IS HOW YOU WRITE:
        await PNG.WriteAsync( texture , @"D:/output.png" );
        
        //remember to always release memory when texture is not needed anymore:
        Destroy( texture );
    }
}
```
