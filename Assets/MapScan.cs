using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;


public class MapScan : MonoBehaviour {
	//一些限定常量值
	private float minPinchDistance = 10.0f;
	private float angleRangeOfRotate = 100;
	private float minHigh = 0.0f;
	private float maxHigh = 0.0f;
	private float fitHigh = 0.0f;
	private float minAngle = 60f;
	private float maxAngle = 89f;

	private float pinchDistanceDelta = 0.0f;
	private bool isDistanceChangeHuge = false;
	private bool isRotateBack = false;
	private bool hasRotated = false;
	private bool hasUpDown = false;


	private bool isRotate = false;
	public string gps_info = "";
	GameObject ball;
	private CharacterController controller;

	private float testLon = 116.272430f;
	private float testLat = 39.991851f;
	Camera camera;
	GoogleProjection gp = new GoogleProjection ();

	string currentGesture = "";//begin,zoom,rotate,updown
	ArrayList gestureList = new ArrayList();  
	string platform = "android";

	string testCoorString0 = "";
	string testCoorString1 = "";

	Vector2 touchBefore;
	Vector2 touch0before;
	Vector2 touch1before;
	
	ArrayList list_display = new ArrayList();//当前正在展示的数据
	ArrayList texture_big = new ArrayList ();
	ArrayList texture_small = new ArrayList ();
	Vector3 targetPosition;
	bool startMove = false;
	bool wantWatch = false;
	bool startWatch = false;
	Vector3 aroundPosition;
	float step = 5;
	KeyAndValue kav;
	int selectedType = -1;
	PoiClass lastSelectedPoi;
	bool isDraging;
	string doorString = "116.281059,39.997910,颐和园东宫门;116.274063,40.002598,颐颐和园北宫门";
	Texture2D texture_door ;
	public static float label_high = 36f;


	//动态规避标注

	ArrayList listPoisAlreadyInScreen = new ArrayList();



	
	// Use this for initialization
	void Start () {
		kav = new KeyAndValue ();
		camera = GetComponent<Camera>();
		if (platform.Equals ("ios")) {
			minPinchDistance = 10.0f;
		} else {
			minPinchDistance = 10.0f;
		}

		minHigh = transform.position.y / 10;
		maxHigh = 40;
		fitHigh = 20;
		StartCoroutine(StartGPS());
		ball = GameObject.Find ("ball");
		controller = ball.GetComponent<CharacterController>();
		//test code:
		zywx_setPoiDateSource ("");
//		LonLatPoint lonlatpoint = new LonLatPoint(116.270928f,39.986163f);
//		PixelPoint point = gp.lonlatToPixel (lonlatpoint,17);
//		print("coor is "+(-(float)point.pointX/100f)+"  "+(float)point.pointY/100f);
		for (int i = 1; i<=24; i++) {
			Texture2D image_big = (Texture2D)Resources.Load ("map_"+i+"@3x");
			GUIContent content_big = new GUIContent ();
			content_big.image = image_big;
			texture_big.Add(content_big);

			Texture2D image_small = (Texture2D)Resources.Load ("map_icon_"+i);
			GUIContent content_small = new GUIContent ();
			content_small.image = image_small;
			texture_small.Add(content_small);
		}
		texture_door = (Texture2D)Resources.Load ("door");
	}



