#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using IO = System.IO;
using Exception = System.Exception;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Pngcs.Unity;
using Pngcs;
using Pngcs.Chunks;

namespace PNGCS.Test
{
    public class Test_PNGCS_Write : EditorWindow
    {

        List<Texture2D> _textures = new List<Texture2D>(100);

        public void OnEnable ()
        {
            string outputDir = IO.Path.Combine( Application.streamingAssetsPath , "Write Tests" );
            {
                string fileName = nameof(Tests.CreateGradientPng);
                string filePath = IO.Path.Combine( outputDir , $"{fileName}.png" );
                CreateUiGroup( $"Test {fileName}" , ()=> Tests.CreateGradientPng( filePath , 1024 , 1024 ) , filePath );
            }
            // {
            //     string fileName = nameof(Tests.CreateGradientPng48bit);
            //     string filePath = IO.Path.Combine( outputDir , $"{fileName}.png" );
            //     CreateUiGroup( $"Test {fileName}" , ()=> Tests.CreateGradientPng48bit( filePath , 1024 , 1024 ) , filePath );
            // }
            // {
            //     string fileName = nameof(Tests.CreateTexture2dGradient);
            //     string filePath = IO.Path.Combine( outputDir , $"{fileName}.png" );
            //     CreateUiGroup( $"Test {fileName}" , ()=> Tests.CreateTexture2dGradient( filePath , 1024 , 1024 ) , filePath );
            // }
        }

        void OnDisable ()
        {
            foreach( var tex in _textures ) DestroyImmediate( tex );
        }

        [MenuItem("Test/PNGCS/Write")]
        public static void ShowWindow ()
        {
            var window = GetWindow<Test_PNGCS_Write>();
            window.titleContent = new GUIContent( window.GetType().Name );
            window.minSize = new Vector2{ x=300 , y=100 };
        }

        void CreateUiGroup ( string title , System.Action action , string filePath )
        {
            var GROUP1 = new VisualElement();
            {
                var group1Style = GROUP1.style;
                group1Style.borderColor = Color.black;
                group1Style.backgroundColor = Color.gray;
                group1Style.borderColor = Color.black;
                group1Style.marginBottom = 8;
                group1Style.marginLeft = 4;
                group1Style.marginRight = 4;
                group1Style.marginTop = 8;
            }
            {
                string outputDir = IO.Path.GetDirectoryName(filePath);
                var BUTTON = new Button( ()=> {
                    if( !IO.Directory.Exists(outputDir) ) IO.Directory.CreateDirectory(outputDir);
                    Tests.ExecWithTimer( action , false );
                    Test_PNGCS_Read.ShowWindow( outputDir );
                } );
                BUTTON.text = title;
                GROUP1.Add( BUTTON );

                var LABEL = new Label( $"path: {filePath}" );
                GROUP1.Add( LABEL );
            }
            rootVisualElement.Add( GROUP1 );
        }

    }

    static class Tests
    {

        public static async void ExecWithTimer ( System.Action action , bool differentThread = true )
        {
            var timeStart = System.DateTime.Now;
            if( differentThread==true )
            {
                await Task.Run( ()=> { try {
                    action();
                    return Task.CompletedTask;
                } catch( Exception ex ) { Debug.LogException(ex); return Task.CompletedTask; } } );
            }
            else
            {
                action();
            }
            Debug.Log($"Job done, took {(System.DateTime.Now-timeStart).TotalSeconds:0.00} seconds");
        }

        // DecreaseRedColor ( string source , string output = "D:/decreased red color.png" )
        public static void DecreaseRedColor ( string source , string output )
        {
            if( source.Equals(output) ) throw new Exception("input and output file cannot coincide");
            var reader = FileHelper.CreatePngReader( source );
            var info = reader.ImgInfo;
            var writer = FileHelper.CreatePngWriter( output , info , true );
            int chunkBehav = ChunkCopyBehaviour.COPY_ALL_SAFE;// copy all 'safe' chunks
            Debug.Log( reader );

            // this can copy some metadata from reader
            writer.CopyChunksFirst( reader , chunkBehav );
            int channels = info.Channels;
            if( channels<3 ) throw new Exception("This method is for RGB/RGBA images");
            int numRows = info.Rows;
            int numCols = info.Cols;
            for( int row=0 ; row<numRows ; row++ )
            {
                ImageLine line = reader.ReadRow( row );
                int[] scanline = line.Scanline;
                for( int col=0 ; col<numCols ; col++ )
                {
                    scanline[ col * channels ] /= 2;
                }
                writer.WriteRow( line , row );
            }

            // just in case some new metadata has been read after the image
            writer.CopyChunksLast( reader , chunkBehav );
            writer.End();
        }

