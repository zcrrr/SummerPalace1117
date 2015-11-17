using System.Collections;
using UnityEngine;
public class GoogleProjection{
	public const float MinLongitude = -180f;
	public const float MaxLongitude = 180f;
	public const float MinLatitude = -85.05112878f;
	public const float MaxLatitude = 85.05112878f;
	public const long offsetX = 27614592;
	public const long offsetY = 12704000;

	private float getRange(float n, float min, float max) {
		return Mathf.Min(Mathf.Max(n, min), max);
	}
	public PixelPoint lonlatToPixel(LonLatPoint llp, int level) {
		LonLatPoint mcP = lonlatToMercator(llp, level);
		return new PixelPoint((long) mcP.lon - offsetX, (long) mcP.lat - offsetY);
	}
	public LonLatPoint lonlatToMercator(LonLatPoint llP, int level) {
		long size = mapSize(level);
		
		float tlon = getRange(llP.lon, MinLongitude, MaxLongitude);
		float x = (tlon + 180) / 360;
		x = getRange(x * size + 0.5f, 0f, size - 1);
		
		float tlat = getRange(llP.lat, MinLatitude, MaxLatitude);
		float sinLatitude = Mathf.Sin(tlat * Mathf.PI / 180);
		float y = 0.5f - Mathf.Log((1 + sinLatitude) / (1 - sinLatitude))
			/ (4 * Mathf.PI);
		y = getRange(y * size + 0.5f, 0f, size - 1);
		
		return new LonLatPoint((long) x, (long) y);
	}
	public long mapSize(int levelOfDetail) {
		// return 256 << levelOfDetail;
		return (long) (256 * Mathf.Pow(2, levelOfDetail));
	}

}
