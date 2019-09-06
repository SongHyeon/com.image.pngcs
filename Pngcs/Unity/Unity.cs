using System.Threading.Tasks;
using IO = System.IO;

using UnityEngine;

namespace Pngcs.Unity
{
    public static class PNG
    {

        /// <summary> Write PNG to file, async </summary>
        public static async Task WriteAsync
        (
            Texture2D texture ,
            string filePath
        )
        {
            try
            {
                Color[] pixels = texture.GetPixels();
                int width = texture.width;
                int height = texture.height;
                var format = texture.format;
                int bitDepth = GetBitDepth( format );
                bool alpha = GetIsAlpha( format );
                bool greyscale = GetIsGreyscale( format );
                
                await WriteAsync(
                    pixels:     pixels ,
                    width:      width ,
                    height:     height ,
                    bitDepth:   bitDepth ,
                    alpha:      alpha ,
                    greyscale:  greyscale ,
                    filePath:   filePath
                ).ConfigureAwait(true);
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }
        public static async Task WriteAsync
        (
            Color[] pixels ,
            int width ,
            int height ,
            int bitDepth ,
            bool alpha ,
            bool greyscale ,
            string filePath
        )
        {
            try
            {
                await Task.Run( ()=> {
                    Write(
                        pixels:     pixels ,
                        width:      width ,
                        height:     height ,
                        bitDepth:   bitDepth ,
                        alpha:      alpha ,
                        greyscale:  greyscale ,
                        filePath:   filePath
                    );
                    return Task.CompletedTask;
                } ).ConfigureAwait(true);
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }

        /// <summary>
        /// Write large texture to PNG, async.
        /// Execute from Main Thread.
        /// </summary>
        public static async Task WriteLargeAsync
        (
            Texture2D texture ,
            string filePath
        )
        {
            await WriteLargeAsync( texture , filePath , FillLine ).ConfigureAwait(true);
        }

        /// <summary>
        /// Write large texture to PNG, async. FillLine( texture , image line , image info , row ).
        /// Execute from Main Thread.
        /// </summary>
        public static async Task WriteLargeAsync
        (
            Texture2D texture ,
            string filePath ,
            System.Func<Texture2D,ImageLine,ImageInfo,int,Task> fillLine
        )
        {
            try
            {
                var info = new ImageInfo(
                    texture.width ,
                    texture.height ,
                    GetBitDepth( texture.format ) ,
                    GetIsAlpha( texture.format ) ,
                    GetIsGreyscale(texture. format ) ,
                    false//not implemented here yet//bitDepth==4
                );
                    
                // open image for writing:
                PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
                
                // add some optional metadata (chunks)
                var meta = writer.GetMetadata();
                meta.SetTimeNow( 0 );// 0 seconds fron now = now

                int numRows = info.Rows;
                int numCols = info.Cols;
                ImageLine line = new ImageLine( info );
                for( int row=0 ; row<numRows ; row++ )
                {
                    //fill line on main or different thread:
                    await fillLine( texture , line , info , row );
                    
                    //write line on another thread:
                    await Task.Run(
                        ()=> writer.WriteRow( line , row )
                    );
                }
                writer.End();
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }

        /// <summary> Write PNG to file </summary>
        public static void Write
        (
            Texture2D texture ,
            string filePath
        )
        {
            if( texture.isReadable==false )
            {
                Debug.LogError( $"'{ texture.name }' is not readable so the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings." );
                return;
            }
            var pixels = texture.GetPixels();
            var format = texture.format;
            int bitDepth = GetBitDepth( format );
            bool alpha = GetIsAlpha( format );
            bool greyscale = GetIsGreyscale( format );
            Write(
                pixels:     pixels ,
                width:      texture.width ,
                height:     texture.height ,
                bitDepth:   bitDepth ,
                alpha:      alpha ,
                greyscale:  greyscale ,
                filePath:   filePath
            );
        }
        public static void Write
        (
            Color[] pixels ,
            int width ,
            int height ,
            int bitDepth ,
            bool alpha ,
            bool greyscale ,
            string filePath
        )
        {
            var info = new ImageInfo(
                width ,
                height ,
                bitDepth ,
                alpha ,
                greyscale ,
                false//not implemented here yet//bitDepth==4
            );
                
            // open image for writing:
            PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
            
            // add some optional metadata (chunks)
            var meta = writer.GetMetadata();
            meta.SetTimeNow( 0 );// 0 seconds fron now = now

            int numRows = info.Rows;
            int numCols = info.Cols;
            ImageLine imageline = new ImageLine( info );
            for( int row=0 ; row<numRows ; row++ )
            {
                //fill line:
                if( greyscale==false )
                {
                    if( alpha )
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            RGBA rgba = ToRGBA( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
                            ImageLineHelper.SetPixel( imageline , col , rgba.R , rgba.G , rgba.B , rgba.A );
                        }
                    }
                    else
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            RGB rgb = ToRGB( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
                            ImageLineHelper.SetPixel( imageline , col , rgb.r , rgb.g , rgb.b );
                        }
                    }
                }
                else
                {
                    if( alpha==false )
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            int r = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].r , bitDepth );
                            ImageLineHelper.SetPixel( imageline , col , r );
                        }
                    }
                    else
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            int a = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].a , bitDepth );
                            ImageLineHelper.SetPixel( imageline , col , a );
                        }
                    }
                }
                
                //write line:
                writer.WriteRow( imageline , row );
            }
            writer.End();
        }

        static async Task FillLine
        (
            Texture2D texture ,
            ImageLine line ,
            ImageInfo info ,
            int row
        )
        {
            int numCols = info.Cols;
            int numRows = info.Rows;
            int bitDepth = info.BitDepth;
            bool alpha = info.Alpha;
            bool greyscale = info.Greyscale;

            //fill line:
            Color[] pixels = texture.GetPixels( 0 , row , numCols , 1 );
            if( greyscale==false )
            {
                if( alpha )
                {
                    for( int col=0 ; col<numCols ; col++ )
                    {
                        RGBA rgba = ToRGBA( pixels[col] , bitDepth );
                        ImageLineHelper.SetPixel( line , col , rgba.R , rgba.G , rgba.B , rgba.A );
                    }
                }
                else
                {
                    for( int col=0 ; col<numCols ; col++ )
                    {
                        RGB rgb = ToRGB( pixels[col] , bitDepth );
                        ImageLineHelper.SetPixel( line , col , rgb.r , rgb.g , rgb.b );
                    }
                }
            }
            else
            {
                if( alpha==false )
                {
                    for( int col=0 ; col<numCols ; col++ )
                    {
                        int r = ToInt( pixels[col].r , bitDepth );
                        ImageLineHelper.SetPixel( line , col , r );
                    }
                }
                else
                {
                    for( int col=0 ; col<numCols ; col++ )
                    {
                        int a = ToInt( pixels[col].a , bitDepth );
                        ImageLineHelper.SetPixel( line , col , a );
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary> Writes 16-bit grayscale image </summary>
        public static async Task WriteGrayscaleAsync
        (
            ushort[] pixels ,
            int width ,
            int height ,
            bool alpha ,
            string filePath
        )
        {
            try {
                var info = new ImageInfo(
                    width ,
                    height ,
                    16 ,
                    alpha ,
                    true ,
                    false//not implemented here yet
                );
                await Task.Run( ()=> {
                    
                    // open image for writing:
                    PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
                    
                    // add some optional metadata (chunks)
                    var meta = writer.GetMetadata();
                    meta.SetTimeNow( 0 );// 0 seconds fron now = now

                    int numRows = info.Rows;
                    int numCols = info.Cols;
                    ImageLine imageline = new ImageLine( info );
                    for( int row=0 ; row<numRows ; row++ )
                    {
                        //fill line:
                        if( alpha==false )
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                ushort r = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                                ImageLineHelper.SetPixel( imageline , col , r );
                            }
                        }
                        else
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                ushort a = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                                ImageLineHelper.SetPixel( imageline , col , a );
                            }
                        }
                        
                        //write line:
                        writer.WriteRow( imageline , row );
                    }
                    writer.End();

                } );
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }

        /// <summary> Writes 8-bit grayscale image </summary>
        public static async Task WriteGrayscaleAsync
        (
            byte[] pixels ,
            int width ,
            int height ,
            bool alpha ,
            string filePath
        )
        {
            try {
                var info = new ImageInfo(
                    width ,
                    height ,
                    8 ,
                    alpha ,
                    true ,
                    false//not implemented here yet
                );
                await Task.Run( ()=> {
                    
                    // open image for writing:
                    PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
                    
                    // add some optional metadata (chunks)
                    var meta = writer.GetMetadata();
                    meta.SetTimeNow( 0 );// 0 seconds fron now = now

                    int numRows = info.Rows;
                    int numCols = info.Cols;
                    ImageLine imageline = new ImageLine( info );
                    for( int row=0 ; row<numRows ; row++ )
                    {
                        //fill line:
                        if( alpha==false )
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                byte r = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                                ImageLineHelper.SetPixel( imageline , col , r );
                            }
                        }
                        else
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                byte a = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                                ImageLineHelper.SetPixel( imageline , col , a );
                            }
                        }
                        
                        //write line:
                        writer.WriteRow( imageline , row );
                    }
                    writer.End();

                } );
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }

        /// <summary> Create Texture2D from PNG file, async </summary>
        public static async Task<Texture2D> ReadAsync
        (
            string filePath
        )
        {
            Texture2D result = null;
            try
            {
                var readout = await ReadColorsAsync( filePath ).ConfigureAwait(true);
                result = new Texture2D( readout.width , readout.height , readout.textureFormatInfered , false , true );
                result.SetPixels( readout.pixels );
                result.Apply();
            }
            catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            return result;
        }

        /// <summary> Create Texture2D from PNG file </summary>
        public static Texture2D Read
        (
            string filePath
        )
        {
            Texture2D result = null;
            try
            {
                var readout = ReadColors( filePath );
                result = new Texture2D( readout.width , readout.height , readout.textureFormatInfered , false , true );
                result.SetPixels( readout.pixels );
                result.Apply();
            }
            catch( System.Exception ex ) { Debug.LogException(ex); }
            return result;
        }
        
        /// <summary> Create Color[] from PNG file, async </summary>
        public static async Task<ReadColorsResult> ReadColorsAsync
        (
            string filePath
        )
        {
            ReadColorsResult results = new ReadColorsResult{
                pixels = null ,
                width = -1 ,
                height = -1 ,
                textureFormatInfered = 0
            };
            PngReader reader = null;
            try
            {
                reader = FileHelper.CreatePngReader( filePath );
                var info = reader.ImgInfo;
                results.height = info.Rows;
                results.width = info.Cols;
                results.pixels = new Color[ results.width * results.height ];
            
                int channels = info.Channels;
                int bitDepth = info.BitDepth;
                if( info.Indexed ) { throw new System.NotImplementedException( "indexed png not implemented" ); }

                //select appropriate texture format:
                results.textureFormatInfered = GetTextureFormat( bitDepth , channels );
                
                //create pixel array:
                await Task.Run( ()=> {
                    ReadSamples( reader , results );
                } );
            }
            catch ( System.Exception ex )
            {
                Debug.LogException( ex );
                if( results.pixels==null )
                {
                    results.width = 2;
                    results.height = 2;
                    results.pixels = new Color[ results.width * results.height ];
                }
                if( results.textureFormatInfered==0 ) { results.textureFormatInfered = TextureFormat.RGBA32; }
            }
            finally
            {
                if( reader!=null ) reader.End();
            }
            return results;
        }

        /// <summary> Create Color[] from PNG file </summary>
        public static ReadColorsResult ReadColors
        (
            string filePath
        )
        {
            ReadColorsResult results = new ReadColorsResult{
                pixels = null ,
                width = -1 ,
                height = -1 ,
                textureFormatInfered = 0
            };
            PngReader reader = null;
            try
            {
                reader = FileHelper.CreatePngReader( filePath );
                var info = reader.ImgInfo;
                results.height = info.Rows;
                results.width = info.Cols;
                results.pixels = new Color[ results.width * results.height ];
            
                int channels = info.Channels;
                int bitDepth = info.BitDepth;
                if( info.Indexed ) { throw new System.NotImplementedException( "indexed png not implemented" ); }

                //select appropriate texture format:
                results.textureFormatInfered = GetTextureFormat( bitDepth , channels );
                
                //create pixel array:
                ReadSamples( reader , results );
            }
            catch ( System.Exception ex )
            {
                Debug.LogException( ex );
                if( results.pixels==null )
                {
                    results.width = 2;
                    results.height = 2;
                    results.pixels = new Color[ results.width * results.height ];
                }
                if( results.textureFormatInfered==0 ) { results.textureFormatInfered = TextureFormat.RGBA32; }
            }
            finally
            {
                if( reader!=null ) reader.End();
            }
            return results;
        }

        /// <summary> Reads samples using given reader </summary>
        static void ReadSamples ( PngReader reader , ReadColorsResult results )
        {
            var info = reader.ImgInfo;
            int channels = info.Channels;
            int bitDepth = info.BitDepth;
            float max = GetBitDepthMaxValue( bitDepth );
            if( !info.Indexed )
            {
                if( !info.Packed )
                {
                    for( int row=0 ; row<results.height ; row++ )
                    {
                        ImageLine imageLine = reader.ReadRowInt( row );
                        var scanline = imageLine.Scanline;
                        for( int col=0 ; col<results.width ; col++ )
                        {
                            int i = col*channels;
                            RGBA rgba;
                            if( channels==4 )
                            {
                                rgba = new RGBA{
                                    R = scanline[i] ,
                                    G = scanline[i+1] ,
                                    B = scanline[i+2] ,
                                    A = scanline[i+3]
                                };
                            }
                            else if( channels==3 )
                            {
                                rgba = new RGBA{
                                    R = scanline[i] ,
                                    G = scanline[i+1] ,
                                    B = scanline[i+2] ,
                                    A = int.MaxValue
                                };
                            }
                            else if( channels==2 )
                            {
                                int val = scanline[i+1];
                                int a = scanline[i];
                                rgba = new RGBA{
                                    R = val , G = val , B = val ,
                                    A = a
                                };
                            }
                            else if( channels==1 )
                            {
                                int val = scanline[i];
                                rgba = new RGBA{ R = val , G = val , B = val , A = val };
                            }
                            else { throw new System.Exception( $"{channels} channels not implemented" ); }

                            Color color = new Color{
                                r = (float)rgba.R / max ,
                                g = (float)rgba.G / max ,
                                b = (float)rgba.B / max ,
                                a = (float)rgba.A / max
                            };
                            results.pixels[ IndexPngToTexture( row , col , results.height , results.width ) ] = color;
                        }
                    }
                }
                else
                {
                    if( bitDepth==4 )
                    {
                        for( int row=0 ; row<results.height ; row++ )
                        {
                            ImageLine imageLine = reader.ReadRowByte( row );
                            var scanline = imageLine.ScanlineB;
                            for( int col=0 ; col<results.width/2 ; col++ )
                            {
                                byte b = scanline[ col ];
                                int hiNybble = (b & 0xF0) >> 4; //Left hand nybble
                                int loNyblle = (b & 0x0F);      //Right hand nybble
                                float val1 = (float)hiNybble / max;
                                float val2 = (float)loNyblle / max;
                                Color color_1 = new Color{
                                    r = val1 ,
                                    g = val1 ,
                                    b = val1 ,
                                    a = val1
                                };
                                Color color_2 = new Color{
                                    r = val2 ,
                                    g = val2 ,
                                    b = val2 ,
                                    a = val2
                                };
                                results.pixels[ IndexPngToTexture( row , col*2+0 , results.height , results.width ) ] = color_1;
                                results.pixels[ IndexPngToTexture( row , col*2+1 , results.height , results.width ) ] = color_2;
                            }
                        }
                    }
                    else if( bitDepth==2 )
                    {
                        throw new System.Exception( $"bit depth {bitDepth} not implemented" );
                    }
                    else if( bitDepth==1 )
                    {
                        int BIT ( int index , byte b ) => (b&(1<<index))!=0 ? 1<<index : 0;
                        for( int row=0 ; row<results.height ; row++ )
                        {
                            ImageLine imageLine = reader.ReadRowByte( row );
                            var scanline = imageLine.ScanlineB;
                            
                            for( int col=0 ; col<results.width/8 ; col++ )
                            {
                                for( int bit=0 ; bit<8 ; bit++ )
                                {
                                    float val = BIT( bit , scanline[ col ] );
                                    Color color = new Color{ r = val , g = val , b = val , a = val };
                                    results.pixels[ IndexPngToTexture( row , col*8+bit , results.height , results.width ) ] = color;
                                }
                            }
                        }
                    }
                    else throw new System.Exception( $"bit depth {bitDepth} not implemented" );
                }
            }
            else { throw new System.NotImplementedException( "indexed png not implemented" ); }
        }

        /// <summary> Creates ImageInfo object based on given png </summary>
        public static ImageInfo ReadImageInfo
        (
            string filePath
        )
        {
            ImageInfo imageInfo = null;
            PngReader reader = null;
            try
            {
                reader = FileHelper.CreatePngReader( filePath );
                imageInfo = reader.ImgInfo;
            }
            catch ( System.Exception ex ) { Debug.LogException( ex ); }
            finally
            {
                if( reader!=null ) reader.End();
            }
            return imageInfo;
        }

        /// <summary> Texture2D's rows start from the bottom while PNG from the top. Hence inverted y/row. </summary>
        public static int IndexPngToTexture ( int row , int col , int numRows , int numCols ) => numCols * ( numRows - 1 - row ) + col;

        public static int Index2dto1d ( int x , int y , int width ) => y * width + x;
        
        public static int GetBitDepth ( TextureFormat format )
        {
            switch ( format )
            {
                case TextureFormat.DXT1: return 8;//4;//indexed color not implemented, fallback to 8
                case TextureFormat.DXT5: return 8;
                case TextureFormat.Alpha8: return 8;
                case TextureFormat.R8: return 8;
                case TextureFormat.R16: return 16;
                case TextureFormat.RHalf: return 16;
                case TextureFormat.RFloat: return 32;
                case TextureFormat.RGB24: return 8;
                case TextureFormat.RGBA32: return 8;
                case TextureFormat.RGBAHalf: return 16;
                case TextureFormat.RGBAFloat: return 32;
                default: throw new System.NotImplementedException( $"format '{ format }' not implemented" );
            }
        }

        public static bool GetIsAlpha ( TextureFormat format )
        {
            switch ( format )
            {
                case TextureFormat.DXT5: return true;
                case TextureFormat.Alpha8: return true;
                case TextureFormat.ARGB4444: return true;
                case TextureFormat.ARGB32: return true;
                case TextureFormat.RGBA32: return true;
                case TextureFormat.RGBAHalf: return true;
                case TextureFormat.RGBAFloat: return true;
                default: return false;
            }
        }

        public static bool GetIsGreyscale ( TextureFormat format )
        {
            switch ( format )
            {
                case TextureFormat.Alpha8: return true;
                case TextureFormat.R8: return true;
                case TextureFormat.R16: return true;
                case TextureFormat.RHalf: return true;
                case TextureFormat.RFloat: return true;
                default: return false;
            }
        }

        /// <summary> Guess-timate most appropriate TextureFormat for given specification. </summary>
        public static TextureFormat GetTextureFormat ( int bitDepth , int channels )
        {
            switch ( bitDepth*10 + channels )
            {
                case 11: return TextureFormat.Alpha8;
                case 41: return TextureFormat.Alpha8;
                //case 43: return TextureFormat.DXT1;//indexed colors not implemented yet
                case 44: return TextureFormat.RGBA4444;
                case 82: return TextureFormat.Alpha8;
                //case 84: return TextureFormat.DXT5;//no way to infer between DXT5 and RGBA32, prefer one for runtime use
                case 84: return TextureFormat.RGBA32;
                case 83: return TextureFormat.RGB24;
                //case 81: return TextureFormat.Alpha8;//no way to infer between Alpha8 and R8, BUT Alpha8 was causing a problem, because grayscale 8bit channel seems to be saved as R in png (ie: reported as non-alpha image on read)
                case 81: return TextureFormat.Alpha8;
                case 161: return TextureFormat.R16;
                //case 161: return TextureFormat.RHalf;//no way to infer between R16 and RHalf
                case 163: return TextureFormat.RGB565;
                case 164: return TextureFormat.RGBAHalf;
                case 321: return TextureFormat.RFloat;
                default: throw new System.NotImplementedException( $"bit depth '{ bitDepth }' for '{ channels }' channels not implemented" );
            }
        }

        public static int ToInt ( float color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return (int)( color * max );
        }

        public static RGB ToRGB ( Color color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return new RGB {
                r = (int)( color.r * max ) ,
                g = (int)( color.g * max ) ,
                b = (int)( color.b * max )
            };
        }
        
        public static RGBA ToRGBA ( Color color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return new RGBA {
                R = (int)( color.r * max ) ,
                G = (int)( color.g * max ) ,
                B = (int)( color.b * max ) ,
                A = (int)( color.a * max )
            };
        }

        public static uint GetBitDepthMaxValue ( int bitDepth )
        {
            switch ( bitDepth )
            {
                case 1: return 1;
                case 2: return 3;
                case 4: return 15;
                case 8: return byte.MaxValue;
                case 16: return ushort.MaxValue;
                case 32: return uint.MaxValue;
                default: throw new System.Exception( $"bitDepth '{ bitDepth }' not implemented" );
            }
        }

        public struct RGB { public int r, g, b; }

        public struct RGBA { public int R, G, B, A; }

        public struct ReadColorsResult
        {
            public Color[] pixels;
            public int width, height;
            public TextureFormat textureFormatInfered;
        }

    }

}
