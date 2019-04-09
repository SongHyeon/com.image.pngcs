# Complete PNG encoding and decoding for Unity engine
GOALS:
- Create simple API to read and write Texture2D objects, raw data
- Drop "Pngcs" folder in your project, no other dependencies, just works.
#
API:
- Texture2D PNG.Read
- Texture2D PNG.ReadAsync (experimental)
- Color[] PNG.ReadColors
- Color[] PNG.ReadColorsAsync (experimental)
- PNG.Write
- PNG.WriteAsync
- PNG.WriteLargeAsync (experimental)
- PNG.WriteGrayscaleAsync
#
TODO:
- PNG.Read creating Texture2D in every useful TextureFormat, infered from file (partially done already)
- Improve READ/WRITE speeds
- PNG.Read/Write byte[]
- PNG.Read with target width and height arguments (to preview bigger image etc.)
- PNG.Read with image rect argument (read texture atlas region)
#
HOW TO USE:
```C#
using UnityEngine;
using Pngcs.Unity;
public class PngcsTest : MonoBehaviour
{
    void Awake ()
    {
        // THIS IS HOW YOU READ:
        Texture2D texture = PNG.Read( @"D:/input.png" );

        // THIS IS HOW YOU WRITE:
        PNG.Write( texture , @"D:/output.png" );
        
        //remember to always release memory when texture is not needed anymore:
        Destroy( texture );
    }
}
```
#
REQUIREMENTS:
- Unity 2017.1
- Scripting runtime version: C# 4.x
#
