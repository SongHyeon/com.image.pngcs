namespace Pngcs.Chunks
{

    /// <summary>
    /// tRNS chunk: http://www.w3.org/TR/PNG/#11tRNS
    /// </summary>
    public class PngChunkTRNS : PngChunkSingle
    {

        public const string ID = ChunkHelper.tRNS;
    
        // this chunk structure depends on the image type
        // only one of these is meaningful
        int gray;
        int red, green, blue;
        int[] paletteAlpha;

        public PngChunkTRNS ( ImageInfo info )
            : base( ID , info )
        {

        }

        public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk ()
        {
            ChunkRaw chunk = null;
            if( ImgInfo.Greyscale )
            {
                chunk = createEmptyChunk( 2 , true );
                byte[] data = chunk.Data;

                PngHelperInternal.WriteInt2tobytes( gray , data , 0 );
            }
            else if( ImgInfo.Indexed )
            {
                chunk = createEmptyChunk( paletteAlpha.Length , true );
                byte[] data = chunk.Data;
                int length = data.Length;

                for( int n=0 ; n<length ; n++ )
                    data[n] = (byte)paletteAlpha[n];
            }
            else
            {
                chunk = createEmptyChunk( 6 , true );
                byte[] data = chunk.Data;

                PngHelperInternal.WriteInt2tobytes( red , data , 0 );
                PngHelperInternal.WriteInt2tobytes( green , data , 0 );
                PngHelperInternal.WriteInt2tobytes( blue , data , 0 );
            }
            return chunk;
        }

        public override void ParseFromRaw ( ChunkRaw chunk )
        {
            byte[] data = chunk.Data;
            if( ImgInfo.Greyscale )
            {
                gray = PngHelperInternal.ReadInt2fromBytes( data , 0 );
            }
            else if( ImgInfo.Indexed )
            {
                int nentries = data.Length;
                paletteAlpha = new int[nentries];
                for( int n=0 ; n<nentries ; n++ )
                {
                    paletteAlpha[n] = (int)( data[n] & 0xff );
                }
            }
            else
            {
                red = PngHelperInternal.ReadInt2fromBytes( data , 0 );
                green = PngHelperInternal.ReadInt2fromBytes( data , 2 );
                blue = PngHelperInternal.ReadInt2fromBytes( data , 4 );
            }
        }

        public override void CloneDataFromRead ( PngChunk other )
        {
            PngChunkTRNS otherx = (PngChunkTRNS)other;
            gray = otherx.gray;
            red = otherx.red;
            green = otherx.green;
            blue = otherx.blue;
            if( otherx.paletteAlpha!=null )
            {
                paletteAlpha = new int[ otherx.paletteAlpha.Length ];
                System.Array.Copy( otherx.paletteAlpha , 0 , paletteAlpha , 0 , paletteAlpha.Length );
            }
        }

        public void SetRGB ( int r , int g , int b )
        {
            if( ImgInfo.Greyscale || ImgInfo.Indexed ) throw new System.Exception("only rgb or rgba images support this");
            red = r;
            green = g;
            blue = b;
        }

        public int[] GetRGB ()
        {
            if( ImgInfo.Greyscale || ImgInfo.Indexed ) throw new System.Exception("only rgb or rgba images support this");
            return new int[] { red, green, blue };
        }

        public void SetGray ( int g )
        {
            if( !ImgInfo.Greyscale ) throw new System.Exception("only grayscale images support this");
            gray = g;
        }

        public int GetGray ()
        {
            if( !ImgInfo.Greyscale ) throw new System.Exception("only grayscale images support this");
            return gray;
        }

        /// <summary> WARNING: non deep copy </summary>
        /// <param name="palAlpha"></param>
        public void SetPalletteAlpha ( int[] palAlpha )
        {
            if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images support this");
            paletteAlpha = palAlpha;
        }

        /// <summary> utiliy method : to use when only one pallete index is set as totally transparent </summary>
        /// <param name="palAlphaIndex"></param>
        public void setIndexEntryAsTransparent ( int palAlphaIndex )
        {
            if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images support this");
            paletteAlpha = new int[] { palAlphaIndex + 1 };
            for( int i=0 ; i<palAlphaIndex ; i++ )
                paletteAlpha[i] = 255;
            paletteAlpha[palAlphaIndex] = 0;
        }

        /// <summary> WARNING: non deep copy </summary>
        public int[] GetPalletteAlpha ()
        {
            if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images support this");
            return paletteAlpha;
        }
        
    }
}
