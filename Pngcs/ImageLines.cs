namespace Pngcs
{
    /// <summary>
    /// Wraps a set of rows from a image, read in a single operation, stored in a int[][] or byte[][] matrix
    /// They can be a subset of the total rows, but in this case they are equispaced.
    /// </summary>
    /// <note> See also ImageLine </note>
    public class ImageLines
    {

        public ImageInfo ImgInfo { get; private set; }
        public ImageLine.ESampleType sampleType { get; private set; }
        public bool SamplesUnpacked { get; private set; }
        public int RowOffset { get; private set; }
        public int Nrows { get; private set; }
        public int RowStep { get; private set; }
        internal readonly int channels;
        internal readonly int bitDepth;
        internal readonly int elementsPerRow;
        public int[][] Scanlines { get; private set; }
        public byte[][] ScanlinesB { get; private set; }

        public ImageLines( ImageInfo ImgInfo ,  ImageLine.ESampleType sampleType , bool unpackedMode , int rowOffset , int nRows , int rowStep )
        {
            this.ImgInfo = ImgInfo;
            channels = ImgInfo.Channels;
            bitDepth = ImgInfo.BitDepth;
            this.sampleType = sampleType;
            this.SamplesUnpacked = unpackedMode || !ImgInfo.Packed;
            this.RowOffset = rowOffset;
            this.Nrows = nRows;
            this.RowStep = rowStep;
            elementsPerRow = unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked;
            if( sampleType==ImageLine.ESampleType.INT )
            {
                Scanlines = new int[nRows][];
                for( int i=0 ; i<nRows ; i++ ) Scanlines[i] = new int[elementsPerRow];
                ScanlinesB = null;
            }
            else if( sampleType==ImageLine.ESampleType.BYTE )
            {
                ScanlinesB = new byte[nRows][];
                for( int i=0 ; i<nRows ; i++ ) ScanlinesB[i] = new byte[elementsPerRow];
                Scanlines = null;
            }
            else throw new System.Exception("bad ImageLine initialization");
        }

        /// <summary>
        /// Translates from image row number to matrix row.
        /// If you are not sure if this image row in included, use better ImageRowToMatrixRowStrict
        /// </summary>
        /// <param name="imageRow">Row number in the original image (from 0) </param>
        /// <returns>Row number in the wrapped matrix. Undefined result if invalid</returns>
        public int ImageRowToMatrixRow ( int imrow )
        {
            int r = (imrow - RowOffset) / RowStep;
            return r<0 ? 0 : ( r<Nrows ? r : Nrows-1 );
        }

        /// <summary> Translates from image row number to matrix row </summary>
        /// <param name="imageRow">Row number in the original image (from 0) </param>
        /// <returns>Row number in the wrapped matrix. Returns -1 if invalid</returns>
        public int ImageRowToMatrixRowStrict ( int imrow )
        {
            imrow -= RowOffset;
            int mrow = imrow>=0 && imrow%RowStep==0 ? imrow/RowStep : -1;
            return mrow<Nrows ? mrow : -1;
        }

        /// <summary> Translates from matrix row number to real image row number </summary>
        public int MatrixRowToImageRow ( int mrow ) => mrow*RowStep + RowOffset;
        /// <param name="matrixRow"> Row number inside the matrix </param>

        /// <summary>
        /// Constructs and returns an ImageLine object backed by a matrix row.
        /// This is quite efficient, no deep copy.
        /// </summary>
        public ImageLine GetImageLineAtMatrixRow ( int mrow )
        /// <param name="matrixRow"> Row number inside the matrix </param>
        {
            if( mrow<0 || mrow>Nrows ) throw new System.Exception($"Bad row {mrow}. Should be positive and less than {Nrows}");
            ImageLine imline =
                    sampleType==ImageLine.ESampleType.INT
                    ? new ImageLine( ImgInfo , sampleType , SamplesUnpacked , Scanlines[mrow] , null )
                    : new ImageLine( ImgInfo , sampleType , SamplesUnpacked , null , ScanlinesB[mrow] );
            imline.Rown = MatrixRowToImageRow( mrow );
            return imline;
        }

    }
}