        // ConvertIndexedPngToTrueColor ( string source , string output = "D:/TrueColor.png" )
        public static void ConvertIndexedPngToTrueColor ( string source , string output )
        {
            PngReader reader = FileHelper.CreatePngReader( source );
            var info = reader.ImgInfo;
            if( info.Indexed==false ) throw new Exception("Not indexed image");
            PngChunkPLTE plte = reader.GetMetadata().GetPLTE();
            PngChunkTRNS trns = reader.GetMetadata().GetTRNS();//transparency metadata, can be null
            bool alpha = trns!=null;
            int numRows = info.Rows;
            int numCols = info.Cols;
            ImageInfo im2 = new ImageInfo( numCols , numRows , 8 , alpha );
            PngWriter writer = FileHelper.CreatePngWriter( output , im2 , true );
            writer.CopyChunksFirst( reader , ChunkCopyBehaviour.COPY_ALL_SAFE );
            int[] buffer = null;
            for( int row=0 ; row<numRows ; row++ )
            {
                ImageLine line = reader.ReadRowInt( row );
                buffer = ImageLineHelper.Palette2rgb( line , plte , trns , buffer );
                writer.WriteRowInt( buffer , row );
            }
            writer.CopyChunksLast( reader , ChunkCopyBehaviour.COPY_ALL_SAFE );
            reader.Dispose();
            writer.End();
            Debug.Log($"True color: {output}");
        }

        // CreateGradientPng ( string output = "D:/orange.png" , int cols = 1024 , int rows = 1024 )
        public static void CreateGradientPng ( string output , int cols , int rows )
        {
            // open image for writing:
            ImageInfo info = new ImageInfo( cols , rows , 8 , false );// 8 bits per channel, no alpha
            PngWriter writer = FileHelper.CreatePngWriter( output , info , true );
            try
            {
                // add some optional metadata (chunks)
                var meta = writer.GetMetadata();
                meta.SetDpi( 100.0 );
                meta.SetTimeNow( 0 );// 0 seconds fron now = now
                meta.SetText( PngChunkTextVar.KEY_Title , "Just a text image" );
                PngChunk chunk = meta.SetText( "my key" , "my text .. bla bla" );
                chunk.Priority = true;// this chunk will be written as soon as possible

                //set pixels:
                int numCols = info.Cols;
                int channels = info.Channels;
                int[] rawColors = new int[ info.SamplesPerRow ];
                for( int col=0 ; col<numCols ; col++ )
                {
                    // this line will be written to all rows
                    RGB<int> rgb = new RGB<int>{
                        R = 255 ,
                        G = 127 ,
                        B = 255 * col / numCols
                    };
                    ImageLineHelper.SetPixel( rawColors , rgb , col , channels );
                }
                var line = new ImageLine( info , ImageLine.ESampleType.INT , false , rawColors , null , 0 );

                //write:
                int numRows = writer.ImgInfo.Rows;
                for( int row=0 ; row<numRows ; row++ )
                    writer.WriteRow( line , row );
            }
            catch( Exception ex ) { Debug.LogException(ex); }
            finally
            {
                writer.End();
            }
        }

        // static void Button_CreateGradientPng48bit ( string output = "D:/gradient 48bit.png" , int cols = 1024 , int rows = 1024 ) => ExecWithTimer( ()=> CreateGradientPng48bit( output , cols, rows ) );
        public static void CreateGradientPng48bit ( string output , int cols , int rows )
        {
            // open image for writing:
            ImageInfo imageInfo = new ImageInfo( cols , rows , 16 , false );
            PngWriter writer = FileHelper.CreatePngWriter( output , imageInfo , true );
            
            // add some optional metadata (chunks)
            var meta = writer.GetMetadata();
            meta.SetDpi( 100.0 );
            meta.SetTimeNow( 0 );// 0 seconds fron now = now
            meta.SetText( PngChunkTextVar.KEY_Title , "Just a text image" );
            PngChunk chunk = meta.SetText( "my key" , "my text .. bla bla" );
            chunk.Priority = true;// this chunk will be written as soon as possible
        
            //set pixels:
            int numCols = imageInfo.Cols;
            int numRows = writer.ImgInfo.Rows;
            int channels = imageInfo.Channels;
            for( int row=0 ; row<numRows ; row++ )
            {
                //fill row:
                int[] integers = new int[ numCols ];
                for( int col=0 ; col<numCols ; col++ )
                {
                    RGB<int> rgb = new RGB<int>{
                        R = (65535/numCols)*col ,//65535/2;
                        G = (65535/numRows)*row ,//65535;
                        B = 0 ,//65535 * col / numCols;
                    };
                    ImageLineHelper.SetPixel( integers , rgb , col , channels );
                }

                //write row:
                ImageLine imageLine = new ImageLine( imageInfo , ImageLine.ESampleType.INT , false , integers , null , row );
                writer.WriteRow( imageLine , row );
            }
            
            writer.End();
        }

