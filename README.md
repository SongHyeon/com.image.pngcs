# Complete PNG encoding and decoding for Unity engine
Goals:
- Create simple API to read and write Texture2D objects, raw data
- Drop "Pngcs" folder in your project, no other dependencies, just works.
#
TODO:
- PNG.ReadAsync creating Texture2D in every useful TextureFormat (infered from file)
- PNG.ReadAsync with target TextureFormat argument
- Improve READ/WRITE speeds
- PNG.ReadAsync/WriteAsync byte[]
- PNG.ReadAsync with target width and height arguemnts (to preview bigger image etc.)
- PNG.ReadAsync with image rect arguemnt (read texture atlas region)

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
