namespace Pngcs
{
	public struct RGBA <T>
		where T : unmanaged
	{
		public T R, G, B, A;
		public RGB<T> RGB => new RGB<T>{ R=R , G=G , B=B };
	}
}
