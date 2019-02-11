# Complete PNG encoding and decoding for Unity.

TODO:
- PNG.READ creating Texture2D in every useful TextureFormat (infered from file)
- PNG.READ with target TextureFormat argument
- PNG.READ with target width and height arguemnts
- Improve READ/WRITE speed
- PNG.READ for big files (10k/10k px).
  Color[] is causing problem in these cases since it's allocating 10k px * 10k px * sizeof(Color) is 1,6GB
