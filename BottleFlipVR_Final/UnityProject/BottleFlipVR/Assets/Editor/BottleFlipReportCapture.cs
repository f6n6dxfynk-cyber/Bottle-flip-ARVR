using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BottleFlipReportCapture
{
 public static void Capture()
 {
  EditorSceneManager.OpenScene("Assets/Scenes/BottleFlipVR.unity");
  var bottle=GameObject.Find("Bottle");
  foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if(!r.transform.IsChildOf(bottle.transform)) r.enabled=false;
  bottle.transform.rotation=Quaternion.Euler(0,0,-22);
  var camera=Camera.main; camera.GetComponent<BottleFlipCamera>().enabled=false;
  camera.transform.position=bottle.transform.position+new Vector3(2,1,-5);
  camera.transform.LookAt(bottle.transform.position+Vector3.up*.1f);
  camera.orthographic=true; camera.orthographicSize=1.28f;
  camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0,0,0,0);
  var rt=new RenderTexture(900,1100,24,RenderTextureFormat.ARGB32); camera.targetTexture=rt;camera.Render();
  RenderTexture.active=rt; var tex=new Texture2D(900,1100,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,900,1100),0,0);tex.Apply();
  System.IO.File.WriteAllBytes("/Users/rudrapancholi/Downloads/BottleFlipVR_Final/Evidence/bottle-model.png",tex.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
  Debug.Log("REPORT_MODEL_CAPTURE_OK");
 }
}
