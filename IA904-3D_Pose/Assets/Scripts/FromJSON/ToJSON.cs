using System.IO;
using UnityEditor;
using UnityEngine;

namespace HIAAC.FromJSON
{
    /// <summary>
    /// Converts ScriptableObjects to JSON
    /// </summary>
    public class ToJSON : MonoBehaviour
    {
        #if UNITY_EDITOR

            /// <summary>
            /// Creates an TextAsset with JSON from a ScriptableObject
            /// </summary>
            [MenuItem("Assets/Create/JSON/JSON from Scriptable Object", false, 0)]
            static void SOToTextAsset()
            {
                foreach(UnityEngine.Object obj in Selection.objects)
                {
                    ScriptableObject so = (ScriptableObject) obj;
                    string json = SOToJSON(so);

                    string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID())); 
                    path += "/"+so.name+".json";

                    StreamWriter writer = new StreamWriter(path);
                    writer.Write(json);
                    writer.Close();
                }
                
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// Validates if all the selected objects are ScriptableObjects for conversion.
            /// </summary>
            /// <returns>True  if if all the selected objects are ScriptableObjects.</returns>
            [MenuItem("Assets/Create/JSON/JSON from Scriptable Object", true)]
            static bool SOToTextAssetValidator()
            {
                foreach(UnityEngine.Object obj in Selection.objects)
                {
                    ScriptableObject test = Selection.activeObject as ScriptableObject;
                    
                    if(test == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        #endif

        /// <summary>
        /// Converts an ScriptableObject to JSON string.
        /// </summary>
        /// <param name="so">ScriptableObject to convert</param>
        /// <returns>Generated JSON string.</returns>
        public static string SOToJSON(ScriptableObject so)
        {
            string json = JsonUtility.ToJson(so, true);

            string type_string = ",\n    \"type\": \""+so.GetType().FullName+"\"";

            json = json.Insert(json.Length-2, type_string);

            return json;
        }
    }
}