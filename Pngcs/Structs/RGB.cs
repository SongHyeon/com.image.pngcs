namespace Pngcs
{
	public struct RGB <T>
		where T : unmanaged
	{
		public T R, G, B;
		public RGBA<T> RGBA ( T A ) => new RGBA<T>{ R=R , G=G , B=B , A=A };
	}
}
