namespace Pngcs
{
    /// <summary>
    /// Utility functions for C# porting
    /// </summary>
    internal class PngCsUtils
    {

        internal unsafe static bool UnSafeEquals ( byte[] strA , byte[] strB )
        {
            int length = strA.Length;
            if( length!=strB.Length ) return false;
            fixed( byte* str = strA )
            {
                byte* chPtr = str;
                fixed( byte* str2 = strB )
                {
                    byte* chPtr2 = str2;
                    byte* chPtr3 = chPtr;
                    byte* chPtr4 = chPtr2;
                    while( length>=10 )
                    {
                        if( (((*(((int*)chPtr3))!=*(((int*)chPtr4))) || (*(((int*)(chPtr3+2)))!=*(((int*)(chPtr4+2))))) || ((*(((int*)(chPtr3+4)))!=*(((int*)(chPtr4+4)))) || (*(((int*)(chPtr3+6)))!=*(((int*)(chPtr4+6)))))) || (*(((int*)(chPtr3+8)))!=*(((int*)(chPtr4+8)))) )
                        {
                            break;
                        }
                        chPtr3 += 10;
                        chPtr4 += 10;
                        length -= 10;
                    }
                    while( length>0 )
                    {
                        if( *(((int*)chPtr3))!=*(((int*)chPtr4)) )
                        {
                            break;
                        }
                        chPtr3 += 2;
                        chPtr4 += 2;
                        length -= 2;
                    }
                    return length<=0;
                }
            }
        }

    }
}