        public static void CreateTexture2dGradient ( string output , int cols , int rows )
        {
            var texture = new Texture2D( cols , rows , TextureFormat.RGBAHalf , false , true );
            Color[] pixels = new Color[ cols * rows ];
            for( int row=0 ; row<rows ; row++ )
            for( int col=0 ; col<cols ; col++ )
            {
                pixels[ row * cols + col ] = new Color{
                    r = (float)col / (float)cols ,
                    g = (float)row / (float)rows ,
                    b = 0f ,
                    a = 1f
                };
            }
            texture.SetPixels( pixels );
            texture.Apply();
            PNG.Write( texture , output );
            Object.Destroy( texture );
        }

        public static void MirrorPngFile ( string source , string destination  )
        {
            if( source.Equals(destination) ) throw new Exception("input and output file cannot coincide");
            PngReader reader = FileHelper.CreatePngReader( source );
            var info = reader.ImgInfo;
            PngWriter writer = FileHelper.CreatePngWriter( destination , info , true );
            reader.SetUnpackedMode( true );// we dont want to do the unpacking ourselves, we want a sample per array element
            writer.SetUseUnPackedMode( true );// not really necesary here, as we pass the ImageLine, but anyway...
            writer.CopyChunksFirst( reader , ChunkCopyBehaviour.COPY_ALL_SAFE );
            int numRows = info.Rows;
            for( int row=0 ; row<numRows ; row++ )
            {
                ImageLine l1 = reader.ReadRowInt( row );
                mirrorLineInt( info , l1.Scanline );
                writer.WriteRow( l1 , row );
            }
            writer.CopyChunksLast( reader , ChunkCopyBehaviour.COPY_ALL_SAFE );
            writer.End();

            //local method:
            void mirrorLineInt ( ImageInfo imgInfo , int[] line )// unpacked line
            {
                int channels = imgInfo.Channels;
                int numCols = imgInfo.Cols;
                for( int c1=0 , c2=numCols - 1 ; c1 < c2 ; c1++ , c2-- )// swap pixels (not samples!)
                {
                    for( int i=0 ; i<channels ; i++ )
                    {
                        int aux = line[ c1 * channels + i ];
                        line[ c1 * channels + i ] = line[ c2 * channels + i ];
                        line[ c2 * channels + i ] = aux;
                    }
                }
            }
        }
        
        public static void PrintPngFileChunks ( string source )
        {
            PngReader reader = FileHelper.CreatePngReader( source );
            reader.MaxTotalBytesRead = 1024 * 1024 * 1024L * 3;// 3Gb!
            reader.ReadSkippingAllRows();
        }

        public static async void WritePngUsingPNGWriteAsync ( Texture2D texture , string path )
        {
            await PNG.WriteAsync( texture , path );
        }

        // PngToCsv ( string source = "D:/input.png" , string output = "D:/output.csv" )
        public static void PngToCsv ( string source , string output )
        {
            PngReader reader = FileHelper.CreatePngReader( source );
            var info = reader.ImgInfo;
            int numRows = info.Rows;
            int numCols = info.Cols;
            int channels = info.Channels;
            int bitDepth = info.BitDepth;//bits per sample (channel) in the buffer
            int bytesPixel = info.BytesPixel;
            var text = new System.Text.StringBuilder();
            int min = int.MaxValue;
            int max = int.MinValue;
            for( int row=0 ; row<numRows ; row++ )
            {
                ImageLine imageLine = reader.ReadRowInt( row );
                var scanline = imageLine.Scanline;
                for( int col=0 ; col<numCols ; col++ )
                {
                    for( int ch=0 ; ch<channels ; ch++ )
                    {
                        int val = scanline[ col * channels + ch ];
                        
                        if( val<min ) { min = val; }
                        if( val>max ) { max = val; }

                        if( ch!=0 ) { text.Append( ' ' ); }
                        text.Append( val );
                    }
                    text.Append( '\n' );
                }
            }
            Debug.Log($"min:{min}\nmax:{max}");
            IO.File.WriteAllText( output , text.ToString() );
        }

    }

}
#endif