	// Update is called once per frame
	void Update () {
		//相机正在移动
		if (startMove) {
			transform.position = Vector3.MoveTowards(transform.position, targetPosition, step*Time.deltaTime);
			if(transform.position.Equals(targetPosition)){
				startMove = false;
				if(wantWatch){//说明移动过去是为了观看
					startWatch = true;
					wantWatch = false;
				}
			}
		}
		//相机正在旋转
		if (startWatch) {
			float angleBefore = transform.eulerAngles.y;
			transform.RotateAround (aroundPosition,new Vector3(0,1,0) , 30 * Time.deltaTime);
			float angleAfter = transform.eulerAngles.y;
			if(angleBefore < 180 && angleAfter > 180){
				startWatch = false;
			}

		}
		gps_info = "手机实际位置: " + Input.location.lastData.longitude + "," + Input.location.lastData.latitude;
		LonLatPoint lonlatpoint = new LonLatPoint(Input.location.lastData.longitude,Input.location.lastData.latitude);
		PixelPoint point = gp.lonlatToPixel (lonlatpoint,17);
		controller.SimpleMove (controller.transform.InverseTransformPoint((new Vector3 (-(float)point.pointX/100f,0.5f,(float)point.pointY/100f))));
//		print ("position is " + new Vector3 (-(float)point.pointX / 100f, 0.5f, (float)point.pointY / 100f));

		if (isRotateBack) {//
			Vector3 centerPoint = new Vector3(Screen.width/2,Screen.height/2,transform.position.y/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x)));
			float angleBefore = transform.eulerAngles.y;
			float rotateSpeed; 
			if(angleBefore > 180){
				rotateSpeed = -50.0f*Time.deltaTime;
			}else{
				rotateSpeed = 50.0f*Time.deltaTime;
			}
			transform.RotateAround (camera.ScreenToWorldPoint(centerPoint), new Vector3(0,1,0), rotateSpeed);
			float angleAfter = transform.eulerAngles.y;
			if(angleBefore <= 180 && angleAfter > 180){
				isRotateBack = false;
				hasRotated = false;
			}
		}
		if (Input.touchCount == 1) {
			if (Input.GetTouch (0).phase == TouchPhase.Began) {//屏幕点击事件
				touchBefore = Input.GetTouch (0).position;
				currentGesture = "";
				startMove = false;
				startWatch = false;
				wantWatch = false;
			}else if (Input.GetTouch (0).phase == TouchPhase.Moved) {
				isDraging = true;
				if(!currentGesture.Equals("")){
					return;
				}
				Vector2 touchAfter = Input.GetTouch (0).position;
				Vector2 touchDeltaPosition = touchAfter - touchBefore;
				float lengthScreen = touchDeltaPosition.magnitude;

				Vector3 touchAfterToWorld = camera.ScreenToWorldPoint(new Vector3(touchAfter.x,touchAfter.y,transform.position.y/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x))));
				Vector3 touchBeforeToWorld = camera.ScreenToWorldPoint(new Vector3(touchBefore.x,touchBefore.y,transform.position.y/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x))));
				Vector3 deltaWorld = touchAfterToWorld - touchBeforeToWorld;
				float lengthWorld = deltaWorld.magnitude;
				float scaleFromSceenToWorld = lengthWorld/lengthScreen;
				float y_weight = -touchDeltaPosition.y*Mathf.Sin(DegreetoRadians(transform.eulerAngles.x));
				float z_weight = -touchDeltaPosition.y*Mathf.Cos(DegreetoRadians(transform.eulerAngles.x));
				transform.Translate (-touchDeltaPosition.x * scaleFromSceenToWorld, y_weight * scaleFromSceenToWorld, z_weight*scaleFromSceenToWorld );
				touchBefore = touchAfter;
			}else if (Input.GetTouch (0).phase == TouchPhase.Ended) {//屏幕点击事件
				isDraging = false;
			}
		} else if (Input.touchCount == 2) {
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);
			if(touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began){
				touch0before = Input.GetTouch(0).position;
				touch1before = Input.GetTouch(1).position;
				currentGesture = "begin";
				gestureList.Clear();
				testCoorString0 = "";
				testCoorString1 = "";
			}else if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended) {
				isDraging = false;
			}
			else if (touchZero.phase == TouchPhase.Moved && touchOne.phase == TouchPhase.Moved) {
				isDraging = true;
				testCoorString0 += touchZero.position+"\n";
				testCoorString1 += touchOne.position+"\n";
				//判断之前和之后，两点间的距离的变化是否是巨大的
				float pinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
				float prevDistance = Vector2.Distance(touch0before,touch1before);
				pinchDistanceDelta = pinchDistance - prevDistance;
				if (Mathf.Abs(pinchDistanceDelta) > minPinchDistance) {
					isDistanceChangeHuge = true;
				}else{
					isDistanceChangeHuge = false;
				}
				Vector2 vectorbefore01 = new Vector2(touch1before.x-touch0before.x,touch1before.y-touch0before.y);
				float angleZero = VectorAngle(vectorbefore01, touchZero.position - touch0before);
				Vector2 vectorbefore10 = new Vector2(touch0before.x-touch1before.x,touch0before.y-touch1before.y);
				float angleOne = VectorAngle(vectorbefore10, touchOne.position - touch1before);
				if(angleZero * angleOne > 0 && Mathf.Abs(angleZero) > 90-angleRangeOfRotate/2 && Mathf.Abs(angleZero) < 90+angleRangeOfRotate/2 && Mathf.Abs(angleOne) > 90-angleRangeOfRotate/2 && Mathf.Abs(angleOne) < 90+angleRangeOfRotate/2){
					isRotate = true;
				}else{
					isRotate = false;
				}
				if(isRotate){
					if(currentGesture.Equals("begin")){
						gestureList.Add("rotate");
						if(isContinuousSameGesture("rotate")){//连续三个rotate
							currentGesture = "rotate";
						}else{
							touch0before = touchZero.position;
							touch1before = touchOne.position;
							return;
						}
					}else{
						if(!currentGesture.Equals("rotate")){//zoom or updown
							touch0before = touchZero.position;
							touch1before = touchOne.position;
							return;
						}
					}
					Vector2 vectorAfter01 = new Vector2(touchOne.position.x-touchZero.position.x,touchOne.position.y-touchZero.position.y);
					float rotateAngle = VectorAngle(vectorbefore01, vectorAfter01);
					Vector3 centerPoint = new Vector3((touch0before.x+touch1before.x)/2,(touch0before.y+touch1before.y)/2,transform.position.y/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x)));
					transform.RotateAround (camera.ScreenToWorldPoint(centerPoint),new Vector3(0,1,0) , -rotateAngle);
					hasRotated = true;
				}else{
					if(isDistanceChangeHuge){
						if(currentGesture.Equals("begin")){
							gestureList.Add("zoom");
							if(isContinuousSameGesture("zoom")){//连续三个zoom
								currentGesture = "zoom";
							}else{
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}else{
							if(!currentGesture.Equals("zoom")){//rotate or updown
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}
						float scaleOfView = pinchDistance/prevDistance;//视野放大了几倍
						Vector2 touchZeroPrevPos = touchZero.position - (touchZero.position - touch0before);
						Vector2 touchOnePrevPos = touchOne.position - (touchOne.position - touch1before);
						// Find the magnitude of the vector (the distance) between the touches in each frame.
						float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
						float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
						
						// Find the difference in the distances between each frame.
						float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
//						print ("height:"+transform.position.y);
						if(deltaMagnitudeDiff > 0){//zoom out
							if(transform.position.y > maxHigh){
								print("too high");
								hasUpDown = false;
								//call ios native function back
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								_unityCallIOS("back");
								return;
							} 
						}else{//zoom in
							if(transform.position.y < minHigh){
								print("too low");
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}

						float h1 = transform.position.y;
						float afterh = h1 * scaleOfView;
						float h3 = Mathf.Abs(afterh - h1);
						float forwardDis = h3/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x));
						if(deltaMagnitudeDiff > 0){//zoom out
							transform.Translate(-Vector3.forward*forwardDis);
						}else{
							transform.Translate(Vector3.forward*forwardDis);
						}


						if(transform.position.y > fitHigh*9/10){
							if(h1 <= fitHigh*9/10){//转回来
								startRotateBack();
								hasUpDown = false;
							}
						}else{
						}
						float h2 = transform.position.y;
						float angle1 = 90.0f-transform.eulerAngles.x;
						if(deltaMagnitudeDiff > 0){//zoom out
							if(transform.eulerAngles.x < getMaxAngleByHeight()){
								Vector3 cameraLeftWorldVector = transform.TransformDirection (Vector3.left);
								float anglecorret = getMaxAngleByHeight()-transform.eulerAngles.x;
								transform.RotateAround (transform.position, cameraLeftWorldVector,-anglecorret);
								float dis1 = Mathf.Tan(DegreetoRadians(angle1))*h2;
								float dis2 = Mathf.Tan(DegreetoRadians(angle1-anglecorret))*h2;
								float dis = dis1 - dis2;
								float y_weight = dis*Mathf.Sin(DegreetoRadians(transform.eulerAngles.x));
								float z_weight = dis*Mathf.Cos(DegreetoRadians(transform.eulerAngles.x));
								transform.Translate (0, y_weight, z_weight );
							}
						}else{//zoom in
							if(!hasUpDown){
								Vector3 cameraLeftWorldVector = transform.TransformDirection (Vector3.left);
								float anglecorret = transform.eulerAngles.x - getMaxAngleByHeight();
								transform.RotateAround (transform.position, cameraLeftWorldVector,anglecorret);
								float dis1 = Mathf.Tan(DegreetoRadians(angle1))*h2;
								float dis2 = Mathf.Tan(DegreetoRadians(angle1+anglecorret))*h2;
								float dis = dis2 - dis1;
								float y_weight = dis*Mathf.Sin(DegreetoRadians(transform.eulerAngles.x));
								float z_weight = dis*Mathf.Cos(DegreetoRadians(transform.eulerAngles.x));
								transform.Translate (0, -y_weight, -z_weight );
							}
						}
					}else{
						if(currentGesture.Equals("begin")){
							gestureList.Add("updown");
							if(isContinuousSameGesture("updown")){//连续三个updown
								currentGesture = "updown";
							}else{
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}else{
							if(!currentGesture.Equals("updown")){//zoom or rotate
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}
						float angle = (touchZero.position - touch0before).y*90.0f / Screen.height;
						if(angle > 0){//look up
							if(transform.eulerAngles.x < getMaxAngleByHeight()){
								print("can not look up anymore");
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}else{
							if(transform.eulerAngles.x > maxAngle){
								print("can not look down anymore");
								touch0before = touchZero.position;
								touch1before = touchOne.position;
								return;
							}
						}
						if(transform.eulerAngles.x - angle > maxAngle){//防止倒过来看
							angle = transform.eulerAngles.x - maxAngle;
						}
						Vector3 cameraLeftWorldVector = transform.TransformDirection (Vector3.left);
						Vector3 centerPoint = new Vector3(Screen.width/2,Screen.height/2,transform.position.y/Mathf.Sin(DegreetoRadians(transform.eulerAngles.x)));
						transform.RotateAround (camera.ScreenToWorldPoint(centerPoint), cameraLeftWorldVector, angle);
						hasUpDown = true;
					}
				}
				touch0before = touchZero.position;
				touch1before = touchOne.position;
			}
		}
	}
	float VectorAngle(Vector2 from, Vector2 to)
	{
		float angle;
		Vector3 cross=Vector3.Cross(from, to);
		angle = Vector2.Angle(from, to);
		return cross.z > 0 ? -angle : angle;
	}
	float getMaxAngleByHeight(){
		float height = transform.position.y;
		if (height <= minHigh*3) {
			return minAngle;
		} else if (height < fitHigh) {
			return minAngle + (maxAngle-minAngle)/(fitHigh-minHigh*3)*(height-minHigh*3);
		} else {
			return maxAngle;
		}
	}

	void startRotateBack(){
		if(hasRotated){
			isRotateBack = true;
		}
	}
	float DegreetoRadians(float x)
	{
		return x * 0.017453292519943295769f;
	}
	public void iosCallUnity(string message){
		print ("iosCallUnity-------"+message);
//		transform.position = new Vector3 (0, minHigh*10, 0);
//		transform.eulerAngles = new Vector3 (90,180,0); 
	}
	IEnumerator StartGPS () {
		// Input.location 用于访问设备的位置属性（手持设备）, 静态的LocationService位置
		// First, check if user has location service enabled
		if (!Input.location.isEnabledByUser)
			yield break;
		
		// Start service before querying location
		Input.location.Start();
		
		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
		{
			yield return new WaitForSeconds(1);
			maxWait--;
		}
		
		// Service didn't initialize in 20 seconds
		if (maxWait < 1)
		{
			print("Timed out");
			yield break;
		}
		
		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed)
		{
			print("Unable to determine device location");
			yield break;
		}
		else
		{
			// Access granted and location value could be retrieved
			print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
		}
		
		// Stop service if there is no need to query location updates continuously
		//		Input.location.Stop();
	}
	void OnGUI () { 
		if (GUI.Button (new Rect (10, 10, 50, 20), "farward")) {
			transform.Translate(Vector3.forward*0.5f);
		}
		if (GUI.Button (new Rect (130, 10, 50, 20), "back")) {
			transform.Translate(-Vector3.forward*0.5f);
		}
		if (GUI.Button (new Rect (10, 40, 50, 20), "left")) {
			transform.Translate(Vector3.left*0.5f);
		}
		if (GUI.Button (new Rect (130, 40, 50, 20), "right")) {
			transform.Translate(-Vector3.left*0.5f);
		}
		if (GUI.Button (new Rect (70, 10, 50, 20), "up")) {
			transform.Translate(Vector3.up*0.5f);
		}
		if (GUI.Button (new Rect (70, 70, 50, 20), "down")) {
			transform.Translate(-Vector3.up*0.5f);
//			zywx_highlightDisplayPoiWithType("0");
		}
		GUI.backgroundColor = Color.clear;
		GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
		centeredStyle.alignment = TextAnchor.UpperCenter;
		centeredStyle.fontSize = 29;
		centeredStyle.normal.textColor = Color.black;


		string[] doors = doorString.Split(new char[] { ';' });
		for (int i=0; i<doors.Length; i++) {
			string[] door = doors[i].Split(new char[] { ',' });
			float lon = float.Parse(door[0]);
			float lat = float.Parse(door[1]);
			LonLatPoint lonlatpoint = new LonLatPoint(lon,lat);
			PixelPoint point = gp.lonlatToPixel (lonlatpoint,17);
			Vector3 screenpos = Camera.main.WorldToScreenPoint(new Vector3 (-(float)point.pointX/100f,0f,(float)point.pointY/100f));
			GUI.Label (new Rect (screenpos.x-texture_door.width/8, Screen.height - screenpos.y-texture_door.height/8, texture_door.width/4, texture_door.height/4), texture_door);
		}
		listPoisAlreadyInScreen = new ArrayList ();
		for (int i=0; i<list_display.Count; i++) {
			PoiClass poi = (PoiClass)list_display[i];
			int level = poi.level;
			float lon = poi.lon;
			float lat = poi.lat;
			int type = poi.type;
			string name = poi.name;
			int isSelected = poi.isSelected;
			LonLatPoint lonlatpoint = new LonLatPoint(lon,lat);
			PixelPoint point = gp.lonlatToPixel (lonlatpoint,17);
			Vector3 screenpos = Camera.main.WorldToScreenPoint(new Vector3 (-(float)point.pointX/100f,0f,(float)point.pointY/100f));
			if(!isPositonInScreen(screenpos))continue;//不在屏幕内的不显示
			float poiDistanceFromCamera = Vector3.Distance(new Vector3 (-(float)point.pointX/100f,0f,(float)point.pointY/100f),transform.position);

			if(kav.gdLevel2UnityCameraHeight(level) > poiDistanceFromCamera){
				poi.screenPosition = new Vector2(screenpos.x,Screen.height - screenpos.y);
				if(!calculateWhichPositionShouldPlace(poi)){
					continue;
				}
				listPoisAlreadyInScreen.Add(poi);
				if(type == selectedType || isSelected == 1){//big
					if (GUI.Button (new Rect (screenpos.x - 31.5f, Screen.height - screenpos.y - 76f, 63f, 76f), (GUIContent)texture_big[type])) {
						if(!isDraging){
							_unityCallIOS("clickpoi|"+name);
						}
					}
					if (GUI.Button (new Rect (screenpos.x-poi.labelLength/2, Screen.height - screenpos.y-10, poi.labelLength, label_high), "")) {
						if(!isDraging){
							_unityCallIOS("clickpoi|"+name);
						}
					}
					centeredStyle.alignment = TextAnchor.UpperCenter;
					centeredStyle.normal.textColor = new Color(1,1,1,0.5f);
					GUI.Label (new Rect (screenpos.x-poi.labelLength/2, Screen.height - screenpos.y-2-10, poi.labelLength, label_high), name,centeredStyle);
					GUI.Label (new Rect (screenpos.x-poi.labelLength/2-2, Screen.height - screenpos.y-10, poi.labelLength, label_high), name,centeredStyle);
					GUI.Label (new Rect (screenpos.x-poi.labelLength/2+2, Screen.height - screenpos.y-10, poi.labelLength, label_high), name,centeredStyle);
					GUI.Label (new Rect (screenpos.x-poi.labelLength/2, Screen.height - screenpos.y+2-10, poi.labelLength, label_high), name,centeredStyle);
					centeredStyle.normal.textColor = Color.black;
					GUI.Label (new Rect (screenpos.x-poi.labelLength/2, Screen.height - screenpos.y-10, poi.labelLength, label_high), name,centeredStyle);
				}else{//small
					if (GUI.Button (new Rect (screenpos.x - 22, Screen.height - screenpos.y - 22, 44, 44), (GUIContent)texture_small[type])) {
						if(!isDraging){
							selectOnePoi(poi);
							_unityCallIOS("clickpoi|"+name);
						}
					}
					if(type == 0 || type == 1 || type ==2){
						float label_position_x = 0.0f;
						float label_position_y = 0.0f;
						switch(poi.textPosition){
						case 1://right
							label_position_x = screenpos.x + 22f;
							label_position_y = Screen.height - screenpos.y - label_high/2;
							centeredStyle.alignment = TextAnchor.MiddleLeft;
							break;
						case 2://left
							label_position_x = screenpos.x - 22f - poi.labelLength;
							label_position_y = Screen.height - screenpos.y - label_high/2;
							centeredStyle.alignment = TextAnchor.MiddleRight;
							break;
						case 3://top
							label_position_x = screenpos.x - poi.labelLength/2;
							label_position_y = Screen.height - screenpos.y - 22 - label_high + 10;
							centeredStyle.alignment = TextAnchor.LowerCenter;
							break;
						case 4://bottom
							label_position_x = screenpos.x - poi.labelLength/2;
							label_position_y = Screen.height - screenpos.y + 22 - 10;
							centeredStyle.alignment = TextAnchor.UpperCenter;
							break;
						default://right
							label_position_x = screenpos.x + 22f;
							label_position_y = Screen.height - screenpos.y - label_high/2;
							centeredStyle.alignment = TextAnchor.MiddleLeft;
							break;
						}

						if (GUI.Button (new Rect (label_position_x, label_position_y, poi.labelLength, label_high), "")) {
							if(!isDraging){
								selectOnePoi(poi);
								_unityCallIOS("clickpoi|"+name);
							}
						}
						centeredStyle.normal.textColor = new Color(1,1,1,0.5f);
						GUI.Label (new Rect (label_position_x, label_position_y-2, poi.labelLength, label_high), name,centeredStyle);
						GUI.Label (new Rect (label_position_x-2, label_position_y, poi.labelLength, label_high), name,centeredStyle);
						GUI.Label (new Rect (label_position_x+2, label_position_y, poi.labelLength, label_high), name,centeredStyle);
						GUI.Label (new Rect (label_position_x, label_position_y+2, poi.labelLength, label_high), name,centeredStyle);
						centeredStyle.normal.textColor = Color.black;
						GUI.Label (new Rect (label_position_x, label_position_y, poi.labelLength, label_high), name,centeredStyle);
					}
				}
			}
		}
	}
	bool isContinuousSameGesture(string ges){
		int count = gestureList.Count;
		if (count < 3) {
			return false;
		} else {
			if(gestureList[count-1].Equals(ges)&&gestureList[count-2].Equals(ges)&&gestureList[count-3].Equals(ges)){
				return true;
			}else{
				return false;
			}
		}
	}
	bool isPositonInScreen(Vector3 position){
		float x_inscreen = position.x;
		float y_inscreen = Screen.height - position.y;
		if (x_inscreen < 0 || y_inscreen < 0)
			return false;
		if (x_inscreen > Screen.width || y_inscreen > Screen.height)
			return false;
		return true;
	}
	//如果返回true则说明可以放到屏幕上，返回false则不能放屏幕上
	bool calculateWhichPositionShouldPlace(PoiClass poi){
		if (listPoisAlreadyInScreen.Count == 0)
			return true;
		if (poi.isSelected == 1) {//点击选择的，没得说，直接加上
			return true;
		}
		int i, j;
		if (poi.type == selectedType) {//把所有该类型的poi都显示为大图标,有冲突的不予显示
			for (i = 0; i<listPoisAlreadyInScreen.Count; i++) {
				PoiClass poiAlreadyInScreen = (PoiClass)listPoisAlreadyInScreen [i];
				if (poi.hasConflict (poiAlreadyInScreen,selectedType)) {
//					print("has conflict2");
					break;
				}
			}
			if(i < listPoisAlreadyInScreen.Count){
				return false;
			}else{
				return true;
			}
		} else {//小图标表示的poi
			if (poi.type == 0 || poi.type == 1 || poi.type == 2) {//有label的图标
				for (i = 1; i<=4; i++) {//4 position
					poi.textPosition = i;
					for (j = 0; j<listPoisAlreadyInScreen.Count; j++) {
						PoiClass poiAlreadyInScreen = (PoiClass)listPoisAlreadyInScreen [j];
						if (poi.hasConflict (poiAlreadyInScreen,selectedType)) {
//							print("has conflict");
							break;
						}
					}
					if (j == listPoisAlreadyInScreen.Count) {
						return true;
					}
				}
				return false;//放到4个方向哪个都有冲突，不放到屏幕上
			} else {//没有label的图标
				return true;
			}
		}
	}
	//------------------------------------------供自游无限使用的接口-----------------------------------------------------
	//清除地图上的poi
	void removeAllPoi(){
		list_display.Clear ();
	}
	//init params:lon,lat,以给定经纬度为中心点显示3d景区:example:"116.270928|39.986163"
	void zywx_init3D(string message){
		print ("unity:zywx_init3D:" + message);
		string []s = message.Split(new char[] { '|' });
		//以该点为中心显示
		zywx_moveToLocation(float.Parse(s[0]),float.Parse(s[1]),true,false,10,20);
		print ("unity:zywx_init3D:success" );
	}
	void zywx_moveToLocation(float lon,float lat,bool annimation,bool isWatch,float high,float speed){
		step = speed;
		LonLatPoint lonlatpoint = new LonLatPoint(lon,lat);
		PixelPoint point = gp.lonlatToPixel (lonlatpoint,17);

		if (isWatch) {
			transform.eulerAngles = new Vector3 (60, 180, 0); 
			transform.position = new Vector3(transform.position.x,1.5f,transform.position.z);
			float zoffsize = Mathf.Tan(DegreetoRadians(90-transform.eulerAngles.x))*transform.position.y;
			targetPosition = new Vector3 (-(float)point.pointX / 100f, high, (float)point.pointY / 100f+zoffsize);
			aroundPosition = new Vector3 (-(float)point.pointX / 100f, 0, (float)point.pointY / 100f);
			wantWatch = true;
		} else {
			transform.eulerAngles = new Vector3 (90, 180, 0); 
			targetPosition = new Vector3 (-(float)point.pointX / 100f, high, (float)point.pointY / 100f);
		}
		if (annimation) {
			startMove = true;
		} else {
			transform.position = targetPosition;
		}
	}
	//poi数据传递给unity:
	void zywx_setPoiDateSource(string message){
//		 		message = "116.274361,40.000229,后大庙,0,0,17;116.274315,39.999905,四大部洲建筑群,0,0,15";
		message =	 "116.273949,39.999092,景区简介,0,0,14;116.273933,39.999416,众香界,0,0,18;116.273941,39.999630,智慧海,0,0,19;116.274269,39.999172,敷华亭 撷秀亭,0,0,17;116.274574,39.999229,转轮藏,0,0,18;116.274551,39.999264,万寿山昆明湖碑,0,0,19;116.273186,39.999096,五方阁,0,0,17;116.273193,39.998981,宝云阁,0,0,15;116.272812,39.998623,邵窝殿,0,0,19;116.272514,39.998421,云松巢,0,0,17;116.272301,39.998520,贵寿无极,0,0,18;116.270966,39.998085,听鹂馆,0,0,19;116.270927,39.998661,画中游,0,0,17;116.271080,39.999161,湖山真意,0,0,18;116.270798,39.998123,西四所,0,0,19;116.269814,39.998112,寄澜堂,0,0,17;116.270248,39.997761,清晏舫,0,0,18;116.269653,39.998234,荇桥,0,0,19;116.269646,39.998028,小西泠,0,0,17;116.268913,39.998177,五圣祠,0,0,16;116.268829,39.998482,迎旭楼,0,0,19;116.268700,39.999104,澄怀阁,0,0,17;116.269341,39.998653,临河殿,0,0,18;116.269730,39.998718,穿堂殿,0,0,19;116.269737,39.998650,小有天,0,0,17;116.269821,39.998573,斜门殿,0,0,18;116.269585,39.998917,延清赏楼,0,0,19;116.268990,39.999989,北船坞,0,0,17;116.269470,39.999779,宿云檐,0,0,18;116.267883,40.000965,半壁桥,0,0,19;116.277924,39.998493,邀月门,0,0,17;116.276894,39.998398,留佳亭,0,0,18;116.275658,39.998260,寄澜亭,0,0,16;116.272400,39.997910,秋水亭,0,0,17;116.270988,39.997780,清遥亭,0,0,18;116.270226,39.997829,石丈亭,0,0,19;116.271782,39.997742,对鸥舫 鱼藻轩,0,0,17;116.271606,39.998074,山色湖光共一楼,0,0,18;116.274101,39.982765,西堤,0,0,19;116.267899,40.000267,界湖桥,0,0,17;116.267517,39.996513,豳风桥,0,0,18;116.269447,39.991138,镜桥,0,0,14;116.271614,39.989082,练桥,0,0,15;116.273598,39.983963,柳桥,0,0,18;116.273796,39.986256,景明楼,0,0,19;116.265717,39.986759,畅观堂,0,0,17;116.280273,39.990837,新建宫门,0,0,18;116.275879,39.991474,南湖岛建筑群,0,0,14;116.276268,39.991192,广润灵雨祠,0,0,17;116.275986,39.991711,涵虚堂,0,0,18;116.279648,39.982094,凤凰墩,0,0,19;116.279884,39.981625,绣漪桥,0,0,17;116.279343,39.996712,知春亭,0,0,18;116.280045,39.996590,文昌阁,0,0,19;116.280220,39.996819,耶律楚材祠,0,0,17;116.283684,39.998135,涵虚牌楼,0,0,18;116.281059,39.997910,东宫门,0,0,19;116.280006,39.997765,仁寿殿,0,0,17;116.279320,39.997871,玉澜堂,0,0,18;116.279167,39.998451,宜芸馆,0,0,19;116.278412,39.998619,乐寿堂,0,0,17;116.278793,39.998783,永寿斋,0,0,18;116.277908,39.999035,扬仁风,0,0,19;116.279762,39.998470,德和园,0,0,17;116.279732,39.998775,颐乐殿,0,0,16;116.279663,39.999004,庆善堂,0,0,19;116.279526,39.998440,膳房,0,0,17;116.278488,39.998299,电灯公所,0,0,18;116.281601,39.999931,引镜,0,0,19;116.281868,40.000271,洗秋 饮绿,0,0,17;116.282509,40.000603,澹碧 知春堂,0,0,18;116.282135,40.000702,蘭亭,0,0,19;116.282013,40.000912,湛清轩,0,0,17;116.281807,40.000668,涵远堂,0,0,18;116.281578,40.000721,瞩新楼,0,0,19;116.281311,40.000332,澄爽斋,0,0,17;116.281937,40.000408,知鱼桥,0,0,16;116.268654,40.001789,西宫门 如意门,0,0,19;116.280983,40.001595,眺远斋,0,0,17;116.281677,40.001125,霁清轩,0,0,18;116.275375,40.001316,寅辉城关,0,0,19;116.279991,40.000320,益寿堂,0,0,17;116.280502,40.000088,乐农轩 永寿斋,0,0,18;116.280716,39.999466,紫气东来城关,0,0,19;116.274361,40.000229,后大庙,0,0,17;116.274162,40.001125,慈福牌楼,0,0,18;116.274315,39.999905,四大部洲建筑群,0,0,15;116.274063,40.002598,北宫门,0,0,17;116.273239,39.999794,云会寺,0,0,18;116.275566,39.999138,写秋轩,0,0,19;116.275871,39.999634,重翠亭,0,0,17;116.276276,39.999783,千峰彩翠,0,0,18;116.276649,39.999031,意迟云在,0,0,19;116.276367,39.998756,无尽意轩,0,0,17;116.277420,39.998875,养云轩,0,0,18;116.277153,39.999516,福荫轩,0,0,19;116.278175,39.999367,含新亭,0,0,17;116.279518,39.999931,景福阁,0,0,18;116.274231,39.997501,云辉玉宇牌楼,0,0,15;116.274559,39.998035,玉华殿 云锦殿,0,0,17;116.273926,39.998753,德辉殿,0,0,15;116.273376,39.998093,介寿堂 清华轩,0,0,19;116.270981,39.998203,戏楼,0,1,17;116.275986,39.998295,长廊,0,1,18;116.266991,39.993355,玉带桥,0,1,19;116.266701,39.997227,耕织图,0,1,17;116.276756,39.999634,万寿山,0,1,16;116.274467,39.994698,昆明湖,0,1,14;116.278610,39.990360,廓如亭,0,1,17;116.278412,39.990292,十七孔桥,0,1,18;116.279739,39.998608,大戏楼,0,1,19;116.281891,40.000050,谐趣园,0,1,17;116.274010,40.001915,苏州街,0,1,18;116.278488,40.000893,澹宁堂,0,1,19;116.273750,39.997917,排云殿,10,1,14;116.273949,39.999092,佛香阁,10,1,14;116.279630,39.990094,百合素食,1,0,13;116.270965,39.998083,听鹂馆饭庄,1,0,14;116.270574,39.998049,北京小吃(青龙桥东街),1,0,15;116.275025,39.998235,颐和园快餐厅,1,0,16;116.270424,39.998196,宫廷小吃,1,0,17;116.281540,39.992997,德善园餐厅,1,0,18;116.280168,39.996348,加加游(颐和园店),1,0,19;116.279869,39.996964,颐和园餐厅,1,0,14;116.276058,39.991808,丽春林词,2,0,13;116.267621,39.994141,颐和园商店,2,0,14;116.273696,39.986538,颐和园商店,2,0,15;116.271499,39.998214,颐和园便利店(青龙桥东街),2,0,16;116.275048,39.998143,颐和园食品部(青龙桥东街),2,0,17;116.279327,39.997471,玉澜门,2,0,18;116.279935,39.997098,颐和园商店,2,0,19;116.275528,39.991456,公共厕所,3,0,13;116.266458,39.994298,公共厕所,3,0,14;116.279704,39.990762,公共厕所,3,0,15;116.272464,39.998539,公共厕所,3,0,16;116.281599,39.990361,公共厕所,3,0,17;116.269945,39.998496,公共厕所,3,0,18;116.277559,40.002323,北宫门,4,0,14;116.279318,39.997309,游船码头,5,0,14;116.280499,39.980737,颐和园南如意门,6,0,14;116.273997,40.002734,颐和园北宫门,6,0,14;116.280284,39.990997,颐和园新建宫门,6,0,14;116.281297,39.992084,停车场(昆明湖路),7,0,14;116.281183,39.993530,停车场(昆明湖东路),7,0,14;116.259813,39.999282,医院T,8,0,14;116.261358,39.994482,娱乐设施T,9,0,14;116.283932,39.993890,住宿T,11,0,14;116.281528,39.998168,颐和园售票处,12,0,14;116.282693,39.997379,ATMT,13,0,14;116.282322,39.991901,邮局T,14,0,14;116.280112,39.991276,公用电话,15,0,14";
		print ("unity:zywx_setPoiDateSource:" + message);
		removeAllPoi ();
		string[] spots = message.Split (new char[] { ';' });
		for (int i = 0; i < spots.Length; i++) {
			string[] oneSpot = spots[i].Split(new char[] { ',' });
			float lon = float.Parse(oneSpot[0]);
			float lat = float.Parse(oneSpot[1]);
			string name = oneSpot[2];
			print("name is "+name+" length is "+name.Length);
			float labelLength = name.Length * 30;
			int type = int.Parse(oneSpot[3]);
			int ishot = int.Parse(oneSpot[4]);
			int level = int.Parse(oneSpot[5]);
			PoiClass poi = new PoiClass(lon,lat,name,type,ishot,level,0,1,labelLength);
			list_display.Add(poi);
		}
		print ("unity:zywx_setPoiDateSource:success" );
	}

	//展示热门景点并360度查看
	void zywx_displayOnePoi(string message){
		print ("unity:zywx_displayOnePoi:" + message);
		foreach (PoiClass poi in list_display) {
			if(poi.name.Equals(message)){
				zywx_moveToLocation(poi.lon,poi.lat,true,false,5f,10);
				selectOnePoi(poi);
				break;
			}
		}
		print ("unity:zywx_displayPoi:success" );
	}
	void zywx_highlightDisplayPoiWithType(string message){
		print ("unity:zywx_highlightDisplayPoiWithType:" + message);
		selectedType = int.Parse (message);
		if (lastSelectedPoi != null) {
			lastSelectedPoi.isSelected = 0;
		}
		print ("unity:zywx_highlightDisplayPoiWithType:success" );
	}
	void selectOnePoi(PoiClass poi){
		poi.isSelected = 1;
		if (lastSelectedPoi != null) {
			lastSelectedPoi.isSelected = 0;
		}
		lastSelectedPoi = poi;
	}

	void setCameraHighToSeeAllPois(){
		print ("unity:setCameraHighToSeeAllPois:");
		if(list_display.Count < 1)return;
		PoiClass poi = (PoiClass)list_display[0];
		float minlat = poi.lat;
		float maxlat = poi.lat;
		float minlon = poi.lon;
		float maxlon = poi.lon;
		if (list_display.Count == 1) {
			zywx_moveToLocation(poi.lon,poi.lat,true,false,10f,8);
			return;
		}
		foreach(PoiClass poi2 in list_display){
			if(poi2.lat < minlat) minlat = poi2.lat;
			if(poi2.lat > maxlat) maxlat = poi2.lat;
			if(poi2.lon < minlon) minlon = poi2.lon;
			if(poi2.lon > maxlon) maxlon = poi2.lon;
		}
		zywx_setCameraPositionByBounds (""+minlat+","+maxlat+","+minlon+","+maxlon);
	}
	void zywx_setCameraPositionByBounds(string message){
		print ("unity:zywx_setCameraPositionByBounds:" + message);
		string[] str = message.Split(new char[] { ',' });
		float minlat = float.Parse(str[0]);
		float maxlat = float.Parse(str[1]);
		float minlon = float.Parse(str[2]);
		float maxlon = float.Parse(str[3]);
		LonLatPoint lonlatpoint1 = new LonLatPoint(minlon,minlat);
		PixelPoint point1 = gp.lonlatToPixel (lonlatpoint1,17);
		LonLatPoint lonlatpoint2 = new LonLatPoint(maxlon,minlat);
		PixelPoint point2 = gp.lonlatToPixel (lonlatpoint2,17);
		float yOffsize = Mathf.Abs ((float)point1.pointX / 100f - (float)point2.pointX / 100f);
		float cameraHigh = yOffsize / Mathf.Tan (DegreetoRadians(30));
		print ("cameraHigh is "+cameraHigh);
		if (cameraHigh > fitHigh)
			cameraHigh = fitHigh;
		zywx_moveToLocation((minlon+maxlon)/2,(minlat+maxlat)/2,true,false,cameraHigh,10);
		print ("unity:zywx_setCameraPositionByBounds:success" );
	}
	//------------------------------------------调用自游无限的接口-----------------------------------------------------
	[DllImport ("__Internal")]
	private static extern void _unityCallIOS (string message);
}

