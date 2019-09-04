namespace Pngcs.Chunks
{
    /// <summary>
    /// PLTE Palette chunk: this is the only optional critical chunk
    /// http://www.w3.org/TR/PNG/#11PLTE
    /// </summary>
    public class PngChunkPLTE : PngChunkSingle
    {

        public const string ID = ChunkHelper.PLTE;

        int nentries = 0;
        
        /// <summary></summary>
        int[] entries;

        public PngChunkPLTE ( ImageInfo info )
            : base( ID , info )
        {
            this.nentries = 0;
        }


        public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NA;

        public override ChunkRaw CreateRawChunk ()
        {
            int len = 3 * nentries;
            int[] rgb = new int[3];
            ChunkRaw c = createEmptyChunk( len , true );
            byte[] data = c.Data;
            for( int n=0 , i=0 ; n<nentries ; n++ )
            {
                GetEntryRgb( n , rgb );
                data[i++] = (byte)rgb[0];
                data[i++] = (byte)rgb[1];
                data[i++] = (byte)rgb[2];
            }
            return c;
        }

        public override void ParseFromRaw ( ChunkRaw chunk )
        {
            SetNentries( chunk.Len/3 );
            byte[] data = chunk.Data;
            for( int n=0 , i=0 ; n<nentries ; n++ )
            {
                SetEntry(
                    n ,
                    (int)( data[i++] & 0xff ) ,
                    (int)( data[i++] & 0xff ) ,
                    (int)( data[i++] & 0xff )
                );
            }
        }

        public override void CloneDataFromRead ( PngChunk other )
        {
            PngChunkPLTE otherx = (PngChunkPLTE)other;
            this.SetNentries( otherx.GetNentries() );
            System.Array.Copy( otherx.entries , 0 , entries , 0 , nentries );
        }

        /// <summary> Also allocates array </summary>
        /// <param name="nentries">1-256</param>
        public void SetNentries ( int nentries )
        {
            this.nentries = nentries;
            if( nentries<1 || nentries>256 ) throw new System.Exception($"invalid pallette - nentries={nentries}");
            if( entries==null || entries.Length!=nentries )// alloc
                entries = new int[nentries];
        }

        public int GetNentries () => nentries;

        public void SetEntry ( int n , int r , int g , int b ) => entries[n] = ((r<<16) | (g<<8) | b);

        /// <summary> as packed RGB8 </summary>
        public int GetEntry ( int n ) => entries[n];

       
        /// <summary> Gets n'th entry, filling 3 positions of given array, at given offset </summary>
        public void GetEntryRgb ( int index , int[] rgb , int offset )
        {
            int v = entries[index];
            rgb[offset] = ((v & 0xff0000)>>16);
            rgb[offset+1] = ((v & 0xff00)>>8);
            rgb[offset+2] = (v & 0xff);
        }

        /// <summary> shortcut: GetEntryRgb(index, int[] rgb, 0) </summary>
        public void GetEntryRgb ( int n , int[] rgb ) => GetEntryRgb( n , rgb , 0 );

        /// <summary> minimum allowed bit depth, given palette size </summary>
        /// <returns>1-2-4-8</returns>
        public int MinBitDepth ()
        {
            if( nentries<=2 ) return 1;
            else if( nentries<=4 ) return 2;
            else if( nentries<=16 ) return 4;
            else return 8;
        }
    }

}
