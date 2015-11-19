using UnityEngine;
using System.Collections;

public class PoiClass{

	public float lon;
	public float lat;
	public string name;
	public int type;
	public int ishot;
	public int level;
	public int isSelected;
	public int textPosition;//1-right,2-left,3-top,4-bottom
	public Vector2 screenPosition;
	public float labelLength;

	public PoiClass(float lon,float lat,string name,int type,int ishot,int level,int isSelected,int textPosition,float labelLength){
		this.lon = lon;
		this.lat = lat;
		this.name = name;
		this.type = type;
		this.ishot = ishot;
		this.level = level;
		this.isSelected = isSelected;
		this.textPosition = textPosition;
		this.labelLength = labelLength;
	}
	//
	public bool hasConflict(PoiClass poi1,int selectedType){
		float minx1 = 0.0f, maxx1 = 0.0f, miny1 = 0.0f, maxy1 = 0.0f;
		float minx = 0.0f, maxx = 0.0f, miny = 0.0f, maxy = 0.0f;
		//接下来得到这几个值
		if (poi1.type == selectedType || poi1.isSelected == 1) {//poi是需要大图标显示的poi
			minx1 = poi1.screenPosition.x - poi1.labelLength/2;
			maxx1 = poi1.screenPosition.x + poi1.labelLength/2;
			miny1 = poi1.screenPosition.y + 38 - 10;
			maxy1 = poi1.screenPosition.y + 38 + MapScan.label_high - 10;
		} else if (poi1.type == 0 || poi1.type == 1 || poi1.type == 2) {//有label的图标
			switch(poi1.textPosition){
			case 1://right
				minx1 = poi1.screenPosition.x + 22;
				maxx1 = poi1.screenPosition.x + 22 + poi1.labelLength;
				miny1 = poi1.screenPosition.y - MapScan.label_high/2;
				maxy1 = poi1.screenPosition.y + MapScan.label_high/2;
				break;
			case 2://left
				minx1 = poi1.screenPosition.x - 22 - poi1.labelLength;
				maxx1 = poi1.screenPosition.x;
				miny1 = poi1.screenPosition.y - MapScan.label_high/2;
				maxy1 = poi1.screenPosition.y + MapScan.label_high/2;
				break;
			case 3://top
				minx1 = poi1.screenPosition.x - poi1.labelLength/2;
				maxx1 = poi1.screenPosition.x + poi1.labelLength/2;
				miny1 = poi1.screenPosition.y - 22 - MapScan.label_high +10;
				maxy1 = poi1.screenPosition.y - 22 + 10;
				break;
			case 4://bottom
				minx1 = poi1.screenPosition.x - poi1.labelLength/2;
				maxx1 = poi1.screenPosition.x + poi1.labelLength/2;
				miny1 = poi1.screenPosition.y + 22 - 10;
				maxy1 = poi1.screenPosition.y + 22 + MapScan.label_high -10;
				break;
			default:
				break;
			}
		} else {//没有label的图标
			return false;
		}

		if (type == selectedType || isSelected == 1) {//poi是需要大图标显示的poi
			minx = screenPosition.x - labelLength/2;
			maxx = screenPosition.x + labelLength/2;
			miny = screenPosition.y + 38 - 10;
			maxy = screenPosition.y + 38 + MapScan.label_high - 10;
		} else if (type == 0 || type == 1 || type == 2) {//有label的图标
			switch(textPosition){
			case 1://right
				minx = screenPosition.x + 22;
				maxx = screenPosition.x + 22 + labelLength;
				miny = screenPosition.y - MapScan.label_high/2;
				maxy = screenPosition.y + MapScan.label_high/2;
				break;
			case 2://left
				minx = screenPosition.x - 22 - labelLength;
				maxx = screenPosition.x;
				miny = screenPosition.y - MapScan.label_high/2;
				maxy = screenPosition.y + MapScan.label_high/2;
				break;
			case 3://top
				minx = screenPosition.x - labelLength/2;
				maxx = screenPosition.x + labelLength/2;
				miny = screenPosition.y - 22 - MapScan.label_high +10;
				maxy = screenPosition.y - 22 + 10;
				break;
			case 4://bottom
				minx = screenPosition.x - labelLength/2;
				maxx = screenPosition.x + labelLength/2;
				miny = screenPosition.y + 22 - 10;
				maxy = screenPosition.y + 22 + MapScan.label_high -10;
				break;
			default:
				break;
			}
		} else {//没有label的图标
			return false;
		}
//		print ("" + minx + " " + maxx + "  " + miny + " " + maxy);
//		print ("" + minx1 + " " + maxx1 + "  " + miny1 + " " + maxy1);

			bool xConflict = (minx <= minx1 && minx1 <= maxx) || (minx <= maxx1 && maxx1 <= maxx)||(minx1 <= minx && minx <=maxx1)||(minx1 <= maxx && maxx <= maxx1);
			bool yConflict = (miny1 <= miny && miny <= maxy1) || (miny1 <= maxy&& maxy <= maxy1)||(miny <= miny1&&miny1 <= maxy)||(miny<=maxy1&&maxy1<=maxy);
		if (xConflict && yConflict) {
//			print("conflict");
			return true;
		}
			
		return false;
	}

}
